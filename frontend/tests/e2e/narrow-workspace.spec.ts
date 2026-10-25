import { chooseMode, expectResult, test, expect, noOverflow, nativeCopy, trackOperations, usage, TRANSLATE_SOURCE, TRANSLATE_RESULT, REWRITE_SOURCE, REWRITE_RESULT, openWorkspace } from './workspace-fixture'
import { expectCleanScan } from './accessibility'

const email = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL
const password = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
if (!email || !password) throw new Error('The dedicated Smoke account is required.')

for (const feature of ['translate', 'rewrite'] as const) {
  test(`M039 ${feature}: narrow real login, paused result, native copy, failure/retry and expiry`, async ({ page, context }) => {
    await context.grantPermissions(['clipboard-read', 'clipboard-write'])
    await page.setViewportSize({ width: 390, height: 844 })
    await page.clock.install()
    await page.clock.pauseAt(new Date(Date.now() + 1000))
    const posts = trackOperations(page)
    const available = page.waitForResponse(response => new URL(response.url()).pathname === '/api/usage')
    await page.goto(`/${feature}`)
    await expect(page).toHaveURL(/\/login$/)
    await expectCleanScan(page, 'narrow login')
    await page.getByLabel('Email').fill(email!)
    await page.getByLabel('Password').fill(password!)
    await page.getByRole('button', { name: 'Sign in', exact: true }).click()
    await expect(page).toHaveURL(new RegExp(`/${feature}$`))
    expect((await available).status()).toBe(200)
    await page.getByLabel(feature === 'translate' ? 'Source language' : 'Writing language').selectOption('en')
    if (feature === 'translate') await page.getByLabel('Target language').selectOption('ro')
    else await chooseMode(page, 'friendly')
    const source = feature === 'translate' ? TRANSLATE_SOURCE : REWRITE_SOURCE
    const resultName = feature === 'translate' ? 'Translation result' : 'Improved result'
    const expected = feature === 'translate' ? TRANSLATE_RESULT : REWRITE_RESULT
    const before = await usage(page)
    await page.getByLabel('Source text', { exact: true }).fill(source)
    await page.clock.runFor(999)
    expect(posts).toHaveLength(0)
    await page.clock.runFor(1)
    await expectResult(page, feature, expected)
    await expect(page.getByLabel('Source text', { exact: true })).toBeFocused()
    expect(posts).toHaveLength(1)
    expect((await usage(page)).consumedCharacters).toBe(before.consumedCharacters + 46)
    await expectCleanScan(page, `narrow ${feature} result`)
    await noOverflow(page)
    const edited = `${expected}\nNarrow edited result.`
    await nativeCopy(page, resultName, edited)
    await page.clock.runFor(2000)
    expect(posts).toHaveLength(1)
    expect((await usage(page)).consumedCharacters).toBe(before.consumedCharacters + 46)

    const failedSource = `${source} ${feature === 'translate' ? 'M028' : 'M029'}-PROCESSING-FAILURE`
    await page.getByLabel('Source text', { exact: true }).fill(failedSource)
    await page.clock.runFor(1000)
    await expect(page.getByRole('alert')).toContainText('We couldn’t process this text.')
    await expect(page.getByRole('textbox', { name: resultName })).toHaveValue(edited)
    await expectCleanScan(page, `narrow ${feature} failure`)
    await page.getByRole('button', { name: 'Try again' }).click()
    await expect.poll(() => posts.length).toBe(3)
    await expect(page.getByRole('alert')).toContainText('We couldn’t process this text.')
    expect(posts[1].operationId).not.toBe(posts[2].operationId)
    expect((await usage(page)).consumedCharacters).toBe(before.consumedCharacters + 46)
    await context.clearCookies()
    await page.getByLabel('Source text', { exact: true }).fill(`${source} Next revision.`)
    await page.clock.runFor(1000)
    await expect(page).toHaveURL(/\/login$/)
    await expect(page.getByText('Your session expired. Sign in again to continue.')).toBeVisible()
    await expect(page.locator('textarea')).toHaveCount(0)
    await expect(page.getByLabel('Password')).toHaveValue('')
    await expectCleanScan(page, 'narrow expired session')
  })
}

test('M039 narrow registration validates and explicitly offers sign in without retaining credentials', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/register')
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page.getByLabel('Email')).toBeFocused()
  await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
  await expectCleanScan(page, 'narrow registration validation')
  const visitor = `m039-${Date.now()}@example.test`
  await page.getByLabel('Email').fill(visitor)
  await page.getByLabel('Password', { exact: true }).fill(password!)
  await page.getByLabel('Confirm password').fill(password!)
  await page.getByRole('button', { name: 'Show password' }).click()
  await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('type', 'text')
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page.getByText('Account registration accepted. Sign in to continue.')).toBeVisible()
  await expect(page.getByLabel('Email')).toHaveCount(0)
  await expect(page.getByLabel('Password')).toHaveCount(0)
  await expect(page.getByText(visitor)).toHaveCount(0)
  await expectCleanScan(page, 'narrow registration accepted')
  await noOverflow(page)
  await page.getByRole('link', { name: 'Go to sign in' }).click()
  await expect(page.getByRole('heading', { name: 'Sign in', exact: true })).toBeFocused()
})

for (const feature of ['translate', 'rewrite'] as const) {
  test(`M039 ${feature} remains available at compact and effective-zoom reflow sizes`, async ({ workspace: { page } }) => {
    for (const viewport of [{ width: 1024, height: 768 }, { width: 720, height: 450 }, { width: 320, height: 640 }]) {
      await page.setViewportSize(viewport)
      await openWorkspace(page, feature)
      await page.addStyleTag({ content: '* { line-height: 1.5 !important; letter-spacing: .12em !important; word-spacing: .16em !important; } p { margin-bottom: 2em !important; }' })
      await expectCleanScan(page, `${feature} spacing at ${viewport.width}`)
      await noOverflow(page)
      for (const selector of ['select', 'textarea', 'button']) for (const control of await page.locator(`${selector}:visible`).all()) {
        const box = await control.boundingBox()
        expect(box!.x).toBeGreaterThanOrEqual(0)
        expect(box!.x + box!.width).toBeLessThanOrEqual(viewport.width)
      }
    }
  })
}

test('M039 active account forms reflow with text spacing at 320 CSS pixels', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 640 })
  for (const route of ['login', 'register']) {
    await page.goto(`/${route}`)
    await page.getByRole('button', { name: route === 'login' ? 'Sign in' : 'Create account', exact: true }).click()
    await page.addStyleTag({ content: '* { line-height: 1.5 !important; letter-spacing: .12em !important; word-spacing: .16em !important; } p { margin-bottom: 2em !important; }' })
    await noOverflow(page)
    await expectCleanScan(page, `320 ${route} text-spacing`)
    await page.getByText('Check the highlighted fields.').scrollIntoViewIfNeeded()
    await expect(page.getByText('Check the highlighted fields.')).toBeInViewport()
  }
})

test('M039 interrupted admitted work exposes original-identity Check status without another operation', async ({ workspace: { page, context } }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.clock.install()
  await page.clock.pauseAt(new Date(Date.now() + 1000))
  const posts = trackOperations(page)
  await openWorkspace(page, 'translate')
  await page.getByLabel('Source language').selectOption('en')
  await page.getByLabel('Target language').selectOption('ro')
  await page.getByLabel('Source text', { exact: true }).fill(TRANSLATE_SOURCE)
  await page.clock.runFor(1000)
  await expect(page.getByRole('textbox', { name: 'Translation result' })).toHaveValue(TRANSLATE_RESULT)
  await page.getByLabel('Source text', { exact: true }).fill(`${TRANSLATE_SOURCE} M039-DELAY`)
  await page.clock.runFor(1000)
  await expect.poll(() => posts.length).toBe(2)
  const originalId = posts[1].operationId
  // A real status response proves admission before interrupting the browser's
  // actual connection. No application route or response is intercepted.
  await expect.poll(async () => {
    const response = await page.request.get(`/api/operations/${originalId}`)
    return response.status() === 200 ? (await response.json() as { status: string }).status : 'not-yet-admitted'
  }, { intervals: [10, 20, 50] }).toBe('pending')
  await context.setOffline(true)
  await expect(page.getByRole('alert')).toContainText('You’re offline.')
  // If the transport engine keeps its wait open, the client deadline is still
  // bounded and must preserve the same unknown identity.
  await page.clock.runFor(30000)
  await expect(page.getByRole('alert')).toContainText('We couldn’t confirm whether this request completed.')
  await expect(page.getByRole('button', { name: 'Check status' })).toBeVisible()
  await expect(page.getByRole('textbox', { name: 'Translation result' })).toHaveValue(TRANSLATE_RESULT)
  const refreshed = page.waitForResponse(response => new URL(response.url()).pathname === '/api/usage')
  await context.setOffline(false)
  expect((await refreshed).status()).toBe(200)
  const reads: string[] = []
  page.on('request', request => { if (request.method() === 'GET' && new URL(request.url()).pathname.startsWith('/api/operations/')) reads.push(new URL(request.url()).pathname) })
  const checked = page.waitForResponse(response => new URL(response.url()).pathname === `/api/operations/${originalId}`)
  await page.getByRole('button', { name: 'Check status' }).click()
  const metadata = await (await checked).json() as { status: string }
  if (metadata.status === 'pending') {
    await expect(page.getByRole('button', { name: 'Check status' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Try again' })).toHaveCount(0)
    await expect(page.getByRole('alert')).toContainText('We couldn’t confirm whether this request completed.')
  } else {
    await expect(page.getByRole('button', { name: 'Check status' })).toHaveCount(0)
  }
  await page.clock.runFor(2000)
  expect(reads).toEqual([`/api/operations/${originalId}`])
  expect(posts).toHaveLength(2)
  await expect(page.getByRole('textbox', { name: 'Translation result' })).toHaveValue(TRANSLATE_RESULT)
  await expectCleanScan(page, 'narrow original-identity status recovery')
})
