import { expectResult, chooseMode, test, expect, nativeCopy, noOverflow, openWorkspace, trackOperations, TRANSLATE_SOURCE, TRANSLATE_RESULT, REWRITE_SOURCE, REWRITE_RESULT } from './workspace-fixture'

test('native source selection, newline and undo retain the caret without implicit work', async ({ workspace: { page } }) => {
  const posts = trackOperations(page)
  await openWorkspace(page, 'translate')
  const source = page.getByLabel('Source text', { exact: true })
  await source.focus()
  await page.keyboard.type('Hello meeting')
  await source.press('End')
  await source.press('Enter')
  await page.keyboard.type('Second line')
  await expect(source).toHaveValue('Hello meeting\nSecond line')
  await source.press('ControlOrMeta+A')
  const selection = await source.evaluate(element => ({ start: (element as HTMLTextAreaElement).selectionStart, end: (element as HTMLTextAreaElement).selectionEnd }))
  expect(selection).toEqual({ start: 0, end: 25 })
  await page.keyboard.insertText('Replacement')
  await source.press('ControlOrMeta+Z')
  await expect(source).toHaveValue('Hello meeting\nSecond line')
  expect(posts).toHaveLength(0)
})

for (const feature of ['translate', 'rewrite'] as const) {
  test(`${feature} composition guards wait for committed input and native result copy preserves exact edits`, async ({ workspace: { page } }) => {
    await page.clock.install()
    // Keep the 999/1000 ms boundary independent of browser action latency.
    await page.clock.pauseAt(new Date(Date.now() + 1000))
    const posts = trackOperations(page)
    await openWorkspace(page, feature)
    if (feature === 'translate') await page.getByLabel('Target language').selectOption('ro')
    const source = page.getByLabel('Source text', { exact: true })
    await source.dispatchEvent('compositionstart')
    await source.fill(feature === 'translate' ? TRANSLATE_SOURCE : REWRITE_SOURCE)
    if (feature === 'rewrite') await chooseMode(page, 'friendly')
    await page.clock.runFor(2000)
    expect(posts).toHaveLength(0)
    await source.dispatchEvent('compositionend')
    await page.clock.runFor(999)
    expect(posts).toHaveLength(0)
    await page.clock.runFor(1)
    const resultName = feature === 'translate' ? 'Translation result' : 'Improved result'
    const expected = feature === 'translate' ? TRANSLATE_RESULT : REWRITE_RESULT
    await expectResult(page, feature, expected)
    expect(posts).toHaveLength(1)
    await nativeCopy(page, resultName, `${expected}\nNative edit.`)
    await page.clock.runFor(2000)
    expect(posts).toHaveLength(1)
  })
}

for (const feature of ['translate', 'rewrite'] as const) {
  for (const [width, expectedContent, expectedPanel] of [[1440, 1344, 666], [768, 672, 330], [390, 358, 358], [320, 296, 296]] as const) {
    test(`${feature} exact workspace geometry at ${width}px`, async ({ workspace: { page } }) => {
      await page.setViewportSize({ width, height: width < 768 ? 844 : 900 })
      await openWorkspace(page, feature)
      const frame = await page.locator('.workspace-frame').boundingBox()
      const source = await page.locator('.source-panel').boundingBox()
      const result = await page.locator('.result-panel').boundingBox()
      expect(frame!.width).toBe(expectedContent)
      expect(source!.width).toBe(expectedPanel)
      expect(result!.width).toBe(expectedPanel)
      if (width >= 768) {
        expect(source!.height).toBe(result!.height)
        expect(source!.y).toBe(result!.y)
        expect(result!.x - source!.x - source!.width).toBe(12)

      } else {
        expect(result!.y).toBeGreaterThan(source!.y + source!.height)
        for (const selector of feature === 'translate' ? ['Source language', 'Target language'] : ['Writing language']) {
          const control = await page.getByLabel(selector).boundingBox()
          expect(control!.width).toBeLessThanOrEqual(expectedContent)
          expect(control!.y + control!.height).toBeLessThan(source!.y)
        }
      }
      await noOverflow(page)
      await page.getByLabel('Source text', { exact: true }).fill('Long English Кириллица 中文'.repeat(350))
      await expect(page.getByRole('alert')).toBeVisible()
      await noOverflow(page)
      // Browser geometry under text-spacing overrides, independent of pixels.
      await page.addStyleTag({ content: '* { line-height: 1.5 !important; letter-spacing: .12em !important; word-spacing: .16em !important; } p { margin-bottom: 2em !important; }' })
      await noOverflow(page)
      for (const control of await page.locator('select:visible, textarea:visible, button:visible').all()) {
        const box = await control.boundingBox()
        expect(box!.x).toBeGreaterThanOrEqual(0)
        expect(box!.x + box!.width).toBeLessThanOrEqual(width)
      }
    })
  }
}
