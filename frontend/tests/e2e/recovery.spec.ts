import { expect, test, type Page } from '@playwright/test'

const SYNTHETIC_USER_ID = 'm014-e2e-synthetic-user'
const SYNTHETIC_CODE = 'bTAxNC1lMmUtc3ludGhldGlj'
const SYNTHETIC_PASSWORD = 'Juniper!Harbor2026'

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

async function storedMaterial(page: Page): Promise<string> {
  return page.evaluate(() => {
    const values: string[] = []
    for (let index = 0; index < window.localStorage.length; index += 1) {
      const key = window.localStorage.key(index)
      if (key !== null) values.push(window.localStorage.getItem(key) ?? '')
    }
    for (let index = 0; index < window.sessionStorage.length; index += 1) {
      const key = window.sessionStorage.key(index)
      if (key !== null) values.push(window.sessionStorage.getItem(key) ?? '')
    }
    return values.join('\n')
  })
}

test('unknown-email recovery request returns the real 202 acknowledgment with heading focus', async ({
  page,
}) => {
  const observed = accountRequests(page)
  const forgotPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/forgot-password') && request.method() === 'POST') {
      forgotPosts.push(request.url())
    }
  })

  const email = uniqueEmail('m014-unknown')
  await page.goto('/forgot-password')
  await expect(page.getByRole('heading', { name: 'Forgot password' })).toBeFocused()

  await page.getByLabel('Email').fill(email)
  await page.getByRole('button', { name: 'Send reset link' }).click()

  await expect(
    page.getByText('If an account exists for that email, we sent a reset link.'),
  ).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Forgot password' })).toBeFocused()
  await expect(page.getByRole('link', { name: 'Back to sign in' })).toHaveAttribute(
    'href',
    '/login',
  )
  expect(forgotPosts).toHaveLength(1)
  for (const entry of observed) {
    expect(entry.startsWith('POST /api/accounts/') || entry.startsWith('GET /api/accounts/')).toBe(
      true,
    )
  }
})

test('invalid recovery email sends no request and links the field error', async ({ page }) => {
  const forgotPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/forgot-password')) {
      forgotPosts.push(request.url())
    }
  })

  await page.goto('/forgot-password')
  await page.getByLabel('Email').fill('not-an-email')
  await page.getByRole('button', { name: 'Send reset link' }).click()

  await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
  await expect(page.getByLabel('Email')).toBeFocused()
  await expect(page).toHaveURL(/\/forgot-password$/)
  expect(forgotPosts).toHaveLength(0)
})

test('missing reset link shows the recovery action with no password fields', async ({ page }) => {
  const resetPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/reset-password')) {
      resetPosts.push(request.url())
    }
  })

  await page.goto('/reset-password')
  await expect(page.getByText('This reset link is invalid or has expired.')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Request a new reset link' })).toHaveAttribute(
    'href',
    '/forgot-password',
  )
  await expect(page.getByLabel('New password')).toHaveCount(0)
  await expect(page).toHaveURL(/\/reset-password$/)
  expect(resetPosts).toHaveLength(0)
})

test('synthetic reset material returns the real generic 400 and strips the URL', async ({
  page,
}) => {
  const observed = accountRequests(page)
  const resetPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/reset-password') && request.method() === 'POST') {
      resetPosts.push(request.url())
    }
  })

  await page.goto(`/reset-password?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`)
  await expect(page.getByRole('form', { name: 'Reset password' })).toBeVisible()
  await expect(page).toHaveURL(/\/reset-password$/)
  expect(page.url()).not.toContain(SYNTHETIC_USER_ID)
  expect(page.url()).not.toContain(SYNTHETIC_CODE)

  await page.getByLabel('New password').fill(SYNTHETIC_PASSWORD)
  await page.getByLabel('Confirm password').fill(SYNTHETIC_PASSWORD)
  await page.getByRole('button', { name: 'Reset password' }).click()

  await expect(page.getByText('This reset link is invalid or has expired.')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Request a new reset link' })).toBeVisible()
  await expect(page.getByLabel('New password')).toHaveCount(0)

  expect(resetPosts).toHaveLength(1)
  for (const entry of observed) {
    expect(entry.startsWith('POST /api/accounts/') || entry.startsWith('GET /api/accounts/')).toBe(
      true,
    )
  }

  const storage = await storedMaterial(page)
  expect(storage).not.toContain(SYNTHETIC_USER_ID)
  expect(storage).not.toContain(SYNTHETIC_CODE)
  expect(storage).not.toContain(SYNTHETIC_PASSWORD)
})

test('empty reset submission sends no request and focuses the first error', async ({ page }) => {
  const resetPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/reset-password')) {
      resetPosts.push(request.url())
    }
  })

  await page.goto(`/reset-password?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`)
  await page.getByRole('button', { name: 'Reset password' }).click()

  await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
  await expect(page.getByLabel('New password')).toBeFocused()
  await expect(page).toHaveURL(/\/reset-password$/)
  expect(resetPosts).toHaveLength(0)
})

for (const viewport of [
  { width: 390, height: 844 },
  { width: 320, height: 720 },
]) {
  test(`recovery reflows at ${viewport.width} CSS pixels without clipping either form`, async ({
    page,
  }) => {
    await page.setViewportSize(viewport)
    await page.emulateMedia({ reducedMotion: 'reduce' })

    await page.goto('/forgot-password')
    await expect(page.getByRole('button', { name: 'Send reset link' })).toBeInViewport()
    await expect(page.getByRole('link', { name: 'Back to sign in' })).toBeInViewport()

    await page.goto(`/reset-password?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`)
    await expect(page.getByRole('button', { name: 'Reset password' })).toBeInViewport()
    await expect(page.getByRole('button', { name: 'Show password' })).toBeInViewport()

    const geometry = await page.evaluate(() => ({
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth,
    }))
    expect(geometry.scrollWidth).toBeLessThanOrEqual(geometry.clientWidth)
  })
}
