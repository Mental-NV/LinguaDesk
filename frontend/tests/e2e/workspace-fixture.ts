import { test as base, expect, type BrowserContext, type Page } from '@playwright/test'
import { verifiedAccountStorageState } from './translate-auth'

export const TRANSLATE_SOURCE = 'Hello, the meeting starts at 14:30. Please go.'
export const TRANSLATE_RESULT = 'Bună, întâlnirea începe la 14:30. Te rog să mergi.'
export const REWRITE_SOURCE = 'The report is really ready. We sends it today.'
export const REWRITE_RESULT = 'The report is ready. We send it today.'
export const test = base.extend<{ workspace: { page: Page; context: BrowserContext } }>({
  workspace: async ({ browser }, provide) => {
    const context = await browser.newContext({ storageState: await verifiedAccountStorageState(browser), permissions: ['clipboard-read', 'clipboard-write'], deviceScaleFactor: 1, reducedMotion: 'reduce' })
    const page = await context.newPage()
    await provide({ page, context })
    await context.close()
  },
})
export { expect }
export function trackOperations(page: Page) {
  const posts: Record<string, string>[] = []
  page.on('request', request => {
    if (new URL(request.url()).pathname === '/api/operations' && request.method() === 'POST') posts.push(request.postDataJSON() as Record<string, string>)
  })
  return posts
}
export async function usage(page: Page): Promise<{ consumedCharacters: number; revision: number }> {
  const response = await page.request.get('/api/usage')
  expect(response.status()).toBe(200)
  return response.json()
}
export async function openWorkspace(page: Page, feature: 'translate' | 'rewrite') {
  await page.goto(`/${feature}`)
  await expect(page.getByRole('heading', { name: feature === 'translate' ? 'Translator' : 'Write', exact: true })).toBeFocused()
  await expect(page.getByLabel(feature === 'translate' ? 'Source language' : 'Writing language')).toBeEnabled()
  await expect.poll(async () => (await page.locator('.workspace-frame').boundingBox())?.width).toBe(page.viewportSize()!.width - (page.viewportSize()!.width >= 768 ? 96 : page.viewportSize()!.width >= 360 ? 32 : 24))
}
export async function nativeCopy(page: Page, resultName: string, value: string) {
  if (resultName === 'Improved result' && await page.getByRole('button', { name: 'Edit result' }).count()) await page.getByRole('button', { name: 'Edit result' }).click()
  const result = page.getByRole('textbox', { name: resultName })
  await result.fill(value)
  await result.focus()
  await result.press('ControlOrMeta+A')
  await result.press('ControlOrMeta+C')
  await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toBe(value)
}
export async function noOverflow(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
}

export async function expectResult(page: Page, feature: 'translate' | 'rewrite', text: string) {
  if (feature === 'translate') await expect(page.getByRole('textbox', { name: 'Translation result' })).toHaveValue(text)
  else {
    const editor = page.getByRole('textbox', { name: 'Improved result' })
    if (await editor.count()) await expect(editor).toHaveValue(text)
    else await expect(page.getByTestId('write-result')).toHaveText(text)
  }
}
export async function chooseMode(page: Page, value: string) {
  const toolbar = page.getByRole('button', { name: 'Toolbar', exact: true })
  if (await toolbar.count()) await toolbar.click()
  const correction = page.getByRole('switch', { name: 'Correction Only' })
  if (await correction.getAttribute('aria-checked') === 'true') await correction.click()
  await page.getByRole('button', { name: 'Styles/Tones', exact: true }).click()
  await page.getByRole('option', { name: value === 'correctionOnly' ? 'None set' : value[0].toUpperCase() + value.slice(1), exact: true }).click()
}
export async function logOut(page: Page) {
  const account = page.getByRole('button', { name: 'Account', exact: true })
  if (!(await account.isVisible())) await page.getByRole('button', { name: 'Navigation', exact: true }).click()
  await account.click()
  await page.getByRole('menuitem', { name: 'Log out' }).click()
}
