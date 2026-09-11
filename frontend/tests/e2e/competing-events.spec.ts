import {
  expect,
  test,
  type Browser,
  type BrowserContext,
  type Page,
} from '@playwright/test'
import { verifiedAccountStorageState } from './translate-auth'

function parseConsumed(line: string): number {
  const match = line.match(/([\d,]+) of/)
  if (!match) throw new Error(`Cannot parse consumed characters from: ${line}`)
  return Number(match[1].replace(/,/g, ''))
}

/**
 * M030 published competing-event journey (AC-001/002/005): the predefined
 * verified E2E user keeps both workspaces trustworthy across navigation and
 * hidden settlement through the published SPA, real cookie auth, real API,
 * migrated Smoke SQLite and durable accounting with the Smoke deterministic
 * provider adapter. Every case asserts its browser-visible outcome through
 * semantic locators; request counts and per-page usage deltas supplement but
 * never replace the UI assertion. In-page stale fencing (AC-003/004) is
 * proven by the V-002/V-003 focused suites; the later-submission case below
 * covers the deterministic in-browser replacement path.
 */

const TOK_A = 'Hello, the meeting starts at 14:30. Please go.'
const RO_FIXTURE = 'Bună, întâlnirea începe la 14:30. Te rog să mergi.'
const W_OK = 'The report is really ready. We sends it today.'
const W_RESULT = 'The report is ready. We send it today.'

const SIGN_IN_PASSWORD = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
const VERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL

if (!SIGN_IN_PASSWORD || !VERIFIED_EMAIL) {
  throw new Error('The published competing-events smoke account configuration is required.')
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

async function usageLine(page: Page): Promise<string> {
  return (await page.getByRole('region', { name: 'Usage' }).innerText()).trim()
}

test('the login form signs the predefined user in, then mode links preserve both workspaces without submitting', async ({
  page,
}) => {
  const operations = trackOperations(page)
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()
  await page.getByLabel('Email').fill(VERIFIED_EMAIL)
  await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page).toHaveURL(/\/translate$/)
  await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()

  await page.getByLabel('Source language').selectOption('en')
  await page.getByLabel('Target language').selectOption('ro')
  await page.getByLabel('Source text').fill(TOK_A)
  await page.waitForTimeout(1000)
  expect(operations).toHaveLength(0)

  await page.getByRole('link', { name: 'Rewriting' }).click()
  await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
  await expect(page.getByLabel('Source text')).toHaveValue('')
  await expect(page.getByLabel('Result')).toHaveCount(0)
  await expect(page.getByLabel('Source text')).toBeVisible()
  await page.getByLabel('Source text').fill(W_OK)
  await page.waitForTimeout(500)
  expect(operations).toHaveLength(0)

  await page.getByRole('link', { name: 'Translation' }).click()
  await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
  await expect(page.getByLabel('Source text')).toHaveValue(TOK_A)
  await expect(page.getByLabel('Target language')).toHaveValue('ro')
  expect(operations).toHaveLength(0)

  await page.getByRole('link', { name: 'Rewriting' }).click()
  await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
  await expect(page.getByLabel('Source text')).toHaveValue(W_OK)
  expect(operations).toHaveLength(0)
})

test('a translate submission settles hidden without touching the rewriting page', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()

    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TOK_A)
    await expect(page.getByText(/characters used/)).toBeVisible()
    const usageBefore = parseConsumed(await usageLine(page))

    await page.getByRole('button', { name: 'Translate' }).click()
    await page.getByRole('link', { name: 'Rewriting' }).click()

    await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
    await expect(page.getByLabel('Source text')).toHaveValue('')
    await expect(page.getByLabel('Result')).toHaveCount(0)
    expect(operations).toHaveLength(1)
    expect(operations[0].body.family).toBe('translation')

    await page.getByRole('link', { name: 'Translation' }).click()
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    await expect(page.getByText('Up to date')).toBeVisible()
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(usageBefore + 46)
    expect(operations).toHaveLength(1)
  } finally {
    await context.close()
  }
})

test('a rewrite submission settles hidden without touching the translation page', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await page.goto('/rewrite')
    await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()

    await page.getByLabel('Source text').fill(W_OK)
    await expect(page.getByText(/characters used/)).toBeVisible()
    const usageBefore = parseConsumed(await usageLine(page))

    await page.getByRole('button', { name: 'Rewrite' }).click()
    await page.getByRole('link', { name: 'Translation' }).click()

    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
    await expect(page.getByLabel('Source text')).toHaveValue('')
    expect(operations).toHaveLength(1)
    expect(operations[0].body.family).toBe('rewriting')

    await page.getByRole('link', { name: 'Rewriting' }).click()
    await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    await expect(page.getByText('Up to date')).toBeVisible()
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(usageBefore + 46)
    expect(operations).toHaveLength(1)
    expect(await page.getByLabel('Source text').inputValue()).toBe(W_OK)
  } finally {
    await context.close()
  }
})

test('a later explicit translation replaces a preexisting result edit', async ({ browser }) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()

    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TOK_A)
    await expect(page.getByText(/characters used/)).toBeVisible()
    const usageBefore = parseConsumed(await usageLine(page))
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)

    await page.getByLabel('Result').fill(`${RO_FIXTURE} Edited.`)
    await page.waitForTimeout(500)
    expect(operations).toHaveLength(1)

    await page.getByLabel('Source text').fill(`${TOK_A} More.`)
    await expect(page.getByText(/Previous result shown/)).toBeVisible()
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    await expect(page.getByText('Up to date')).toBeVisible()
    expect(operations).toHaveLength(2)
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(usageBefore + 46 + 52)
  } finally {
    await context.close()
  }
})
