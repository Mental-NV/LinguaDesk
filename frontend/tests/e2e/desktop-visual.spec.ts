import { readFileSync } from 'node:fs'
import { test, expect, chooseMode, openWorkspace, TRANSLATE_SOURCE, TRANSLATE_RESULT, REWRITE_SOURCE, REWRITE_RESULT } from './workspace-fixture'
import type { Page } from '@playwright/test'
const fontManifest = JSON.parse(readFileSync(new URL('./fonts/manifest.json', import.meta.url), 'utf8')) as { family: string; weight: string; file: string }[]

// M045: twelve D-23 baselines, independently checked by geometry and semantic tests.
// Changes require recorded visual review under D-22.
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
  if (name.includes('menu')) await expect(page.getByRole('listbox', { name: 'Styles/Tones' })).toBeVisible()
  await expect(page).toHaveScreenshot(name, { animations: 'disabled', caret: 'hide', scale: 'css', fullPage: !name.includes('menu'), threshold: .1, maxDiffPixelRatio: .001 })
}
// D-23: exactly eleven workspace captures plus the retained registration capture.
async function result(page: Page, feature: 'translate' | 'rewrite') {
  await openWorkspace(page, feature)
  if (feature === 'translate') await page.getByLabel('Target language').selectOption('ro')
  await page.getByLabel('Source text', { exact: true }).fill(feature === 'translate' ? TRANSLATE_SOURCE : REWRITE_SOURCE)
  if (feature === 'translate') await expect(page.getByRole('textbox', { name: 'Translation result' })).toHaveValue(TRANSLATE_RESULT)
  else await expect(page.getByTestId('write-result')).toHaveText(REWRITE_RESULT)
}
async function alternatives(page: Page) {
  await result(page, 'rewrite')
  await page.getByRole('button', { name: 'Sentence alternatives: The report is ready.' }).click()
  await expect(page.getByRole('button', { name: 'The report is prepared.', exact: true })).toBeVisible()
}
test('Translator empty collapsed baseline', async ({ workspace: { page } }) => {
  await openWorkspace(page, 'translate'); await baseline(page, 'translator-empty-collapsed-1440x900.png')
})
test('Translator empty both panels expanded baseline', async ({ workspace: { page } }) => {
  await openWorkspace(page, 'translate')
  await page.getByRole('button', { name: 'Expand navigation' }).click()
  await page.getByRole('button', { name: 'Toolbar', exact: true }).click()
  await baseline(page, 'translator-empty-expanded-1440x900.png')
})
test('Write empty focused baseline', async ({ workspace: { page } }) => {
  await openWorkspace(page, 'rewrite'); await page.getByLabel('Source text', { exact: true }).focus()
  await baseline(page, 'write-empty-focused-1440x900.png')
})
test('Write alternatives and changes baseline', async ({ workspace: { page } }) => {
  await alternatives(page)
  await page.getByRole('button', { name: 'Toolbar', exact: true }).click()
  await page.getByRole('switch', { name: 'Show changes' }).click()
  await baseline(page, 'write-alternatives-changes-1440x900.png')
})
test('Write selected Business menu baseline', async ({ workspace: { page } }) => {
  await openWorkspace(page, 'rewrite'); await chooseMode(page, 'business')
  await page.getByRole('button', { name: 'Styles/Tones', exact: true }).click()
  await baseline(page, 'write-business-menu-1440x900.png')
})
for (const [width, height] of [[1440, 900], [390, 844]] as const) {
  test(`Translator result baseline ${width}`, async ({ workspace: { page } }) => {
    await page.setViewportSize({ width, height }); await result(page, 'translate')
    await baseline(page, `translation-result-${width}x${height}.png`)
  })
  test(`Translator oversize baseline ${width}`, async ({ workspace: { page } }) => {
    await page.setViewportSize({ width, height }); await openWorkspace(page, 'translate')
    await page.getByLabel('Source text', { exact: true }).fill('a'.repeat(5312))
    await expect(page.getByRole('alert')).toContainText('Shorten the text to continue.')
    await baseline(page, `translation-oversize-error-${width}x${height}.png`)
  })
}
test('Write mobile alternatives baseline', async ({ workspace: { page } }) => {
  await page.setViewportSize({ width: 390, height: 844 }); await alternatives(page)
  await baseline(page, 'write-alternatives-390x844.png')
})
test('Write mobile tools menu baseline', async ({ workspace: { page } }) => {
  await page.setViewportSize({ width: 390, height: 844 }); await openWorkspace(page, 'rewrite')
  await chooseMode(page, 'business'); await page.getByRole('button', { name: 'Styles/Tones', exact: true }).click()
  await baseline(page, 'write-tools-menu-390x844.png')
})
test('registration long-validation baseline', async ({ page }) => {
  await page.goto('/register')
  await page.getByLabel('Email').fill('not-an-email')
  await page.getByLabel('Password', { exact: true }).fill('short')
  await page.getByLabel('Confirm password').fill('mismatch')
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
  await baseline(page, 'registration-long-validation-1440x900.png')
})
