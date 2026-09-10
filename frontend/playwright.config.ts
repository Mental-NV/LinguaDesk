import { defineConfig, devices } from '@playwright/test'

const baseURL = process.env.LINGUADESK_PUBLISHED_URL

if (!baseURL) {
  throw new Error('LINGUADESK_PUBLISHED_URL must identify the owned published host.')
}

const publishedUrl = new URL(baseURL)
if (publishedUrl.protocol !== 'https:' || publishedUrl.hostname !== '127.0.0.1') {
  throw new Error('LINGUADESK_PUBLISHED_URL must identify the owned HTTPS loopback host.')
}

export default defineConfig({
  testDir: './tests/e2e',
  outputDir: '../artifacts/playwright',
  globalTimeout: 60_000,
  timeout: 15_000,
  expect: { timeout: 5_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [
    ['list'],
    ['junit', { outputFile: '../artifacts/test-results/frontend-e2e.xml' }],
  ],
  use: {
    baseURL,
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
  ],
})
