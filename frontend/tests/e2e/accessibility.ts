import { expect, type Page } from '@playwright/test'

interface ScanFinding {
  kind: string
  detail: string
}

async function runSemanticScan(page: Page): Promise<ScanFinding[]> {
  return page.evaluate(() => {
    const findings: ScanFinding[] = []

    const parse = (color: string): [number, number, number, number] | null => {
      const match = color.match(/rgba?\(([^)]+)\)/)
      if (!match) return null
      const parts = match[1].split(',').map((part) => Number.parseFloat(part.trim()))
      return [parts[0], parts[1], parts[2], parts.length > 3 ? parts[3] : 1]
    }

    const luminance = (r: number, g: number, b: number): number => {
      const channel = (value: number): number => {
        const s = value / 255
        return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4
      }
      return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b)
    }

    const effectiveBackground = (element: Element): [number, number, number] => {
      let current: Element | null = element
      while (current) {
        const background = parse(getComputedStyle(current).backgroundColor)
        if (background && background[3] >= 1) return [background[0], background[1], background[2]]
        current = current.parentElement
      }
      return [255, 255, 255]
    }

    const isVisible = (element: Element): boolean => {
      const style = getComputedStyle(element)
      if (style.display === 'none' || style.visibility === 'hidden' || style.opacity === '0') return false
      const rect = element.getBoundingClientRect()
      return rect.width > 0 && rect.height > 0
    }

    const labelOf = (element: Element): string => {
      const describe = (node: Element): string =>
        `${node.tagName.toLowerCase()}${node.id ? `#${node.id}` : ''}.${(node.className || '').toString().split(' ').slice(0, 2).join('.')}`
      return describe(element)
    }

    if (!document.documentElement.lang) {
      findings.push({ kind: 'language', detail: '<html> declares no lang attribute.' })
    }

    for (const button of document.querySelectorAll('button')) {
      if (!isVisible(button)) continue
      const name = (button.textContent ?? '').trim() || button.getAttribute('aria-label') || ''
      if (name.length === 0) {
        findings.push({ kind: 'name', detail: `Unnamed button: ${labelOf(button)}` })
      }
    }

    for (const control of document.querySelectorAll('input, select, textarea')) {
      if (!isVisible(control)) continue
      const labelled =
        (control instanceof HTMLInputElement ||
          control instanceof HTMLSelectElement ||
          control instanceof HTMLTextAreaElement) &&
        (control.labels?.length ?? 0) > 0
      const aria = control.getAttribute('aria-label') || control.getAttribute('aria-labelledby')
      if (!labelled && !aria) {
        findings.push({ kind: 'label', detail: `Unlabelled control: ${labelOf(control)}` })
      }
    }

    for (const image of document.querySelectorAll('img')) {
      if (!isVisible(image)) continue
      if (!image.hasAttribute('alt')) {
        findings.push({ kind: 'alt', detail: `Image without alt: ${labelOf(image)}` })
      }
    }

    // WCAG exempts inactive (disabled) components from the contrast
    // minimum; names/labels above still apply to them.
    const isInactive = (element: Element): boolean =>
      element.matches(':disabled') || element.getAttribute('aria-disabled') === 'true'

    const textSelectors = [
      'body',
      'h1',
      'h2',
      'p',
      'a',
      'button',
      'label',
      'input',
      'select',
      'textarea',
      '[role="alert"]',
      '[role="status"]',
      '[role="region"]',
    ]
    const checked = new Set<Element>()
    for (const selector of textSelectors) {
      for (const element of document.querySelectorAll(selector)) {
        if (checked.has(element) || !isVisible(element) || isInactive(element)) continue
        checked.add(element)
        const style = getComputedStyle(element)
        const foreground = parse(style.color)
        if (!foreground || foreground[3] < 1) continue
        const text = (element.textContent ?? '').trim()
        if (text.length === 0 && element.children.length > 0) continue
        const background = effectiveBackground(element)
        const l1 = luminance(foreground[0], foreground[1], foreground[2])
        const l2 = luminance(background[0], background[1], background[2])
        const ratio = (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05)
        const size = Number.parseFloat(style.fontSize)
        const bold = Number.parseInt(style.fontWeight, 10) >= 700
        const large = size >= 24 || (size >= 18.66 && bold)
        const minimum = large ? 3.0 : 4.5
        if (ratio < minimum) {
          findings.push({
            kind: 'contrast',
            detail:
              `${labelOf(element)} contrast ${ratio.toFixed(2)}:1 below ${minimum}:1 ` +
              `for "${text.slice(0, 60)}"`,
          })
        }
      }
    }

    // Native field/panel boundaries and empty-result placeholders are
    // independent contrast assertions; screenshots cannot establish them.
    for (const element of document.querySelectorAll('input, select, .editor-panel')) {
      if (!isVisible(element) || isInactive(element)) continue
      const style = getComputedStyle(element)
      const border = parse(style.borderTopColor)
      if (!border || Number.parseFloat(style.borderTopWidth) === 0) continue
      const background = effectiveBackground(element)
      const first = luminance(border[0], border[1], border[2])
      const second = luminance(background[0], background[1], background[2])
      const ratio = (Math.max(first, second) + .05) / (Math.min(first, second) + .05)
      if (ratio < 3) findings.push({ kind: 'control-boundary', detail: `${labelOf(element)} boundary contrast ${ratio.toFixed(2)}:1 below 3:1` })
    }
    for (const editor of document.querySelectorAll('textarea:placeholder-shown')) {
      const placeholder = parse(getComputedStyle(editor, '::placeholder').color)
      if (!placeholder) continue
      const background = effectiveBackground(editor)
      const first = luminance(placeholder[0], placeholder[1], placeholder[2])
      const second = luminance(background[0], background[1], background[2])
      const ratio = (Math.max(first, second) + .05) / (Math.min(first, second) + .05)
      if (ratio < 4.5) findings.push({ kind: 'placeholder', detail: `${labelOf(editor)} placeholder contrast ${ratio.toFixed(2)}:1 below 4.5:1` })
    }

    return findings
  })
}

export async function expectCleanScan(page: Page, state: string): Promise<void> {
  const findings = await runSemanticScan(page)
  expect(
    findings.map((finding) => `${finding.kind}: ${finding.detail}`).join('\n'),
    `semantic/contrast scan for ${state} reports no violations`,
  ).toBe('')
}

