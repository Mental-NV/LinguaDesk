import { test, expect, openWorkspace, trackOperations, TRANSLATE_SOURCE, TRANSLATE_RESULT, REWRITE_SOURCE, REWRITE_RESULT } from './workspace-fixture'

for (const feature of ['translate', 'rewrite'] as const) {
  test(`${feature} remains editable offline and reconnect alone never starts processing`, async ({ workspace: { page, context } }) => {
    const posts = trackOperations(page)
    await page.clock.install()
    await openWorkspace(page, feature)
    if (feature === 'translate') await page.getByLabel('Target language').selectOption('ro')
    const source = feature === 'translate' ? TRANSLATE_SOURCE : REWRITE_SOURCE
    const expected = feature === 'translate' ? TRANSLATE_RESULT : REWRITE_RESULT
    const resultName = feature === 'translate' ? 'Translation result' : 'Improved result'
    await page.getByLabel('Source text').fill(source)
    await page.clock.runFor(1000)
    await expect(page.getByRole('textbox', { name: resultName })).toHaveValue(expected)
    await context.setOffline(true)
    await expect(page.getByRole('alert')).toContainText('You’re offline.')
    await page.getByLabel('Source text').fill(`${source} Offline edit.`)
    await page.getByRole('textbox', { name: resultName }).fill('Offline result edit')
    await page.clock.runFor(2000)
    expect(posts).toHaveLength(1)
    await context.setOffline(false)
    await expect(page.getByText('You’re offline.', { exact: false })).toHaveCount(0)
    await page.clock.runFor(2000)
    expect(posts).toHaveLength(1)
    await expect(page.getByRole('textbox', { name: resultName })).toHaveValue('Offline result edit')
    await page.getByLabel('Source text').fill(`${source} Online edit.`)
    await page.clock.runFor(1000)
    await expect(page.getByRole('textbox', { name: resultName })).toHaveValue(expected)
    expect(posts).toHaveLength(2)
  })
}

test('an API-observed session expiry clears both editors before the login page appears', async ({ workspace: { page, context } }) => {
  await openWorkspace(page, 'translate')
  await page.getByLabel('Target language').selectOption('ro')
  await page.getByLabel('Source text').fill(TRANSLATE_SOURCE)
  await expect(page.getByRole('textbox', { name: 'Translation result' })).toHaveValue(TRANSLATE_RESULT)
  await context.clearCookies()
  await page.getByLabel('Source text').fill(`${TRANSLATE_SOURCE} Next.`)
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByText('Your session expired. Sign in again to continue.')).toBeVisible()
  await expect(page.locator('textarea')).toHaveCount(0)
  await expect(page.getByLabel('Password')).toHaveValue('')
})
