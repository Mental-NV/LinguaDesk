import { expect, test } from '@playwright/test'

/**
 * M043 live browser case (AC-006): drives /translate exactly as a user does —
 * real login form, verified account, explicit Translate activation — and
 * asserts a visible Result. Opt-in only: runs under LINGUADESK_E2E_LIVE=1 with
 * LINGUADESK_LIVE_URL plus a verified account created by
 * `scripts/backend.sh e2e-live`. Skipped otherwise; never part of the default
 * gates. Live LLM text varies, so the Result assertion is non-empty with
 * Cyrillic content rather than an exact fixture match.
 */

const LIVE_URL = process.env.LINGUADESK_LIVE_URL
const LIVE_EMAIL = process.env.LINGUADESK_LIVE_EMAIL
const LIVE_PASSWORD = process.env.LINGUADESK_LIVE_PASSWORD
const LIVE_ENABLED =
  process.env.LINGUADESK_E2E_LIVE === '1' && !!LIVE_URL && !!LIVE_EMAIL && !!LIVE_PASSWORD

test.skip(!LIVE_ENABLED, 'Live serving browser case requires LINGUADESK_E2E_LIVE=1 with LINGUADESK_LIVE_URL and a verified account.')

test('live translate through the serving chain shows a visible Result', async ({ page }) => {
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()

  await page.getByLabel('Email').fill(LIVE_EMAIL!)
  await page.getByLabel('Password').fill(LIVE_PASSWORD!)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).not.toHaveURL(/\/login$/)

  await page.goto('/translate')
  await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()

  await page.getByLabel('Source language').selectOption('en')
  await page.getByLabel('Target language').selectOption('ru')
  await page.getByLabel('Source text').fill('Hello.')

  await page.getByRole('button', { name: 'Translate' }).click()

  const result = page.getByLabel('Result')
  await expect
    .poll(async () => (await result.inputValue()).trim(), { timeout: 60_000 })
    .not.toBe('')
  const text = (await result.inputValue()).trim()
  expect(text.length).toBeGreaterThan(0)
  expect(/[\u0400-\u04FF]/.test(text)).toBe(true)
})
