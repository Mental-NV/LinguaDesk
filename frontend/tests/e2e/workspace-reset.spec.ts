import { logOut } from './workspace-fixture'
import {
  expect,
  test,
  type Browser,
  type BrowserContext,
  type Page,
} from '@playwright/test'
import { verifiedAccountStorageState } from './translate-auth'

/**
 * M044 published lifetime teardown regression: the predefined verified
 * E2E user ends the workspace safely through the published SPA, real cookie
 * auth, real API, migrated Smoke SQLite and durable accounting with the Smoke
 * deterministic provider adapter. Every case asserts its browser-visible
 * outcome through semantic locators; request counts and storage/URL checks
 * supplement but never replace the UI assertion. Cases run sequentially in
 * declaration order against the single predefined account; no case submits a
 * transformation, so usage never moves and zero operation posts is exact.
 */

const TOK_A = 'Hello, the meeting starts at 14:30. Please go.'
const W_OK = 'The report is really ready. We sends it today.'
const SENTINEL = 'M032-PRIVACY-SENTINEL'

const SIGN_IN_PASSWORD = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
const VERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL

if (!SIGN_IN_PASSWORD || !VERIFIED_EMAIL) {
  throw new Error('The published workspace-lifetime smoke account configuration is required.')
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
}

function trackOperations(page: Page): OperationPost[] {
  const observed: OperationPost[] = []
  page.on('request', (request) => {
    const url = new URL(request.url())
    if (url.pathname === '/api/operations' && request.method() === 'POST') {
      observed.push({ method: request.method(), url: url.pathname })
    }
  })
  return observed
}

async function fillBothWorkspaces(page: Page): Promise<void> {
  await page.goto('/translate')
  await expect(page.getByRole('heading', { name: 'Translator', exact: true })).toBeFocused()
  await page.getByLabel('Source language').selectOption('en')
  await page.getByLabel('Target language').selectOption('ro')
  await page.getByLabel('Source text', { exact: true }).fill(TOK_A)

  await page.getByRole('link', { name: 'Write' }).click()
  await expect(page.getByRole('heading', { name: 'Write' })).toBeFocused()
  await page.getByLabel('Source text', { exact: true }).fill(W_OK)

  await page.getByRole('link', { name: 'Translator' }).click()
  await expect(page.getByRole('heading', { name: 'Translator', exact: true })).toBeFocused()
  await expect(page.getByLabel('Source text', { exact: true })).toHaveValue(TOK_A)
}

async function expectCleanClientState(page: Page, sentinel: string): Promise<void> {
  const state = await page.evaluate(() => ({
    localStorageLength: window.localStorage.length,
    sessionStorageLength: window.sessionStorage.length,
    url: window.location.href,
  }))
  expect(state.localStorageLength).toBe(0)
  expect(state.sessionStorageLength).toBe(0)
  expect(state.url).not.toContain(sentinel)
}

test('reload, full-document navigation and restoration clear private text', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await fillBothWorkspaces(page)
    await expectCleanClientState(page, TOK_A.slice(0, 12))

    await page.reload()
    await expect(page.getByRole('heading', { name: 'Translator', exact: true })).toBeFocused()
    await expect(page.getByLabel('Source text', { exact: true })).toHaveValue('')
    await expect(page.getByLabel('Source language')).toHaveValue('auto')
    await expect(page.getByLabel('Target language')).toHaveValue('')
    await expect(page.getByRole('textbox', { name: /Translation result|Improved result/ })).toHaveValue('')
    await expectCleanClientState(page, TOK_A.slice(0, 12))

    await fillBothWorkspaces(page)
    await page.goto('/rewrite')
    await expect(page.getByRole('heading', { name: 'Write' })).toBeFocused()
    await expect(page.getByLabel('Source text', { exact: true })).toHaveValue('')
    await expect(page.getByTestId('write-result')).toHaveText('')

    await page.goBack()
    await expect(page.getByRole('heading', { name: 'Translator', exact: true })).toBeFocused()
    await expect(page.getByLabel('Source text', { exact: true })).toHaveValue('')
    await expect(page.getByRole('textbox', { name: /Translation result|Improved result/ })).toHaveValue('')
    await expectCleanClientState(page, TOK_A.slice(0, 12))

    const secondTab = await context.newPage()
    try {
      await secondTab.goto('/translate')
      await expect(
        secondTab.getByRole('heading', { name: 'Translator', exact: true }),
      ).toBeFocused()
      await expect(secondTab.getByLabel('Source text', { exact: true })).toHaveValue('')
    } finally {
      await secondTab.close()
    }
    expect(operations).toHaveLength(0)
  } finally {
    await context.close()
  }
})

test('sign-out clears immediately and Back exposes no text', async ({ browser }) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await fillBothWorkspaces(page)

    await logOut(page)
    // The transient `Signing out…` copy is proven deterministically with a
    // gated sign-out response in `tests/unit/login.test.tsx` (M013); the
    // published path asserts the immediate clear and the settled form.
    await expect(page).toHaveURL(/\/login$/)
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible()
    await expect(page.getByLabel('Source text', { exact: true })).toHaveCount(0)
    await expectCleanClientState(page, TOK_A.slice(0, 12))

    await page.goto('/translate')
    await expect(page).toHaveURL(/\/login$/)
    await expect(page.locator('textarea')).toHaveCount(0)
    expect(operations).toHaveLength(0)
  } finally {
    await context.close()
  }
})

test('an expired session shows MSG-037 with a clean workspace', async ({ browser }) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const operations = trackOperations(page)
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translator', exact: true })).toBeFocused()
    await page.getByLabel('Source text', { exact: true }).fill(TOK_A)

    await context.clearCookies()
    await page.reload()
    await expect(page).toHaveURL(/\/login$/)
    await expect(
      page.getByText('Your session expired. Sign in again to continue.'),
    ).toBeVisible()
    await expect(page.getByLabel('Password')).toHaveValue('')
    await expectCleanClientState(page, TOK_A.slice(0, 12))

    await page.getByLabel('Email').fill(VERIFIED_EMAIL)
    await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page).toHaveURL(/\/translate$/)
    await expect(page.getByLabel('Source text', { exact: true })).toHaveValue('')
    expect(operations).toHaveLength(0)
  } finally {
    await context.close()
  }
})

test('both pages state the workspace lifetime with no client-side text residue', async ({
  browser,
}) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    const footer =
      'Text and settings are cleared when this workspace ends, including refresh, sign-out, or session expiry.'
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translator', exact: true })).toBeFocused()
    await page.getByLabel('Source text', { exact: true }).fill(`${TOK_A} ${SENTINEL}`)
    await expect(page.getByText(footer)).toBeVisible()
    await expectCleanClientState(page, SENTINEL)

    await page.getByRole('link', { name: 'Write' }).click()
    await expect(page.getByRole('heading', { name: 'Write' })).toBeFocused()
    await page.getByLabel('Source text', { exact: true }).fill(`${W_OK} ${SENTINEL}`)
    await expect(page.getByText(footer)).toBeVisible()
    await expectCleanClientState(page, SENTINEL)
  } finally {
    await context.close()
  }
})
