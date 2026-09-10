import { expect, test } from '@playwright/test'

test('navigates with native history and focuses each destination heading', async ({ page }) => {
  await page.goto('/')
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()

  await page.getByRole('link', { name: 'Create account' }).click()
  await expect(page).toHaveURL(/\/register$/)
  await expect(page.getByRole('heading', { name: 'Create account' })).toBeFocused()

  await page.goBack()
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()
  await page.goForward()
  await expect(page.getByRole('heading', { name: 'Create account' })).toBeFocused()

  await page.reload()
  await expect(page.getByRole('heading', { name: 'Create account' })).toBeFocused()
})

test('routes protected deep links to the sign-in form without workspace controls', async ({ page }) => {
  await page.goto('/translate')
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()
  await expect(page.getByRole('form', { name: 'Sign in' })).toBeVisible()
  await expect(page.getByLabel('Email')).toBeVisible()
  await expect(page.getByLabel('Password')).toBeVisible()
  await expect(page.locator('textarea')).toHaveCount(0)

  await page.goto('/an-unknown-client-page')
  await expect(page.getByRole('heading', { name: 'Page not found' })).toBeFocused()
  await expect(page.getByRole('link', { name: 'Go to sign in' })).toHaveAttribute('href', '/login')
})

test('keeps API, health, asset, file, and method boundaries non-HTML', async ({ request }) => {
  for (const path of ['/api', '/api/missing', '/assets/missing', '/assets/missing.js', '/missing.js', '/health/missing']) {
    const response = await request.get(path)
    expect(response.status(), path).toBe(404)
    expect(response.headers()['content-type'] ?? '', path).not.toContain('text/html')
  }

  const apiResponse = await request.get('/api/missing')
  expect(apiResponse.headers()['content-type']).toContain('application/problem+json')
  const capabilityResponse = await request.get('/api/capabilities')
  expect(capabilityResponse.status()).toBe(200)
  expect(capabilityResponse.headers()['content-type']).toContain('application/json')
  expect(capabilityResponse.headers()['cache-control']).toBe('no-store')
  expect((await capabilityResponse.json()).countingPolicy.id).toBe('unicode-scalar-v1')
  expect((await request.post('/api/capabilities')).status()).toBe(405)
  const healthResponse = await request.get('/health/live')
  expect(healthResponse.status()).toBe(200)
  expect(await healthResponse.text()).toBe('Healthy')
  expect((await request.post('/health/live')).status()).toBe(405)
  expect((await request.post('/register')).headers()['content-type'] ?? '').not.toContain('text/html')
})

test('loads referenced production assets from the published host', async ({ page, request }) => {
  await page.goto('/login')
  const references = await page.locator('script[src], link[rel="stylesheet"][href]').evaluateAll((elements) =>
    elements.map((element) => element.getAttribute('src') ?? element.getAttribute('href')).filter(Boolean) as string[],
  )

  expect(references.length).toBeGreaterThan(0)
  for (const reference of references) {
    const response = await request.get(reference)
    expect(response.ok(), reference).toBeTruthy()
    expect(response.headers()['content-type'] ?? '', reference).not.toContain('text/html')
  }
})

for (const viewport of [
  { width: 390, height: 844 },
  { width: 320, height: 720 },
]) {
  test(`reflows at ${viewport.width} CSS pixels and exposes keyboard focus`, async ({ page }, testInfo) => {
    await page.setViewportSize(viewport)
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.goto('/login')

    const geometry = await page.evaluate(() => ({
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth,
    }))
    expect(geometry.scrollWidth).toBeLessThanOrEqual(geometry.clientWidth)
    await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()

    for (let step = 0; step < 4; step += 1) {
      await page.keyboard.press('Shift+Tab')
    }
    await expect(page.getByRole('link', { name: 'Skip to main content' })).toBeFocused()
    await expect(page.getByRole('link', { name: 'Skip to main content' })).toBeInViewport()
    await page.keyboard.press('Enter')
    await expect(page.getByRole('main')).toBeFocused()
    await page.screenshot({ path: testInfo.outputPath(`shell-${viewport.width}.png`), fullPage: true })
  })
}
