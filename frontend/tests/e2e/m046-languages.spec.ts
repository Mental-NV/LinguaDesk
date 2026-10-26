import { test, expect, openWorkspace, expectResult, trackOperations, noOverflow, nativeCopy, usage } from './workspace-fixture'
import { stabilizeMultilingualFonts } from './m046-fonts'

const arResult = 'التقرير جاهز. سنرسله اليوم.'
const heSource = 'הדוח ממש ממש מוכן. נשלח אותו היום.'
const heResult = 'הדוח מוכן. נשלח אותו היום.'
const fixtures = [
  ['hi', 'रिपोर्ट बिल्कुल बिल्कुल तैयार है। हम इसे आज भेजेंगे।', 'रिपोर्ट तैयार है। हम इसे आज भेजेंगे।'],
  ['ja', '報告書はもうもう完成しています。今日送ります。', '報告書は完成しています。今日送ります。'],
  ['ko', '보고서가 정말 정말 준비되었습니다. 오늘 보내겠습니다.', '보고서가 준비되었습니다. 오늘 보내겠습니다.'],
  ['fil', 'Handang handa na ang ulat. Ipapadala namin ito ngayon.', 'Handa na ang ulat. Ipapadala namin ito ngayon.'],
] as const

for (const [width, height] of [[1440, 900], [390, 844]] as const) {
  test(`M046 RTL Translator visible swap and native logical clipboard ${width}`, async ({ workspace: { page } }) => {
    await page.setViewportSize({ width, height }); await openWorkspace(page, 'translate')
    const posts = trackOperations(page)
    await page.getByLabel('Target language').selectOption('ar')
    await page.getByLabel('Source text', { exact: true }).fill('Hello, the meeting starts at 14:30. Please go.')
    await expectResult(page, 'translate', arResult)
    await expect(page.getByRole('textbox', { name: 'Translation result' })).toHaveAttribute('dir', 'rtl')
    await page.getByRole('button', { name: 'Swap languages and texts' }).click()
    await expect(page.getByLabel('Source text', { exact: true })).toHaveValue(arResult)
    await expect(page.getByLabel('Source language')).toHaveValue('ar')
    await expect(page.getByLabel('Target language')).toHaveValue('en')
    await expectResult(page, 'translate', 'Hello, the meeting starts at 14:30. Please go.')
    await expect.poll(() => posts.length).toBe(2)
    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ar')
    await page.getByLabel('Source text', { exact: true }).fill('The report is ready.')
    await expectResult(page, 'translate', arResult)
    await stabilizeMultilingualFonts(page); await noOverflow(page)
    expect(await page.locator('.language-bar').evaluate(node => getComputedStyle(node).direction)).toBe('ltr')
    await expect(page).toHaveScreenshot(`m046-rtl-translator-${width}x${height}.png`, { fullPage: true, animations: 'disabled', maxDiffPixelRatio: .001 })
    const logical = `${arResult}\nhttps://example.test/12 — 14:30\nنعم؟`
    await nativeCopy(page, 'Translation result', logical)
    await page.getByRole('button', { name: 'Copy result', exact: true }).click()
    await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toBe(logical)
    const beforeFailure = await usage(page)
    await page.getByLabel('Source text', { exact: true }).fill('The report is ready. M028-PROCESSING-FAILURE')
    await expect(page.getByRole('alert')).toContainText('We couldn’t process this text.')
    await expectResult(page, 'translate', logical)
    expect((await usage(page)).consumedCharacters).toBe(beforeFailure.consumedCharacters)
  })
  test(`M046 RTL Write review alternatives and editing ${width}`, async ({ workspace: { page } }) => {
    await page.setViewportSize({ width, height }); await openWorkspace(page, 'rewrite')
    const posts = trackOperations(page)
    await page.getByLabel('Source text', { exact: true }).fill(heSource)
    await expectResult(page, 'rewrite', heResult)
    await expect(page.getByTestId('write-result')).toHaveAttribute('dir', 'rtl')
    await page.getByRole('button', { name: 'Toolbar', exact: true }).click()
    await page.getByRole('switch', { name: 'Show changes' }).click()
    await expect(page.getByLabel('Deleted source text')).toContainText('ממש')
    await expect(page.getByLabel('Deleted source text').locator('del').first()).toHaveAttribute('dir', 'auto')
    const before = await usage(page)
    await page.getByRole('button', { name: 'Sentence alternatives: הדוח מוכן.' }).click()
    await expect(page.getByRole('button', { name: 'הדוח כבר מוכן.', exact: true })).toBeVisible()
    await expect(page.getByRole('button', { name: 'הדוח כבר מוכן.', exact: true })).toHaveAttribute('dir', 'rtl')
    await stabilizeMultilingualFonts(page); await noOverflow(page)
    await expect(page).toHaveScreenshot(`m046-rtl-write-${width}x${height}.png`, { fullPage: true, animations: 'disabled', maxDiffPixelRatio: .001 })
    const charged = await usage(page)
    expect(charged.consumedCharacters - before.consumedCharacters).toBe(Array.from('הדוח מוכן.').length)
    await page.getByRole('button', { name: 'הדוח כבר מוכן.', exact: true }).click()
    await expectResult(page, 'rewrite', 'הדוח כבר מוכן. נשלח אותו היום.')
    await page.getByRole('button', { name: 'Copy result', exact: true }).click()
    await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toBe('הדוח כבר מוכן. נשלח אותו היום.')
    expect((await usage(page)).consumedCharacters).toBe(charged.consumedCharacters)
    expect(posts).toHaveLength(1)
    await nativeCopy(page, 'Improved result', `${heResult}\nhttps://example.test/12`)
    await page.getByRole('button', { name: 'Done editing' }).click()
    await expect(page.getByRole('button', { name: /Sentence alternatives:/ })).toHaveCount(0)
    await page.getByRole('button', { name: 'Use result as source' }).click()
    await expect(page.getByLabel('Source text', { exact: true })).toHaveValue(new RegExp(heResult.replaceAll('.', '\\.')))
  })
}

for (const [language, source, result] of fixtures) {
  test(`M046 published ${language} Write has native editing copying and sentence alternatives`, async ({ workspace: { page } }) => {
    await openWorkspace(page, 'rewrite')
    await page.getByLabel('Writing language').selectOption(language)
    await page.getByLabel('Source text', { exact: true }).fill(source)
    await expectResult(page, 'rewrite', result)
    await expect(page.getByTestId('write-result')).toHaveAttribute('lang', language)
    await stabilizeMultilingualFonts(page)
    const first = page.getByRole('button', { name: /Sentence alternatives:/ }).first()
    await first.click(); await expect(page.locator('.alternative-option')).toHaveCount(2)
    await page.locator('.alternative-option').first().click()
    await page.getByRole('button', { name: 'Copy result', exact: true }).click()
    const value = await page.getByTestId('write-result').innerText()
    await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toBe(value)
    await nativeCopy(page, 'Improved result', `${result}\ne\u0301 क्\u200dष`)
  })
}

test('M046 close-language override restarts one pause and long labels reflow at 320 px', async ({ workspace: { page } }) => {
  await page.setViewportSize({ width: 320, height: 844 }); await openWorkspace(page, 'translate')
  const posts = trackOperations(page)
  await page.getByLabel('Target language').selectOption('ar')
  await page.getByLabel('Source text', { exact: true }).fill('Rapporten er klar.')
  await expect(page.getByRole('alert')).toContainText('Choose the source language')
  await page.getByLabel('Source language').selectOption('nn')
  await expectResult(page, 'translate', arResult)
  expect(posts).toHaveLength(2)
  await page.getByLabel('Target language').selectOption('zh')
  await noOverflow(page)
  await expect(page.getByLabel('Target language')).toContainText('Chinese (Simplified output)')
})

test('M046 glyph and combining-script fallback coverage is rendered without clipping', async ({ workspace: { page } }) => {
  await openWorkspace(page, 'translate'); await stabilizeMultilingualFonts(page)
  const source = 'هل أرسلت 12 تقريرًا؟\nהדוח מוכן.\nक्\u200dष e\u0301\nሪፖርቱ ዝግጁ ነው።\nအစီရင်ခံစာ အဆင်သင့်ဖြစ်ပြီ။\nរបាយការណ៍រួចរាល់ហើយ។\nรายงานพร้อมแล้ว\n報告書は完成しています。\n보고서가 준비되었습니다.'
  await page.getByLabel('Source text', { exact: true }).fill(source)
  await expect(page.getByLabel('Source text', { exact: true })).toHaveValue(source)
  await page.getByRole('button', { name: 'Copy source text' }).click()
  await expect.poll(() => page.evaluate(() => navigator.clipboard.readText())).toBe(source)
  await expect(page.getByLabel('Source text', { exact: true })).toHaveAttribute('dir', 'auto')
  await noOverflow(page)
  await page.screenshot({ path: '../artifacts/m046/script-glyphs.png', fullPage: true })
})


test('M046 real login form opens the protected workspace and translates a new language', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email', { exact: true }).fill(process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL!)
  await page.getByLabel('Password', { exact: true }).fill(process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD!)
  await page.getByRole('button', { name: 'Sign in', exact: true }).click()
  await expect(page.getByLabel('Source language')).toBeEnabled()
  await page.getByLabel('Target language').selectOption('ar')
  await page.getByLabel('Source text', { exact: true }).fill('The report is ready.')
  await expectResult(page, 'translate', arResult)
})
