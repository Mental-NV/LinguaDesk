import {
  expect,
  test,
  type Browser,
  type BrowserContext,
  type Page,
} from '@playwright/test'
import { verifiedAccountStorageState } from './rewrite-auth'

function parseConsumed(line: string): number {
  const match = line.match(/([\d,]+) of/)
  if (!match) throw new Error(`Cannot parse consumed characters from: ${line}`)
  return Number(match[1].replace(/,/g, ''))
}

/**
 * M029 published journey (AC-001-006): the predefined verified E2E user
 * rewrites text at /rewrite through the published SPA, real cookie auth,
 * real API, migrated Smoke SQLite and durable accounting with the Smoke
 * deterministic rewriting provider adapter. Every case asserts its
 * browser-visible outcome through semantic locators; request counts and usage
 * text supplement but never replace the UI assertion. Cases run sequentially
 * in declaration order against the single predefined account, which is shared
 * with the sibling published suites: the happy path therefore asserts the
 * +46 W-OK charge as a delta over the usage observed in its own page rather
 * than an absolute baseline (the absolute 7,546 = seed 7,500 + 46 holds when
 * this suite runs first or standalone against a fresh seed).
 */

const W_OK = 'The report is really ready. We sends it today.'
const W_RESULT = 'The report is ready. We send it today.'
const W_MODES_RESULT = W_RESULT
const W_CHANGED = 'The summary is really complete. She go now.'
const W_LONG = 'a'.repeat(2312)

const SIGN_IN_PASSWORD = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
const VERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL

if (!SIGN_IN_PASSWORD || !VERIFIED_EMAIL) {
  throw new Error('The published rewrite smoke account configuration is required.')
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

async function openWorkspace(page: Page): Promise<void> {
  await page.goto('/rewrite')
  await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
  await expect(page.getByLabel('Source text')).toBeVisible()
}

async function usageLine(page: Page): Promise<string> {
  return (await page.getByRole('region', { name: 'Usage' }).innerText()).trim()
}

test('happy path rewrites once with an editable, copyable result and authoritative usage', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await openWorkspace(page)

    await expect(page.getByLabel('Writing mode')).toHaveValue('correctionOnly')
    await page.getByLabel('Writing language').selectOption('en')
    await page.getByLabel('Source text').fill(W_OK)
    await expect(page.getByText(/characters used/)).toBeVisible()
    const usageBefore = parseConsumed(await usageLine(page))
    await page.waitForTimeout(1000)
    expect(operations).toHaveLength(0)

    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    expect(operations).toHaveLength(1)
    expect(operations[0].body.family).toBe('rewriting')
    expect(operations[0].body.source).toBe(W_OK)
    expect(operations[0].body.sourceSelection).toBe('en')
    expect(operations[0].body.mode).toBe('correctionOnly')
    expect(operations[0].body.operationId).not.toBe('')
    await expect(page.getByText('Up to date')).toBeVisible()
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(usageBefore + 46)
    await expect(page.getByLabel('Source text')).toHaveValue(W_OK)

    await page.getByLabel('Result').fill(`${W_RESULT} Edited.`)
    await page.waitForTimeout(500)
    expect(operations).toHaveLength(1)
    await page.getByRole('button', { name: 'Copy result' }).click()
    await expect(page.getByText('Result copied to clipboard.')).toBeVisible()
    expect(await page.evaluate(() => window.navigator.clipboard.readText())).toBe(
      `${W_RESULT} Edited.`,
    )
  } finally {
    await context.close()
  }
})

test('mode choice submits once with the single final value and zero prior requests', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await openWorkspace(page)

    await page.getByLabel('Writing language').selectOption('en')
    await page.getByLabel('Source text').fill(W_OK)
    await page.getByLabel('Writing mode').selectOption('business')
    await page.getByLabel('Writing mode').selectOption('friendly')
    await expect(page.getByLabel('Writing mode')).toHaveValue('friendly')
    await page.waitForTimeout(500)
    expect(operations).toHaveLength(0)

    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(W_MODES_RESULT)
    expect(operations).toHaveLength(1)
    expect(operations[0].body.family).toBe('rewriting')
    expect(operations[0].body.mode).toBe('friendly')
  } finally {
    await context.close()
  }
})

test('oversize input is blocked with the excess count and charges nothing', async ({ browser }) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await openWorkspace(page)

    await page.getByLabel('Source text').fill(W_LONG)
    await expect(
      page.getByText('Rewriting is limited to 2,000 characters. Remove 312 characters to continue.'),
    ).toBeVisible()
    await expect(page.getByRole('button', { name: 'Rewrite' })).toBeDisabled()
    await expect(page.getByLabel('Source text')).toHaveValue(W_LONG)
    await page.waitForTimeout(500)
    expect(operations).toHaveLength(0)
  } finally {
    await context.close()
  }
})

test('eligibility rejection preserves work with no successful-operation charge', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await openWorkspace(page)

    await page.getByLabel('Writing language').selectOption('en')
    await page.getByLabel('Source text').fill(W_OK)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    const usageBefore = await usageLine(page)
    const baselineOperations = operations.length

    await page.getByLabel('Source text').fill(`${W_OK} M029-ELIGIBILITY-REJECT`)
    await expect(page.getByText(/Input or settings changed\./)).toBeVisible()
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(
      page.getByText(
        'This text contains too much unsupported or mixed-language content. Use one main language: English, Russian, Romanian, or Chinese.',
      ),
    ).toBeVisible()
    expect(operations.length).toBe(baselineOperations + 1)
    await expect(page.getByLabel('Source text')).toHaveValue(`${W_OK} M029-ELIGIBILITY-REJECT`)
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    expect(await usageLine(page)).toBe(usageBefore)
  } finally {
    await context.close()
  }
})

test('definitive failure preserves work and retries once with a new operation key', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await openWorkspace(page)

    await page.getByLabel('Writing language').selectOption('en')
    await page.getByLabel('Source text').fill(W_OK)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    const usageBefore = await usageLine(page)
    const baselineOperations = operations.length

    await page.getByLabel('Source text').fill(`${W_OK} M029-PROCESSING-FAILURE`)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    const failureMessage =
      'We couldn’t process this text. Your text and previous result are safe.'
    await expect(page.getByText(failureMessage)).toBeVisible()
    await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible()
    await expect(page.getByLabel('Source text')).toHaveValue(`${W_OK} M029-PROCESSING-FAILURE`)
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    expect(await usageLine(page)).toBe(usageBefore)

    // Explicit retry captures the current fields once with a new operation key.
    await page.getByRole('button', { name: 'Try again' }).click()
    await expect.poll(() => operations.length, { timeout: 10_000 }).toBe(baselineOperations + 2)
    await expect(page.getByText(failureMessage)).toBeVisible()
    expect(operations[operations.length - 1].body.operationId).not.toBe(
      operations[operations.length - 2].body.operationId,
    )
    expect(await usageLine(page)).toBe(usageBefore)

    // Correcting the input clears the failure and reenables Rewrite.
    await page.getByLabel('Source text').fill(W_CHANGED)
    await expect(page.getByText(failureMessage)).toHaveCount(0)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect.poll(() => operations.length, { timeout: 10_000 }).toBe(baselineOperations + 3)
    // The deterministic adapter returns the same fixed fixture text for every
    // valid input; the corrected submission is proven by its charge.
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    const consumedBefore = parseConsumed(usageBefore)
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(consumedBefore + W_CHANGED.length)
  } finally {
    await context.close()
  }
})

test('the login form signs the predefined user into the rewriting workspace', async ({
  page,
}) => {
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()
  await page.getByLabel('Email').fill(VERIFIED_EMAIL)
  await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page).toHaveURL(/\/translate$/)
  await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
  await page.goto('/rewrite')
  await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
  await expect(page.getByLabel('Source text')).toBeVisible()
  await expect(page.getByLabel('Writing language')).toHaveValue('auto')
  await expect(page.getByLabel('Writing mode')).toHaveValue('correctionOnly')
  await expect(page.getByRole('button', { name: 'Rewrite' })).toBeVisible()
})
