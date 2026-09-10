import { expect, test, type Page } from '@playwright/test'

const SIGN_IN_PASSWORD = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
const VERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL
const UNVERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_UNVERIFIED_EMAIL

if (!SIGN_IN_PASSWORD || !VERIFIED_EMAIL || !UNVERIFIED_EMAIL) {
  throw new Error('The published sign-in smoke account configuration is required.')
}

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

test('invalid credentials return the real shared 401 with cleared password and email focus', async ({
  page,
}) => {
  const observed = accountRequests(page)
  const signInPosts: unknown[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/sign-in') && request.method() === 'POST') {
      signInPosts.push(request.postDataJSON())
    }
  })

  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()

  const email = uniqueEmail('m013-unknown')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page.getByText('Email or password is incorrect.')).toBeVisible()
  await expect(page.getByLabel('Password')).toHaveValue('')
  await expect(page.getByRole('textbox', { name: 'Email' })).toHaveValue(email)
  await expect(page.getByLabel('Email')).toBeFocused()
  await expect(page).toHaveURL(/\/login$/)

  expect(signInPosts).toHaveLength(1)
  expect(signInPosts[0]).toEqual({ email, password: SIGN_IN_PASSWORD })
  for (const entry of observed) {
    expect(entry).not.toContain('/api/translate')
    expect(entry).not.toContain('/api/rewrite')
  }
})

test('unverified sign-in reaches verification entry with the typed email', async ({ page }) => {
  const observed = accountRequests(page)
  const email = UNVERIFIED_EMAIL

  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page).toHaveURL(/\/verify-email$/)
  await expect(page.getByRole('heading', { name: 'Verify your email' })).toBeFocused()
  await expect(page.getByRole('textbox', { name: 'Email' })).toHaveValue(email)
  await expect(page.getByLabel('Password')).toHaveCount(0)
  for (const entry of observed) {
    expect(entry).not.toContain('/api/translate')
    expect(entry).not.toContain('/api/rewrite')
  }
})

test('verified sign-in returns to the remembered protected route', async ({ page }) => {
  const observed = accountRequests(page)
  const signInPosts: unknown[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/sign-in') && request.method() === 'POST') {
      signInPosts.push(request.postDataJSON())
    }
  })

  await page.goto('/login')
  await page.getByRole('link', { name: 'Rewriting' }).click()
  await expect(page).toHaveURL(/\/login$/)
  await page.getByLabel('Email').fill(VERIFIED_EMAIL)
  await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page).toHaveURL(/\/rewrite$/)
  await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
  await expect(page.getByLabel('Password')).toHaveCount(0)
  expect(signInPosts).toEqual([{ email: VERIFIED_EMAIL, password: SIGN_IN_PASSWORD }])
  for (const entry of observed) {
    expect(entry).not.toContain('/api/translate')
    expect(entry).not.toContain('/api/rewrite')
  }
})

test('verified session signs out through the real API and Back exposes no authenticated content', async ({
  page,
}) => {
  const signOutPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/sign-out') && request.method() === 'POST') {
      signOutPosts.push(request.url())
    }
  })

  await page.goto('/login')
  await page.getByLabel('Email').fill(VERIFIED_EMAIL)
  await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).toHaveURL(/\/translate$/)

  const signOutResponse = page.waitForResponse((response) =>
    response.url().includes('/api/accounts/sign-out') && response.request().method() === 'POST',
  )
  await page.getByRole('button', { name: 'Sign out' }).click()
  expect((await signOutResponse).status()).toBe(204)
  await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible()
  await expect(page).toHaveURL(/\/login$/)
  expect(signOutPosts).toHaveLength(1)

  await page.goBack()
  await expect(page.getByText(/workspace arrives in a later update/i)).toHaveCount(0)
  expect(page.url()).not.toMatch(/\/(translate|rewrite)$/)
})

test('protected entry without a session shows the expiry notice and reload clears the form', async ({
  page,
}) => {
  await page.goto('/translate')
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByText('Your session expired. Sign in again to continue.')).toBeVisible()

  await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
  await page.reload()
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()
  await expect(page.getByLabel('Password')).toHaveValue('')
  await expect(page.getByLabel('Email')).toHaveValue('')
})

test('invalid local input sends no sign-in request', async ({ page }) => {
  const signInPosts: string[] = []
  page.on('request', (request) => {
    if (request.url().includes('/api/accounts/sign-in')) {
      signInPosts.push(`${request.method()} ${request.url()}`)
    }
  })

  await page.goto('/login')
  await page.getByLabel('Email').fill('not-an-email')
  await page.getByLabel('Password').fill(SIGN_IN_PASSWORD)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
  await expect(page).toHaveURL(/\/login$/)
  expect(signInPosts).toHaveLength(0)
})

for (const viewport of [
  { width: 390, height: 844 },
  { width: 320, height: 720 },
]) {
  test(`sign-in reflows at ${viewport.width} CSS pixels without clipping the form`, async ({
    page,
  }) => {
    await page.setViewportSize(viewport)
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.goto('/login')

    await expect(page.getByRole('button', { name: 'Sign in' })).toBeInViewport()
    await expect(page.getByRole('link', { name: 'Forgot password' })).toBeInViewport()

    const geometry = await page.evaluate(() => ({
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth,
    }))
    expect(geometry.scrollWidth).toBeLessThanOrEqual(geometry.clientWidth)
  })
}
