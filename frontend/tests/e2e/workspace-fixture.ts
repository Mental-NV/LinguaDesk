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
  await expect(page.getByRole('heading', { name: feature === 'translate' ? 'Translation' : 'Rewriting', exact: true })).toBeFocused()
  await expect(page.getByLabel(feature === 'translate' ? 'Source language' : 'Writing language')).toBeEnabled()
}
export async function nativeCopy(page: Page, resultName: string, value: string) {
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
