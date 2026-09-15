import { defineConfig, devices } from '@playwright/test'

/**
 * M043 opt-in live browser config. Used only by `scripts/backend.sh e2e-live`
 * with LINGUADESK_LIVE_URL pointing at the serving host. The default
 * `playwright.config.ts` (testDir ./tests/e2e) never picks up ./tests/e2e-live,
 * so live suites stay out of `check`, `smoke` and CI by construction.
 */
export default defineConfig({
  testDir: './tests/e2e-live',
  outputDir: '../artifacts/playwright-live',
  globalTimeout: 120_000,
  timeout: 90_000,
  expect: { timeout: 60_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [
    ['list'],
    ['junit', { outputFile: '../artifacts/test-results/frontend-live.xml' }],
  ],
  use: {
    baseURL: process.env.LINGUADESK_LIVE_URL,
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
