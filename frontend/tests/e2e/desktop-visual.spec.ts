import {
  expect,
  test,
  type Browser,
  type BrowserContext,
  type Page,
} from '@playwright/test'
import { verifiedAccountStorageState } from './translate-auth'

/**
 * M038 curated desktop baselines (AC-004, V-011 automated slice): three
 * desktop-size screenshots captured with pinned settings — 1440×900 CSS
 * viewport, device scale factor 1, CSS screenshot scale, reduced motion,
 * hidden caret, awaited fonts and deterministic synthetic content — at the
 * section 4.4 starting thresholds (per-pixel 0.1, max differing ratio
 * 0.001). The run-owned usage line is pinned to a fixed representative
 * value during capture because sibling published suites legitimately move
 * the shared seeded baseline and its relative reset wording. No baseline is
 * auto-accepted: first capture uses --update-snapshots, every later change
 * needs a measured reason plus human design-review approval (T005), and no
 * sentence/comparison/rails/sheet baseline is added here. Narrow sizes stay
 * with M039. Baselines captured on the implementation host carry its
 * engine/font provenance in tasks.md; container-pinned re-capture is a
 * recorded follow-up, not a silent substitution.
 */

const TOK_A = 'Hello, the meeting starts at 14:30. Please go.'
const TLONG = 'a'.repeat(5312)

const SIGN_IN_PASSWORD = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
const VERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL

if (!SIGN_IN_PASSWORD || !VERIFIED_EMAIL) {
  throw new Error('The published desktop-visual smoke account configuration is required.')
}

let authenticatedState: Awaited<ReturnType<typeof verifiedAccountStorageState>> | null = null

test.beforeAll(async ({ browser }) => {
  authenticatedState = await verifiedAccountStorageState(browser)
})

async function pinnedDesktopContext(browser: Browser): Promise<{
  context: BrowserContext
  page: Page
}> {
  const context = await browser.newContext({
    storageState: authenticatedState ?? undefined,
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    reducedMotion: 'reduce',
  })
  const page = await context.newPage()
  return { context, page }
}

async function stabilizeForCapture(page: Page): Promise<void> {
  await page.evaluate(() => document.fonts.ready.then(() => undefined))
  await page.addStyleTag({ content: '* { caret-color: transparent !important; }' })
  // The run-owned usage line moves with every sibling suite's legitimate
  // charges and its relative reset wording, which shifts the layout below
  // it. A mask would hide the text but not the shift, so the capture pins a
  // fixed representative line instead: same region, deterministic layout.
  const usage = page.getByRole('region', { name: 'Usage' })
  if ((await usage.count()) > 0) {
    await usage.evaluate((element) => {
      element.innerHTML =
        '<p>7,500 of 20,000 characters used \u00b7 12,500 remaining \u00b7 Resets 00:00 UTC</p>'
    })
  }
}

async function expectDesktopBaseline(page: Page, name: string): Promise<void> {
  await expect(page).toHaveScreenshot(name, {
    animations: 'disabled',
    caret: 'hide',
    scale: 'css',
    threshold: 0.1,
    maxDiffPixelRatio: 0.001,
  })
}

test('translation ready layout matches the desktop baseline', async ({ browser }) => {
  const { context, page } = await pinnedDesktopContext(browser)
  try {
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TOK_A)
    await expect(page.getByText(/characters used/)).toBeVisible()
    await expect(page.getByRole('button', { name: 'Translate' })).toBeEnabled()
    await stabilizeForCapture(page)
    await expectDesktopBaseline(page, 'translation-ready-1440x900.png')
  } finally {
    await context.close()
  }
})

test('translation oversize-error layout matches the desktop baseline', async ({ browser }) => {
  const { context, page } = await pinnedDesktopContext(browser)
  try {
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TLONG)
    await expect(
      page.getByText('Translation is limited to 5,000 characters. Remove 312 characters to continue.'),
    ).toBeVisible()
    await expect(page.getByRole('button', { name: 'Translate' })).toBeDisabled()
    await stabilizeForCapture(page)
    await expectDesktopBaseline(page, 'translation-oversize-error-1440x900.png')
  } finally {
    await context.close()
  }
})

test('registration long-validation layout matches the desktop baseline', async ({
  browser,
}) => {
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    reducedMotion: 'reduce',
  })
  const page = await context.newPage()
  try {
    await page.goto('/register')
    await expect(page.getByRole('heading', { name: 'Create account' })).toBeFocused()
    await page.getByLabel('Email').fill('not-an-email')
    await page.getByLabel('Password', { exact: true }).fill('short')
    await page.getByLabel('Confirm password').fill('mismatch')
    await page.getByRole('button', { name: 'Create account' }).click()
    await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
    const fieldErrors = page.locator('p.field-error')
    await expect(fieldErrors.getByText('Enter a valid email address.', { exact: true })).toBeVisible()
    await expect(fieldErrors.getByText('Password must meet all requirements.', { exact: true })).toBeVisible()
    await expect(fieldErrors.getByText('Passwords do not match.', { exact: true })).toBeVisible()
    await stabilizeForCapture(page)
    await expectDesktopBaseline(page, 'registration-long-validation-1440x900.png')
  } finally {
    await context.close()
  }
})
