import { expect, test, type Page } from '@playwright/test'

const REGISTER_PASSWORD = 'Maple!River2026'
const SYNTHETIC_USER_ID = 'm012-synthetic-user'
const SYNTHETIC_CODE = 'c3ludGhldGljLWNvZGU'

function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.test`
}

function accountRequests(page: Page): string[] {
  const observed: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/')) {
      observed.push(`${request.method()} ${new URL(request.url()).pathname}`)
    }
  })
  return observed
}

async function fillRegistrationForm(page: Page, email: string) {
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password', { exact: true }).fill(REGISTER_PASSWORD)
  await page.getByLabel('Confirm password').fill(REGISTER_PASSWORD)
}

async function registerAndEnterVerifyPage(page: Page, email: string) {
  await page.goto('/register')
  await fillRegistrationForm(page, email)
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page).toHaveURL(/\/verify-email$/)
  await expect(page.getByRole('heading', { name: 'Verify your email' })).toBeFocused()
  await expect(page.getByText('Check your email to verify your account.')).toBeVisible()
  await expect(page.getByText(email)).toBeVisible()
}

test('invalid verification link returns the real generic 400 and shows the resend path', async ({
  page,
}) => {
  const observed = accountRequests(page)
  const confirmPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/confirm-email') && request.method() === 'POST') {
      confirmPosts.push(request.url())
    }
  })

  await page.goto(`/verify-email?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`)
  await expect(page.getByText('This verification link is invalid or has expired.')).toBeVisible()
  await expect(
    page.getByRole('button', { name: 'Send a new verification email' }),
  ).toBeVisible()

  expect(confirmPosts).toHaveLength(1)
  expect(page.url()).not.toContain(SYNTHETIC_USER_ID)
  expect(page.url()).not.toContain(SYNTHETIC_CODE)
  await expect(page).toHaveURL(/\/verify-email$/)
  for (const entry of observed) {
    expect(entry.startsWith('POST /api/accounts/') || entry.startsWith('GET /api/accounts/')).toBe(
      true,
    )
  }
})

test('real resend returns 202 with server cooldown behavior', async ({ page }) => {
  const email = uniqueEmail('m012-resend')
  await registerAndEnterVerifyPage(page, email)

  const resendPosts: string[] = []
  let resendBody: unknown = null
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/resend-verification') && request.method() === 'POST') {
      resendPosts.push(request.url())
      resendBody = request.postDataJSON()
    }
  })

  await page.getByRole('button', { name: 'Send a new verification email' }).click()
  await expect(page.getByText('Verification email sent.')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Send a new verification email' })).toBeDisabled()
  await expect(page.getByText(/You can request another email in \d+ seconds\./)).toBeVisible()

  expect(resendPosts).toHaveLength(1)
  expect(resendBody).toEqual({ email })
})

test('direct no-material entry routes away and reload clears query state', async ({ page }) => {
  await page.goto('/verify-email')
  await expect(page).toHaveURL(/\/register$/)

  await page.goto(`/verify-email?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`)
  await expect(page.getByText('This verification link is invalid or has expired.')).toBeVisible()
  await expect(page).toHaveURL(/\/verify-email$/)

  await page.reload()
  await expect(page).toHaveURL(/\/register$/)
  await expect(page.getByLabel('Password', { exact: true })).toHaveValue('')
})

test('unverified protected navigation stays on verification without language work', async ({
  page,
}) => {
  const email = uniqueEmail('m012-guarded')
  const observed = accountRequests(page)
  await registerAndEnterVerifyPage(page, email)

  await page.getByRole('link', { name: 'Translation' }).click()
  await expect(page).toHaveURL(/\/verify-email$/)
  await expect(page.getByRole('heading', { name: 'Verify your email' })).toBeFocused()

  await page.getByRole('link', { name: 'Rewriting' }).click()
  await expect(page).toHaveURL(/\/verify-email$/)
  for (const entry of observed) {
    expect(entry).not.toContain('/api/translate')
    expect(entry).not.toContain('/api/rewrite')
  }
})

for (const viewport of [
  { width: 390, height: 844 },
  { width: 320, height: 720 },
]) {
  test(`verification reflows at ${viewport.width} CSS pixels without clipping controls`, async ({
    page,
  }) => {
    await page.setViewportSize(viewport)
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.goto(`/verify-email?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`)

    await expect(page.getByText('This verification link is invalid or has expired.')).toBeVisible()
    await expect(
      page.getByRole('button', { name: 'Send a new verification email' }),
    ).toBeInViewport()
    await expect(page.getByRole('button', { name: 'I’ve verified my email' })).toBeInViewport()

    const geometry = await page.evaluate(() => ({
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth,
    }))
    expect(geometry.scrollWidth).toBeLessThanOrEqual(geometry.clientWidth)
  })
}
