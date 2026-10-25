import { expectCleanScan } from './accessibility'
import {
  expect,
  test,
  type Browser,
  type BrowserContext,
  type Page,
} from '@playwright/test'
import { verifiedAccountStorageState } from './translate-auth'

/**
 * M038 automated semantic/contrast scans (AC-005 automated part, V-011):
 * dependency-free browser checks over both workspaces and the selected
 * auth states at desktop size. Every button must expose an accessible name,
 * every form control must be labelled, the document must declare its
 * language, and visible text must meet WCAG contrast (4.5:1 ordinary,
 * 3:1 large text) against its effective background. Any violation fails the
 * run with the offending element identified — there is no serious/critical
 * budget. This scan is independent of screenshots. Additional engines/devices,
 * human review and AT observations are optional under D-22.
 */

const SIGN_IN_PASSWORD = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
const VERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL

if (!SIGN_IN_PASSWORD || !VERIFIED_EMAIL) {
  throw new Error('The published desktop scan smoke account configuration is required.')
}

let authenticatedState: Awaited<ReturnType<typeof verifiedAccountStorageState>> | null = null

test.beforeAll(async ({ browser }) => {
  authenticatedState = await verifiedAccountStorageState(browser)
})

async function verifiedWorkspace(browser: Browser): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext({
    storageState: authenticatedState ?? undefined,
    viewport: { width: 1440, height: 900 },
  })
  const page = await context.newPage()
  return { context, page }
}

test('translate workspace states scan clean at desktop', async ({ browser }) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation', exact: true })).toBeFocused()
    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill('Hello, the meeting starts at 14:30. Please go.')
    await expectCleanScan(page, 'translate ready')

    await page.getByLabel('Source text').fill('a'.repeat(5312))
    await expect(
      page.getByText('Translation is limited to 5,000 characters. Shorten the text to continue.'),
    ).toBeVisible()
    await expectCleanScan(page, 'translate oversize error')

    // Keyboard focus must remain visible on the native Clear control.
    await page.getByLabel('Source text').fill('Hello.')
    await page.getByRole('button', { name: 'Clear source text' }).focus()
    const ring = await page.getByRole('button', { name: 'Clear source text' }).evaluate((element) => {
      const style = getComputedStyle(element)
      return { outlineWidth: style.outlineWidth, outlineStyle: style.outlineStyle }
    })
    expect(
      `${ring.outlineStyle} ${ring.outlineWidth}`,
      'focused Clear control keeps a visible focus indicator',
    ).not.toBe('none 0px')
  } finally {
    await context.close()
  }
})

test('rewrite workspace scans clean at desktop', async ({ browser }) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    await page.goto('/rewrite')
    await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
    await page.getByLabel('Source text').fill('The report is really ready. We sends it today.')
    await expectCleanScan(page, 'rewrite ready')
  } finally {
    await context.close()
  }
})

test('register and login states scan clean at desktop', async ({ browser }) => {
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } })
  const page = await context.newPage()
  try {
    await page.goto('/register')
    await expect(page.getByRole('heading', { name: 'Create account' })).toBeFocused()
    await page.getByLabel('Email').fill('not-an-email')
    await page.getByLabel('Password', { exact: true }).fill('short')
    await page.getByLabel('Confirm password').fill('mismatch')
    await page.getByRole('button', { name: 'Create account' }).click()
    await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
    await expectCleanScan(page, 'register long validation')

    await page.goto('/login')
    await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()
    await expectCleanScan(page, 'login')
  } finally {
    await context.close()
  }
})
