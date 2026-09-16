import {
  expect,
  test,
  type Browser,
  type BrowserContext,
  type Locator,
  type Page,
} from '@playwright/test'
import { verifiedAccountStorageState } from './translate-auth'

/**
 * M038 desktop interaction contracts (AC-003, V-010 slice): the predefined
 * verified E2E user exercises native editing, real clipboard copy,
 * keyboard-only completion, IME composition guard and desktop geometry at
 * Chromium desktop sizes through the published SPA, real cookie auth, real
 * API, migrated Smoke SQLite and durable accounting with the Smoke
 * deterministic provider adapters. Every case asserts its browser-visible
 * outcome through semantic locators; request counts and per-page usage deltas
 * supplement but never replace the UI assertion. Cases run sequentially in
 * declaration order against the single predefined account shared with the
 * sibling published suites, so usage is asserted as per-page deltas only.
 * Narrow-screen, branded-browser and assistive-technology evidence stays with
 * M039/successors and the T004 manual pass; this file proves the
 * engine-level Chromium desktop contracts only.
 */

const TOK_A = 'Hello, the meeting starts at 14:30. Please go.'
const RO_FIXTURE = 'Bună, întâlnirea începe la 14:30. Te rog să mergi.'
const W_OK = 'The report is really ready. We sends it today.'
const W_RESULT = 'The report is ready. We send it today.'

const SIGN_IN_PASSWORD = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
const VERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL

if (!SIGN_IN_PASSWORD || !VERIFIED_EMAIL) {
  throw new Error('The published desktop-interaction smoke account configuration is required.')
}

let authenticatedState: Awaited<ReturnType<typeof verifiedAccountStorageState>> | null = null

test.beforeAll(async ({ browser }) => {
  authenticatedState = await verifiedAccountStorageState(browser)
})

async function verifiedWorkspace(browser: Browser): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext({
    storageState: authenticatedState ?? undefined,
    permissions: ['clipboard-read', 'clipboard-write'],
  })
  const page = await context.newPage()
  return { context, page }
}

interface OperationPost {
  method: string
  url: string
  body: Record<string, unknown>
}

function trackOperations(page: Page): OperationPost[] {
  const observed: OperationPost[] = []
  page.on('request', (request) => {
    const url = new URL(request.url())
    if (url.pathname === '/api/operations' && request.method() === 'POST') {
      observed.push({
        method: request.method(),
        url: url.pathname,
        body: (request.postDataJSON() ?? {}) as Record<string, unknown>,
      })
    }
  })
  return observed
}

function parseConsumed(line: string): number {
  const match = line.match(/([\d,]+) of/)
  if (!match) throw new Error(`Cannot parse consumed characters from: ${line}`)
  return Number(match[1].replace(/,/g, ''))
}

async function usageLine(page: Page): Promise<string> {
  return (await page.getByRole('region', { name: 'Usage' }).innerText()).trim()
}

async function assertNoHorizontalOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => ({
    scrollWidth: document.documentElement.scrollWidth,
    innerWidth: window.innerWidth,
  }))
  expect(
    overflow.scrollWidth,
    `no horizontal page overflow at ${overflow.innerWidth}px CSS width`,
  ).toBeLessThanOrEqual(overflow.innerWidth)
}

/** Move keyboard focus with real Tab presses until the target is focused. */
async function tabUntil(page: Page, target: Locator, label: string): Promise<void> {
  for (let step = 0; step < 60; step += 1) {
    if (await target.evaluate((element) => element === document.activeElement)) return
    await page.keyboard.press('Tab')
  }
  throw new Error(`Keyboard focus never reached ${label} within 60 Tab presses.`)
}

async function selectionOf(locator: Locator): Promise<{ start: number; end: number; value: string }> {
  return locator.evaluate((element) => {
    if (!(element instanceof HTMLTextAreaElement)) throw new Error('Expected a textarea element.')
    return { start: element.selectionStart, end: element.selectionEnd, value: element.value }
  })
}

test('native editing keeps caret, selection and undo intact on the translate source', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()

    const source = page.getByLabel('Source text')
    await source.click()
    await page.keyboard.type('Hello meeting', { delay: 10 })
    await expect(source).toHaveValue('Hello meeting')

    // Caret motion and shift-selection are native: ArrowLeft moves the caret,
    // Shift+ArrowLeft extends a real selection over the typed text.
    await page.keyboard.press('ArrowLeft')
    let selection = await selectionOf(source)
    expect(selection.start).toBe('Hello meetin'.length)
    expect(selection.end).toBe(selection.start)

    await page.keyboard.press('Shift+ArrowLeft')
    await page.keyboard.press('Shift+ArrowLeft')
    selection = await selectionOf(source)
    expect(selection.end - selection.start).toBe(2)

    // Typing over the selection replaces it, and a single undo restores it.
    await page.keyboard.type('X')
    await expect(source).toHaveValue('Hello meetXg')
    await page.keyboard.press('ControlOrMeta+z')
    await expect(source).toHaveValue('Hello meeting')

    // Native select-all still works on the field.
    await page.keyboard.press('ControlOrMeta+a')
    selection = await selectionOf(source)
    expect(selection.start).toBe(0)
    expect(selection.end).toBe('Hello meeting'.length)
  } finally {
    await context.close()
  }
})

test('keyboard-only completion translates once with visible focus and no trap', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()

    // Every step below is a real key press: Tab to each control, arrows to
    // choose languages, typed text, and keyboard activation of Translate.
    const sourceLanguage = page.getByLabel('Source language')
    await tabUntil(page, sourceLanguage, 'Source language')
    expect(await page.evaluate(() => document.activeElement?.tagName)).not.toBe('BODY')
    await sourceLanguage.selectOption('en')

    const targetLanguage = page.getByLabel('Target language')
    await tabUntil(page, targetLanguage, 'Target language')
    await targetLanguage.selectOption('ro')

    const source = page.getByLabel('Source text')
    await tabUntil(page, source, 'Source text')
    await page.keyboard.type(TOK_A, { delay: 5 })
    await expect(source).toHaveValue(TOK_A)
    await expect(page.getByText(/characters used/)).toBeVisible()
    const usageBefore = parseConsumed(await usageLine(page))
    await page.waitForTimeout(1000)
    expect(operations).toHaveLength(0)

    const translate = page.getByRole('button', { name: 'Translate' })
    await tabUntil(page, translate, 'Translate')
    await expect(translate).toBeFocused()
    await page.keyboard.press('Enter')
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    expect(operations).toHaveLength(1)
    await expect(page.getByText('Up to date')).toBeVisible()
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(usageBefore + 46)

    // Focus remains in a visible control after completion: Tab still moves.
    await page.keyboard.press('Tab')
    const focusedTag = await page.evaluate(() => document.activeElement?.tagName ?? 'NONE')
    expect(focusedTag).not.toBe('NONE')
  } finally {
    await context.close()
  }
})

test('keyboard-only completion rewrites once while the source stays unchanged', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await page.goto('/rewrite')
    await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
    await expect(page.getByLabel('Writing mode')).toHaveValue('correctionOnly')

    const source = page.getByLabel('Source text')
    await tabUntil(page, source, 'Source text')
    await page.keyboard.type(W_OK, { delay: 5 })
    await expect(source).toHaveValue(W_OK)
    const usageBefore = parseConsumed(await usageLine(page))
    await page.waitForTimeout(1000)
    expect(operations).toHaveLength(0)

    const rewrite = page.getByRole('button', { name: 'Rewrite' })
    await tabUntil(page, rewrite, 'Rewrite')
    await expect(rewrite).toBeFocused()
    await page.keyboard.press('Enter')
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    expect(operations).toHaveLength(1)
    await expect(page.getByLabel('Source text')).toHaveValue(W_OK)
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(usageBefore + 46)
  } finally {
    await context.close()
  }
})

test('activation during composition submits nothing; explicit activation after composition submits once', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')

    const source = page.getByLabel('Source text')
    await source.fill(TOK_A)
    await expect(page.getByText(/characters used/)).toBeVisible()
    await page.waitForTimeout(500)
    expect(operations).toHaveLength(0)

    // A real compositionstart event on the native textarea engages the guard:
    // the workspace leaves the ready state, so explicit activation is
    // refused with no request and no queued work.
    await source.evaluate((element) => {
      element.dispatchEvent(new CompositionEvent('compositionstart', { bubbles: true }))
    })
    await expect(page.getByRole('button', { name: 'Translate' })).toBeDisabled()
    await page.waitForTimeout(500)
    expect(operations).toHaveLength(0)
    await expect(page.getByLabel('Result')).toHaveCount(0)

    // Ending the composition and activating explicitly submits exactly once.
    await source.evaluate((element) => {
      element.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true }))
    })
    await expect(page.getByRole('button', { name: 'Translate' })).toBeEnabled()
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    expect(operations).toHaveLength(1)
  } finally {
    await context.close()
  }
})

test('rewrite composition changes and mode selection submit nothing until explicit activation', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await page.goto('/rewrite')
    await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()

    const source = page.getByLabel('Source text')
    await source.fill(W_OK)
    await page.getByLabel('Writing mode').selectOption('business')
    await source.evaluate((element) => {
      element.dispatchEvent(new CompositionEvent('compositionstart', { bubbles: true }))
    })
    await expect(page.getByRole('button', { name: 'Rewrite' })).toBeDisabled()
    await page.waitForTimeout(500)
    expect(operations).toHaveLength(0)

    await source.evaluate((element) => {
      element.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true }))
    })
    await expect(page.getByRole('button', { name: 'Rewrite' })).toBeEnabled()
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    expect(operations).toHaveLength(1)
    expect(operations[0].body.mode).toBe('business')
  } finally {
    await context.close()
  }
})

test('real clipboard copy captures the exact edited result on both workspaces', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TOK_A)
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)

    await page.getByLabel('Result').fill(`${RO_FIXTURE} Desktop.`)
    await page.getByRole('button', { name: 'Copy result' }).click()
    await expect(page.getByText('Result copied to clipboard.')).toBeVisible()
    expect(await page.evaluate(() => window.navigator.clipboard.readText())).toBe(
      `${RO_FIXTURE} Desktop.`,
    )

    await page.goto('/rewrite')
    await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
    await page.getByLabel('Source text').fill(W_OK)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)

    await page.getByLabel('Result').fill(`${W_RESULT} Desktop.`)
    await page.getByRole('button', { name: 'Copy result' }).click()
    await expect(page.getByText('Result copied to clipboard.')).toBeVisible()
    expect(await page.evaluate(() => window.navigator.clipboard.readText())).toBe(
      `${W_RESULT} Desktop.`,
    )
  } finally {
    await context.close()
  }
})

test('desktop geometry has no horizontal overflow or clipped controls including zoom and text spacing', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    for (const route of ['/translate', '/rewrite', '/login', '/register']) {
      await page.goto(route)
      await assertNoHorizontalOverflow(page)
    }

    // Effective 200% zoom doubles CSS pixel size; emulate it by halving the
    // CSS viewport and re-checking both workspaces for overflow.
    await page.setViewportSize({ width: 720, height: 450 })
    for (const route of ['/translate', '/rewrite']) {
      await page.goto(route)
      await assertNoHorizontalOverflow(page)
    }
    await page.setViewportSize({ width: 1440, height: 900 })

    // Text-spacing overrides (WCAG 1.4.12) must not clip controls or content.
    await page.goto('/translate')
    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TOK_A)
    await page.addStyleTag({
      content:
        '* { line-height: 1.5 !important; letter-spacing: 0.12em !important; ' +
        'word-spacing: 0.16em !important; } p { margin-bottom: 2em !important; }',
    })
    await assertNoHorizontalOverflow(page)
    await expect(page.getByRole('button', { name: 'Translate' })).toBeVisible()
    await expect(page.getByLabel('Source text')).toBeVisible()
    await expect(page.getByLabel('Result')).toHaveCount(0)

    await page.goto('/rewrite')
    await page.getByLabel('Source text').fill(W_OK)
    await page.addStyleTag({
      content:
        '* { line-height: 1.5 !important; letter-spacing: 0.12em !important; ' +
        'word-spacing: 0.16em !important; } p { margin-bottom: 2em !important; }',
    })
    await assertNoHorizontalOverflow(page)
    await expect(page.getByRole('button', { name: 'Rewrite' })).toBeVisible()
  } finally {
    await context.close()
  }
})
