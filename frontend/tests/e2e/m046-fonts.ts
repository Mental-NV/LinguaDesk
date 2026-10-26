import { readFileSync } from 'node:fs'
import { createHash } from 'node:crypto'
import type { Page } from '@playwright/test'
const manifests = ['manifest.json', 'manifest.m046.json']
export async function stabilizeMultilingualFonts(page: Page) {
  const fonts = manifests.flatMap(file => JSON.parse(readFileSync(new URL(`./fonts/${file}`, import.meta.url), 'utf8')) as { family: string; weight: string; file: string; sha256: string }[])
  const css = fonts.map(font => {
    const bytes = readFileSync(new URL(`./fonts/${font.file}`, import.meta.url))
    if (createHash('sha256').update(bytes).digest('hex') !== font.sha256) throw new Error(`Font hash mismatch: ${font.file}`)
    return `@font-face { font-family: '${font.family}'; font-weight: ${font.weight}; font-style: normal; src: url(data:font/ttf;base64,${bytes.toString('base64')}); }`
  }).join('\n')
  const families = [...new Set(fonts.map(font => font.family))]
  await page.addStyleTag({ content: `${css}\n:root { font-family: ${families.map(family => `'${family}'`).join(', ')}, sans-serif; } * { caret-color: transparent !important; transition: none !important; }` })
  await page.evaluate(async (families) => { await Promise.all(families.map(family => document.fonts.load(`16px "${family}"`))); await document.fonts.ready; scrollTo(0, 0) }, families)
}
