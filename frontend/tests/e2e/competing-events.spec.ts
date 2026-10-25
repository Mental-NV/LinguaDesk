import { test, expect, openWorkspace, trackOperations, TRANSLATE_SOURCE, TRANSLATE_RESULT, REWRITE_SOURCE, REWRITE_RESULT } from './workspace-fixture'

test('mode links and Back/Forward preserve each draft and cancel an undispatched pause', async ({ workspace: { page } }) => {
  await page.clock.install()
  const posts = trackOperations(page)
  await openWorkspace(page, 'translate')
  await page.getByLabel('Target language').selectOption('ro')
  await page.getByLabel('Source text').fill(TRANSLATE_SOURCE)
  await page.getByRole('link', { name: 'Rewriting', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Rewriting', exact: true })).toBeFocused()
  await expect(page.getByLabel('Writing language')).toBeEnabled()
  await page.getByLabel('Source text').fill(REWRITE_SOURCE)
  await page.goBack()
  await expect(page.getByRole('heading', { name: 'Translation', exact: true })).toBeFocused()
  await expect(page.getByLabel('Source text')).toHaveValue(TRANSLATE_SOURCE)
  await page.clock.runFor(2000)
  expect(posts).toHaveLength(0)
  await page.goForward()
  await expect(page.getByLabel('Source text')).toHaveValue(REWRITE_SOURCE)
  await expect(page.getByRole('heading', { name: 'Rewriting', exact: true })).toBeFocused()
  await page.clock.runFor(2000)
  expect(posts).toHaveLength(0)
})

test('submitted work survives mode navigation without replacing the destination text or focus', async ({ workspace: { page } }) => {
  const posts = trackOperations(page)
  await openWorkspace(page, 'translate')
  await page.getByLabel('Target language').selectOption('ro')
  const completed = page.waitForResponse(response => new URL(response.url()).pathname === '/api/operations' && response.request().method() === 'POST')
  await page.getByLabel('Source text').fill(TRANSLATE_SOURCE)
  await expect.poll(() => posts.length).toBe(1)
  await page.getByRole('link', { name: 'Rewriting', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Rewriting', exact: true })).toBeFocused()
  expect((await completed).status()).toBe(201)
  await expect(page.getByRole('textbox', { name: 'Improved result' })).toHaveValue('')
  await expect(page.getByLabel('Source text')).toHaveValue('')
  await expect(page.getByRole('heading', { name: 'Rewriting', exact: true })).toBeFocused()
  await page.getByRole('link', { name: 'Translation', exact: true }).click()
  await expect(page.getByRole('textbox', { name: 'Translation result' })).toHaveValue(TRANSLATE_RESULT)
})

test('a new eligible revision replaces a preexisting result edit on both pages', async ({ workspace: { page } }) => {
  for (const feature of ['translate', 'rewrite'] as const) {
    await openWorkspace(page, feature)
    if (feature === 'translate') await page.getByLabel('Target language').selectOption('ro')
    const source = feature === 'translate' ? TRANSLATE_SOURCE : REWRITE_SOURCE
    const expected = feature === 'translate' ? TRANSLATE_RESULT : REWRITE_RESULT
    const result = page.getByRole('textbox', { name: feature === 'translate' ? 'Translation result' : 'Improved result' })
    await page.getByLabel('Source text').fill(source)
    await expect(result).toHaveValue(expected)
    await result.fill('My manual edit')
    await page.getByLabel('Source text').fill(`${source} Next.`)
    await expect(result).toHaveValue(expected)
  }
})
