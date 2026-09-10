import { expect, test, type Page } from '@playwright/test'

const REGISTER_EMAIL = 'm011-visitor@example.test'
const REGISTER_PASSWORD = 'Maple!River2026'

async function fillRegistrationForm(page: Page) {
  await page.getByLabel('Email').fill(REGISTER_EMAIL)
  await page.getByLabel('Password', { exact: true }).fill(REGISTER_PASSWORD)
  await page.getByLabel('Confirm password').fill(REGISTER_PASSWORD)
}

test('registers once through the real API and reaches verification entry', async ({ page }) => {
  const registerPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/register') && request.method() === 'POST') {
      registerPosts.push(request.url())
    }
  })

  await page.goto('/register')
  await expect(page.getByRole('heading', { name: 'Create account' })).toBeFocused()
  await fillRegistrationForm(page)
  await page.getByRole('button', { name: 'Create account' }).click()

  await expect(page).toHaveURL(/\/verify-email$/)
  await expect(page.getByRole('heading', { name: 'Verify your email' })).toBeFocused()
  await expect(page.getByText('Check your email to verify your account.')).toBeVisible()
  await expect(page.getByText(REGISTER_EMAIL)).toBeVisible()
  await expect(page.getByLabel('Password')).toHaveCount(0)
  expect(registerPosts).toHaveLength(1)
})

test('invalid local input sends no registration request', async ({ page }) => {
  const registerPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/register')) {
      registerPosts.push(`${request.method()} ${request.url()}`)
    }
  })

  await page.goto('/register')
  await page.getByLabel('Email').fill('not-an-email')
  await page.getByLabel('Password', { exact: true }).fill('short')
  await page.getByLabel('Confirm password').fill('short')
  await page.getByRole('button', { name: 'Create account' }).click()

  await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
  await expect(page).toHaveURL(/\/register$/)
  expect(registerPosts).toHaveLength(0)
})

test('reload clears password state and the in-memory verification entry', async ({ page }) => {
  await page.goto('/register')
  await page.getByLabel('Password', { exact: true }).fill(REGISTER_PASSWORD)
  await page.reload()
  await expect(page.getByLabel('Password', { exact: true })).toHaveValue('')

  await fillRegistrationForm(page)
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page).toHaveURL(/\/verify-email$/)

  await page.reload()
  await expect(page).toHaveURL(/\/register$/)
  await expect(page.getByLabel('Password', { exact: true })).toHaveValue('')
  await expect(page.getByLabel('Email')).toHaveValue('')
})

for (const viewport of [
  { width: 390, height: 844 },
  { width: 320, height: 720 },
]) {
  test(`registration reflows at ${viewport.width} CSS pixels without clipping errors`, async ({ page }) => {
    await page.setViewportSize(viewport)
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.goto('/register')

    await page.getByRole('button', { name: 'Create account' }).click()
    await expect(page.getByText('Check the highlighted fields.')).toBeVisible()

    const geometry = await page.evaluate(() => ({
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth,
    }))
    expect(geometry.scrollWidth).toBeLessThanOrEqual(geometry.clientWidth)
    await expect(page.getByText('Check the highlighted fields.')).toBeInViewport()
  })
}
