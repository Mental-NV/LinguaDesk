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
 * M031 published recovery journey (AC-001–AC-007): the predefined verified E2E
 * user distinguishes definitive failure, unavailable usage and unknown outcome
 * on both workspaces through the published SPA, real cookie auth, real API,
 * migrated Smoke SQLite and durable accounting with the Smoke deterministic
 * provider adapter. Every case asserts its browser-visible outcome through
 * semantic locators; request counts and per-page usage deltas supplement but
 * never replace the UI assertion. Cases run sequentially in declaration order
 * against the single predefined account shared with the sibling published
 * suites, so every charge is asserted as a delta observed in its own page.
 * Offline simulation uses browser-level `context.setOffline` (no `/api`
 * interception): the unknown-outcome submit genuinely never reaches the
 * server, so Check status deterministically observes the no-record path.
 */

const TOK_A = 'Hello, the meeting starts at 14:30. Please go.'
const RO_FIXTURE = 'Bună, întâlnirea începe la 14:30. Te rog să mergi.'
const TOK_B = 'The report is ready.'
const W_OK = 'The report is really ready. We sends it today.'
const W_RESULT = 'The report is ready. We send it today.'
const W_CHANGED = 'The summary is really complete. She go now.'

const UNKNOWN_COPY =
  'We couldn’t confirm whether this request completed. Your text is safe. Check its status before trying again.'
const NO_RECORD_COPY =
  'We found no record of this request, so its outcome is unknown. Your text is safe. You can submit it as a new request.'
const FAILURE_COPY = 'We couldn’t process this text. Your text and previous result are safe.'
const USAGE_UNAVAILABLE_COPY = 'Usage update unavailable'

const SIGN_IN_PASSWORD = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
const VERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL

if (!SIGN_IN_PASSWORD || !VERIFIED_EMAIL) {
  throw new Error('The published operation-recovery smoke account configuration is required.')
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

function trackReads(page: Page): string[] {
  const observed: string[] = []
  page.on('request', (request) => {
    const url = new URL(request.url())
    if (request.method() === 'GET' && (url.pathname === '/api/usage' || url.pathname.startsWith('/api/operations/'))) {
      observed.push(`GET ${url.pathname}`)
    }
  })
  return observed
}

async function openTranslate(page: Page): Promise<void> {
  await page.goto('/translate')
  await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
  await expect(page.getByLabel('Source text')).toBeVisible()
}

async function openRewrite(page: Page): Promise<void> {
  await page.goto('/rewrite')
  await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
  await expect(page.getByLabel('Source text')).toBeVisible()
}

async function usageLine(page: Page): Promise<string> {
  return (await page.getByRole('region', { name: 'Usage' }).innerText()).trim()
}

test('the login form signs the predefined user into the translation workspace', async ({
  page,
}) => {
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()
  await page.getByLabel('Email').fill(VERIFIED_EMAIL)
  await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page).toHaveURL(/\/translate$/)
  await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
  await expect(page.getByLabel('Source text')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Translate' })).toBeVisible()
})

test('translate definitive failure preserves work and retries once with a new operation key', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await openTranslate(page)

    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TOK_A)
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    const usageBefore = await usageLine(page)
    const baselineOperations = operations.length

    await page.getByLabel('Source text').fill(`${TOK_A} M028-PROCESSING-FAILURE`)
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect(page.getByText(FAILURE_COPY)).toBeVisible()
    await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Check status' })).toHaveCount(0)
    await expect(page.getByLabel('Source text')).toHaveValue(`${TOK_A} M028-PROCESSING-FAILURE`)
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    expect(await usageLine(page)).toBe(usageBefore)

    await page.getByRole('button', { name: 'Try again' }).click()
    await expect.poll(() => operations.length, { timeout: 10_000 }).toBe(baselineOperations + 2)
    await expect(page.getByText(FAILURE_COPY)).toBeVisible()
    expect(operations[operations.length - 1].body.operationId).not.toBe(
      operations[operations.length - 2].body.operationId,
    )
    expect(await usageLine(page)).toBe(usageBefore)

    await page.getByLabel('Source text').fill(TOK_B)
    await expect(page.getByText(FAILURE_COPY)).toHaveCount(0)
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect.poll(() => operations.length, { timeout: 10_000 }).toBe(baselineOperations + 3)
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    const consumedBefore = parseConsumed(usageBefore)
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(consumedBefore + TOK_B.length)
  } finally {
    await context.close()
  }
})

test('rewrite definitive failure preserves work and retries once with a new operation key', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await openRewrite(page)

    await page.getByLabel('Writing language').selectOption('en')
    await page.getByLabel('Source text').fill(W_OK)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    const usageBefore = await usageLine(page)
    const baselineOperations = operations.length

    await page.getByLabel('Source text').fill(`${W_OK} M029-PROCESSING-FAILURE`)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByText(FAILURE_COPY)).toBeVisible()
    await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Check status' })).toHaveCount(0)
    await expect(page.getByLabel('Source text')).toHaveValue(`${W_OK} M029-PROCESSING-FAILURE`)
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    expect(await usageLine(page)).toBe(usageBefore)

    await page.getByRole('button', { name: 'Try again' }).click()
    await expect.poll(() => operations.length, { timeout: 10_000 }).toBe(baselineOperations + 2)
    await expect(page.getByText(FAILURE_COPY)).toBeVisible()
    expect(operations[operations.length - 1].body.operationId).not.toBe(
      operations[operations.length - 2].body.operationId,
    )
    expect(await usageLine(page)).toBe(usageBefore)

    await page.getByLabel('Source text').fill(W_CHANGED)
    await expect(page.getByText(FAILURE_COPY)).toHaveCount(0)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect.poll(() => operations.length, { timeout: 10_000 }).toBe(baselineOperations + 3)
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    const consumedBefore = parseConsumed(usageBefore)
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(consumedBefore + W_CHANGED.length)
  } finally {
    await context.close()
  }
})

test('translate interruption shows unknown outcome and Check status resolves no-record read-only', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    const reads = trackReads(page)
    await openTranslate(page)

    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TOK_A)
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    const usageBefore = await usageLine(page)
    const consumedBefore = parseConsumed(usageBefore)

    await page.getByLabel('Source text').fill(TOK_B)
    await context.setOffline(true)
    try {
      await page.getByRole('button', { name: 'Translate' }).click()
      await expect(page.getByText(UNKNOWN_COPY)).toBeVisible()
    } finally {
      await context.setOffline(false)
    }
    await expect(page.getByRole('button', { name: 'Check status' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Try again' })).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Translate' })).toBeDisabled()
    await expect(page.getByLabel('Source text')).toHaveValue(TOK_B)
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    // Neither success nor zero charge is asserted while the outcome is unknown.
    expect(await usageLine(page)).toBe(usageBefore)
    // Baselines are captured after the offline attempt: the attempt itself may
    // still surface as an issued browser request even though it never reached
    // the server (the 404 below proves no record exists there).
    const operationsAfterUnknown = operations.length
    const readsAfterUnknown = reads.length

    // Check status reads the original identity only: zero operation posts.
    await page.getByRole('button', { name: 'Check status' }).click()
    await expect(page.getByText(NO_RECORD_COPY)).toBeVisible()
    await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Translate' })).toBeEnabled()
    expect(operations.length).toBe(operationsAfterUnknown)
    expect(reads.length).toBe(readsAfterUnknown + 1)
    expect(reads[reads.length - 1].startsWith('GET /api/operations/')).toBe(true)

    // Only now is an explicit new submission permitted; it charges once.
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect.poll(() => operations.length, { timeout: 10_000 }).toBe(operationsAfterUnknown + 1)
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(consumedBefore + TOK_B.length)
  } finally {
    await context.close()
  }
})

test('rewrite interruption shows unknown outcome and Check status resolves no-record read-only', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    const reads = trackReads(page)
    await openRewrite(page)

    await page.getByLabel('Writing language').selectOption('en')
    await page.getByLabel('Source text').fill(W_OK)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    const usageBefore = await usageLine(page)
    const consumedBefore = parseConsumed(usageBefore)

    await page.getByLabel('Source text').fill(W_CHANGED)
    await context.setOffline(true)
    try {
      await page.getByRole('button', { name: 'Rewrite' }).click()
      await expect(page.getByText(UNKNOWN_COPY)).toBeVisible()
    } finally {
      await context.setOffline(false)
    }
    await expect(page.getByRole('button', { name: 'Check status' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Try again' })).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Rewrite' })).toBeDisabled()
    await expect(page.getByLabel('Source text')).toHaveValue(W_CHANGED)
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    expect(await usageLine(page)).toBe(usageBefore)
    // Baselines are captured after the offline attempt: the attempt itself may
    // still surface as an issued browser request even though it never reached
    // the server (the 404 below proves no record exists there).
    const operationsAfterUnknown = operations.length
    const readsAfterUnknown = reads.length

    await page.getByRole('button', { name: 'Check status' }).click()
    await expect(page.getByText(NO_RECORD_COPY)).toBeVisible()
    await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Rewrite' })).toBeEnabled()
    expect(operations.length).toBe(operationsAfterUnknown)
    expect(reads.length).toBe(readsAfterUnknown + 1)
    expect(reads[reads.length - 1].startsWith('GET /api/operations/')).toBe(true)

    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect.poll(() => operations.length, { timeout: 10_000 }).toBe(operationsAfterUnknown + 1)
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    await expect
      .poll(async () => parseConsumed(await usageLine(page)), { timeout: 10_000 })
      .toBe(consumedBefore + W_CHANGED.length)
  } finally {
    await context.close()
  }
})

test('translate Refresh usage sends a usage read only and recovers from an unavailable update', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    const reads = trackReads(page)
    await openTranslate(page)

    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TOK_A)
    await page.getByRole('button', { name: 'Translate' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)
    const usageBefore = await usageLine(page)
    const operationsAfterSuccess = operations.length

    // A successful refresh replaces the counter with zero operation posts.
    const readsBeforeRefresh = reads.length
    await page.getByRole('button', { name: 'Refresh usage' }).click()
    await expect.poll(() => reads.length, { timeout: 10_000 }).toBe(readsBeforeRefresh + 1)
    expect(reads[reads.length - 1]).toBe('GET /api/usage')
    expect(operations.length).toBe(operationsAfterSuccess)
    expect(await usageLine(page)).toBe(usageBefore)

    // A failed refresh preserves the result and reports the unavailable update.
    await context.setOffline(true)
    try {
      await page.getByRole('button', { name: 'Refresh usage' }).click()
      await expect(page.getByText(USAGE_UNAVAILABLE_COPY)).toBeVisible()
    } finally {
      await context.setOffline(false)
    }
    const operationsAfterUnavailable = operations.length
    await expect(page.getByLabel('Result')).toHaveValue(RO_FIXTURE)

    // Recovery sends a usage read only and replaces the counter.
    await page.getByRole('button', { name: 'Refresh usage' }).click()
    await expect(page.getByText(USAGE_UNAVAILABLE_COPY)).toHaveCount(0)
    expect(operations.length).toBe(operationsAfterUnavailable)
    expect(await usageLine(page)).toBe(usageBefore)
  } finally {
    await context.close()
  }
})

test('rewrite Refresh usage sends a usage read only', async ({ browser }) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    const reads = trackReads(page)
    await openRewrite(page)

    await page.getByLabel('Writing language').selectOption('en')
    await page.getByLabel('Source text').fill(W_OK)
    await page.getByRole('button', { name: 'Rewrite' }).click()
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
    const usageBefore = await usageLine(page)
    const operationsAfterSuccess = operations.length

    const readsBeforeRefresh = reads.length
    await page.getByRole('button', { name: 'Refresh usage' }).click()
    await expect.poll(() => reads.length, { timeout: 10_000 }).toBe(readsBeforeRefresh + 1)
    expect(reads[reads.length - 1]).toBe('GET /api/usage')
    expect(operations.length).toBe(operationsAfterSuccess)
    expect(await usageLine(page)).toBe(usageBefore)
    await expect(page.getByLabel('Result')).toHaveValue(W_RESULT)
  } finally {
    await context.close()
  }
})
