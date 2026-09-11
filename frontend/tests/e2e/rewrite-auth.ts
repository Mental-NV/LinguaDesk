import { request, type Browser } from '@playwright/test'

/**
 * Documented real-auth fixture (#6 section 4.1/4.5): obtains antiforgery state
 * and signs the single predefined verified Smoke account in through the real
 * published account API, then supplies the resulting same-origin cookie storage
 * state to browser contexts. It skips only form interaction: no forged cookies,
 * no session-row writes, no disabled middleware, no test login endpoint, no
 * intercepted account/feature APIs and no in-memory replacement host.
 */
export async function verifiedAccountStorageState(browser: Browser) {
  void browser
  const baseURL = process.env.LINGUADESK_PUBLISHED_URL
  const email = process.env.LINGUADESK_SMOKE_VERIFIED_EMAIL
  const password = process.env.LINGUADESK_SMOKE_ACCOUNT_PASSWORD
  if (!baseURL || !email || !password) {
    throw new Error('The published rewrite smoke account configuration is required.')
  }

  const api = await request.newContext({ baseURL, ignoreHTTPSErrors: true })
  try {
    const bootstrap = await api.get('/api/accounts/antiforgery')
    if (!bootstrap.ok()) {
      throw new Error(`Antiforgery bootstrap failed with status ${bootstrap.status()}.`)
    }
    const bootstrapBody = (await bootstrap.json()) as { requestToken?: unknown }
    if (typeof bootstrapBody.requestToken !== 'string' || bootstrapBody.requestToken.length === 0) {
      throw new Error('Antiforgery bootstrap returned no request token.')
    }
    const signIn = await api.post('/api/accounts/sign-in', {
      data: { email, password },
      headers: { 'X-LinguaDesk-Antiforgery': bootstrapBody.requestToken },
    })
    if (!signIn.ok()) {
      throw new Error(`Published sign-in failed with status ${signIn.status()}.`)
    }
    return await api.storageState()
  } finally {
    await api.dispose()
  }
}
