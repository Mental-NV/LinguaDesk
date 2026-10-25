import { readFileSync } from 'node:fs'
import { test, expect, openWorkspace, TRANSLATE_SOURCE, TRANSLATE_RESULT, REWRITE_SOURCE, REWRITE_RESULT } from './workspace-fixture'
import type { Page } from '@playwright/test'
const fontManifest = JSON.parse(readFileSync(new URL('./fonts/manifest.json', import.meta.url), 'utf8')) as { family: string; weight: string; file: string }[]

// M044: seven current-contract baselines, independently checked by geometry
// and semantic tests. Changes require recorded AI visual review under D-22.
async function stabilize(page: Page) {
  const fonts = fontManifest.map(font => `@font-face { font-family: '${font.family}'; font-weight: ${font.weight}; font-style: normal; src: url(data:font/ttf;base64,${readFileSync(new URL(`./fonts/${font.file}`, import.meta.url)).toString('base64')}); }`).join('\n')
  await page.addStyleTag({ content: `${fonts}\n:root { font-family: 'Noto Sans', 'Noto Sans SC', sans-serif; } * { caret-color: transparent !important; transition: none !important; }` })
  await page.evaluate(async () => {
    await Promise.all([document.fonts.load('16px "Noto Sans"'), document.fonts.load('600 20px "Noto Sans"'), document.fonts.load('700 20px "Noto Sans"'), document.fonts.load('16px "Noto Sans SC"')])
    await document.fonts.ready
    document.querySelectorAll('textarea').forEach(editor => { editor.scrollTop = 0 })
    scrollTo(0, 0)
  })
}
async function baseline(page: Page, name: string) {
  await stabilize(page)
  await expect(page).toHaveScreenshot(name, { animations: 'disabled', caret: 'hide', scale: 'css', fullPage: true, threshold: .1, maxDiffPixelRatio: .001 })
}
for (const [width, height] of [[1440, 900], [390, 844]] as const) {
  test(`Translation result baseline ${width}`, async ({ workspace: { page } }) => {
    await page.setViewportSize({ width, height })
    await openWorkspace(page, 'translate')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill(TRANSLATE_SOURCE)
    await expect(page.getByRole('textbox', { name: 'Translation result' })).toHaveValue(TRANSLATE_RESULT)
    await baseline(page, `translation-result-${width}x${height}.png`)
  })
  test(`Translation oversize baseline ${width}`, async ({ workspace: { page } }) => {
    await page.setViewportSize({ width, height })
    await openWorkspace(page, 'translate')
    await page.getByLabel('Source text').fill('a'.repeat(5312))
    await expect(page.getByRole('alert')).toContainText('Shorten the text to continue.')
    await baseline(page, `translation-oversize-error-${width}x${height}.png`)
  })
  test(`Rewriting edited-result baseline ${width}`, async ({ workspace: { page } }) => {
    await page.setViewportSize({ width, height })
    await openWorkspace(page, 'rewrite')
    await expect(page.getByLabel('Writing language')).toHaveValue('auto')
    await expect(page.getByLabel('Writing language').locator('option:checked')).toHaveText('Detect automatically')
    await page.getByLabel('Writing mode').selectOption('friendly')
    await page.getByLabel('Source text').fill(REWRITE_SOURCE)
    await expect(page.getByRole('textbox', { name: 'Improved result' })).toHaveValue(REWRITE_RESULT)
    await page.getByRole('textbox', { name: 'Improved result' }).fill(`${REWRITE_RESULT}\nMy edited result.`)
    await expect(page.getByLabel('Writing language')).toHaveValue('auto')
    await expect(page.getByLabel('Writing language').locator('option:checked')).toHaveText('Detect automatically')
    await baseline(page, `rewriting-edited-result-${width}x${height}.png`)
  })
}
test('registration long-validation baseline', async ({ page }) => {
  await page.goto('/register')
  await page.getByLabel('Email').fill('not-an-email')
  await page.getByLabel('Password', { exact: true }).fill('short')
  await page.getByLabel('Confirm password').fill('mismatch')
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
  await baseline(page, 'registration-long-validation-1440x900.png')
})
