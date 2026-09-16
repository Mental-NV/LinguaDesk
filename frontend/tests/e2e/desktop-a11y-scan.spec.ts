import {
  expect,
  test,
  type Browser,
  type BrowserContext,
  type Page,
} from '@playwright/test'
import { verifiedAccountStorageState } from './translate-auth'

/**
 * M038 automated semantic/contrast scans (AC-005 automated part, V-011):
 * dependency-free browser checks over both workspaces and the selected
 * auth states at desktop size. Every button must expose an accessible name,
 * every form control must be labelled, the document must declare its
 * language, and visible text must meet WCAG contrast (4.5:1 ordinary,
 * 3:1 large text) against its effective background. Any violation fails the
 * run with the offending element identified — there is no serious/critical
 * budget. Understandable narration, native mobile keyboards and the full
 * device/AT matrix remain manual evidence (T004); this scan is independent
 * of screenshots.
 */

const SIGN_IN_PASSWORD = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
const VERIFIED_EMAIL = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL

if (!SIGN_IN_PASSWORD || !VERIFIED_EMAIL) {
  throw new Error('The published desktop scan smoke account configuration is required.')
}

let authenticatedState: Awaited<ReturnType<typeof verifiedAccountStorageState>> | null = null

test.beforeAll(async ({ browser }) => {
  authenticatedState = await verifiedAccountStorageState(browser)
})

async function verifiedWorkspace(browser: Browser): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext({
    storageState: authenticatedState ?? undefined,
    viewport: { width: 1440, height: 900 },
  })
  const page = await context.newPage()
  return { context, page }
}

interface ScanFinding {
  kind: string
  detail: string
}

async function runDesktopScan(page: Page): Promise<ScanFinding[]> {
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

    return findings
  })
}

async function expectCleanScan(page: Page, state: string): Promise<void> {
  const findings = await runDesktopScan(page)
  expect(
    findings.map((finding) => `${finding.kind}: ${finding.detail}`).join('\n'),
    `semantic/contrast scan for ${state} reports no violations`,
  ).toBe('')
}

test('translate workspace states scan clean at desktop', async ({ browser }) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    await page.goto('/translate')
    await expect(page.getByRole('heading', { name: 'Translation' })).toBeFocused()
    await page.getByLabel('Source language').selectOption('en')
    await page.getByLabel('Target language').selectOption('ro')
    await page.getByLabel('Source text').fill('Hello, the meeting starts at 14:30. Please go.')
    await expectCleanScan(page, 'translate ready')

    await page.getByLabel('Source text').fill('a'.repeat(5312))
    await expect(
      page.getByText('Translation is limited to 5,000 characters. Remove 312 characters to continue.'),
    ).toBeVisible()
    await expectCleanScan(page, 'translate oversize error')

    // Keyboard focus must stay visible on the primary action.
    await page.getByLabel('Source text').fill('Hello.')
    await page.getByRole('button', { name: 'Translate' }).focus()
    const ring = await page.getByRole('button', { name: 'Translate' }).evaluate((element) => {
      const style = getComputedStyle(element)
      return { outlineWidth: style.outlineWidth, outlineStyle: style.outlineStyle }
    })
    expect(
      `${ring.outlineStyle} ${ring.outlineWidth}`,
      'focused Translate control keeps a visible focus indicator',
    ).not.toBe('none 0px')
  } finally {
    await context.close()
  }
})

test('rewrite workspace scans clean at desktop', async ({ browser }) => {
  const { context, page } = await verifiedWorkspace(browser)
  try {
    await page.goto('/rewrite')
    await expect(page.getByRole('heading', { name: 'Rewriting' })).toBeFocused()
    await page.getByLabel('Source text').fill('The report is really ready. We sends it today.')
    await expectCleanScan(page, 'rewrite ready')
  } finally {
    await context.close()
  }
})

test('register and login states scan clean at desktop', async ({ browser }) => {
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } })
  const page = await context.newPage()
  try {
    await page.goto('/register')
    await expect(page.getByRole('heading', { name: 'Create account' })).toBeFocused()
    await page.getByLabel('Email').fill('not-an-email')
    await page.getByLabel('Password', { exact: true }).fill('short')
    await page.getByLabel('Confirm password').fill('mismatch')
    await page.getByRole('button', { name: 'Create account' }).click()
    await expect(page.getByText('Check the highlighted fields.')).toBeVisible()
    await expectCleanScan(page, 'register long validation')

    await page.goto('/login')
    await expect(page.getByRole('heading', { name: 'Sign in' })).toBeFocused()
    await expectCleanScan(page, 'login')
  } finally {
    await context.close()
  }
})
