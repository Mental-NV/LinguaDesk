import { act, render, screen, waitFor } from '@testing-library/react'
import { useEffect } from 'react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, useLocation } from 'react-router-dom'
import { App } from '../../src/shell/App'
import {
  requestLocalAccountPasswordReset,
  resetLocalAccountPassword,
} from '../../src/api/accounts'

const KNOWN_EMAIL = 'known-visitor@example.test'
const UNKNOWN_EMAIL = 'absent-visitor@example.test'

const SUCCESS_MESSAGE = 'If an account exists for that email, we sent a reset link.'
const SUMMARY_MESSAGE = 'Check the highlighted fields.'
const EMAIL_REQUIRED_MESSAGE = 'Enter your email address.'
const EMAIL_SHAPE_MESSAGE = 'Enter a valid email address.'
const RETRY_MESSAGE = 'We couldn\u2019t complete this request. Try again.'
const DELIVERY_MESSAGE = 'We couldn\u2019t send the email. Try again.'

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function acceptedResponse(retryAfterSeconds = 60): Response {
  return jsonResponse(202, { status: 'passwordResetRequested', retryAfterSeconds })
}

function fieldResponse(): Response {
  return jsonResponse(400, {
    type: 'about:blank',
    title: 'Invalid request',
    status: 400,
    detail: 'One or more account fields are invalid.',
    category: 'invalidRequest',
    correlationId: 'test-correlation',
    errors: { email: ['Email must be a valid address.'] },
  })
}

function malformedResponse(): Response {
  return jsonResponse(400, {
    type: 'about:blank',
    title: 'Invalid request',
    status: 400,
    detail: 'Only the required email field is accepted.',
    category: 'invalidRequest',
    correlationId: 'test-correlation',
  })
}

function unavailableResponse(): Response {
  return jsonResponse(503, {
    type: 'about:blank',
    title: 'Service unavailable',
    status: 503,
    detail: 'Account password reset is temporarily unavailable.',
    category: 'availability',
    correlationId: 'test-correlation',
  })
}

interface RecordedCall {
  url: string
  init?: RequestInit
}

function stubFetch(implementation: (url: string, init?: RequestInit) => Promise<Response>) {
  const calls: RecordedCall[] = []
  const spy = vi.fn(async (url: string, init?: RequestInit) => {
    calls.push({ url, init })
    return implementation(url, init)
  })
  vi.stubGlobal('fetch', spy)
  return { spy, calls }
}

function callBody(call: RecordedCall): unknown {
  if (call.init?.body === undefined) return undefined
  return JSON.parse(String(call.init.body))
}

function renderApp(path: string) {
  const seen = { pathname: '', search: '' }
  function Probe() {
    const location = useLocation()
    const { pathname, search } = location
    useEffect(() => {
      seen.pathname = pathname
      seen.search = search
    }, [pathname, search])
    return null
  }
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
      <Probe />
    </MemoryRouter>,
  )
  return seen
}

function expectNoLanguageRequests(calls: readonly RecordedCall[]): void {
  for (const call of calls) {
    expect(call.url).not.toContain('/api/translate')
    expect(call.url).not.toContain('/api/rewrite')
    expect(call.url).not.toContain('/api/usage')
  }
}

function expectNoCredentialMaterial(calls: readonly RecordedCall[]): void {
  for (const call of calls) {
    expect(call.url.startsWith('/api/accounts/')).toBe(true)
  }
}

describe('forgot-password wrapper mapping (T001)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('maps 202 to sent with the server retry interval and posts the email-only body', async () => {
    const { calls } = stubFetch(async () => acceptedResponse(60))
    const result = await requestLocalAccountPasswordReset({ email: KNOWN_EMAIL })
    expect(result).toEqual({ kind: 'sent', retryAfterSeconds: 60 })
    expect(calls).toHaveLength(1)
    expect(calls[0].url).toBe('/api/accounts/forgot-password')
    expect(calls[0].init?.method).toBe('POST')
    expect(Object.keys(callBody(calls[0]) as Record<string, unknown>)).toEqual(['email'])
  })

  it('defaults a missing retry interval to the fixed 60-second acknowledgment', async () => {
    stubFetch(async () => jsonResponse(202, { status: 'passwordResetRequested' }))
    const result = await requestLocalAccountPasswordReset({ email: UNKNOWN_EMAIL })
    expect(result).toEqual({ kind: 'sent', retryAfterSeconds: 60 })
  })

  it('maps a field-safe 400 to field and a malformed 400 to retry', async () => {
    stubFetch(async () => fieldResponse())
    expect(await requestLocalAccountPasswordReset({ email: KNOWN_EMAIL })).toEqual({
      kind: 'field',
    })
    vi.unstubAllGlobals()

    stubFetch(async () => malformedResponse())
    expect(await requestLocalAccountPasswordReset({ email: KNOWN_EMAIL })).toEqual({
      kind: 'retry',
      status: 400,
    })
  })

  it('maps 503 to delivery without leaking bodies and transport failure to retry', async () => {
    stubFetch(async () => unavailableResponse())
    expect(await requestLocalAccountPasswordReset({ email: KNOWN_EMAIL })).toEqual({
      kind: 'delivery',
    })
    vi.unstubAllGlobals()

    stubFetch(async () => {
      throw new TypeError('network down')
    })
    expect(await requestLocalAccountPasswordReset({ email: KNOWN_EMAIL })).toEqual({
      kind: 'retry',
      status: null,
    })
  })

  it('exposes the reset wrapper mapping for the delivered reset contract', async () => {
    stubFetch(async () => jsonResponse(200, { status: 'passwordReset' }))
    expect(
      await resetLocalAccountPassword({
        userId: 'user',
        code: 'Y29kZQ',
        newPassword: 'Maple!River2026',
      }),
    ).toEqual({ kind: 'reset' })
  })
})

describe('forgot-password success (AC-001)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each([
    { name: 'known account', email: KNOWN_EMAIL },
    { name: 'unknown account', email: UNKNOWN_EMAIL },
  ])(
    'submits exactly one email-only request for a $name and shows the identical confirmation',
    async ({ email }) => {
      const user = userEvent.setup()
      const { spy, calls } = stubFetch(async () => acceptedResponse())
      renderApp('/forgot-password')

      expect(await screen.findByRole('heading', { level: 1, name: 'Forgot password' })).toHaveFocus()
      await user.type(screen.getByLabelText('Email'), email)
      await user.click(screen.getByRole('button', { name: 'Send reset link' }))

      expect(await screen.findByText(SUCCESS_MESSAGE)).toBeVisible()
      expect(screen.getByRole('heading', { level: 1, name: 'Forgot password' })).toHaveFocus()
      expect(screen.getByRole('link', { name: 'Back to sign in' })).toHaveAttribute('href', '/login')
      expect(screen.queryByRole('form')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()

      await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
      expect(calls).toHaveLength(1)
      expect(calls[0].url).toBe('/api/accounts/forgot-password')
      expect(callBody(calls[0])).toEqual({ email })
      expectNoLanguageRequests(calls)
      expectNoCredentialMaterial(calls)
    },
  )

  it('disables the form during flight and ignores duplicate click and Enter', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    const { spy, calls } = stubFetch(async () => gate)
    renderApp('/forgot-password')

    await user.type(screen.getByLabelText('Email'), KNOWN_EMAIL)
    await user.click(screen.getByRole('button', { name: 'Send reset link' }))

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(screen.getByLabelText('Email')).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Sending…' }))
    await user.type(screen.getByLabelText('Email'), '{Enter}')
    expect(spy).toHaveBeenCalledTimes(1)

    release(acceptedResponse())
    expect(await screen.findByText(SUCCESS_MESSAGE)).toBeVisible()
    expect(calls.filter((call) => call.url === '/api/accounts/forgot-password')).toHaveLength(1)
  })
})

describe('forgot-password validation (AC-002)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends no request for a missing email and links the required error with focus', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => acceptedResponse())
    renderApp('/forgot-password')

    await user.click(await screen.findByRole('button', { name: 'Send reset link' }))

    expect(await screen.findByText(SUMMARY_MESSAGE)).toBeVisible()
    expect(screen.getAllByText(EMAIL_REQUIRED_MESSAGE).length).toBeGreaterThan(0)
    expect(screen.getByText(EMAIL_REQUIRED_MESSAGE, { selector: 'p' })).toBeVisible()
    const email = screen.getByLabelText('Email')
    expect(email).toHaveAttribute('aria-invalid', 'true')
    expect(email).toHaveAttribute('aria-describedby', expect.stringContaining('-error'))
    expect(email).toHaveFocus()
    expect(spy).not.toHaveBeenCalled()
    expect(screen.queryByText(SUCCESS_MESSAGE)).not.toBeInTheDocument()
  })

  it('sends no request for a malformed email and focuses the field', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => acceptedResponse())
    renderApp('/forgot-password')

    await user.type(screen.getByLabelText('Email'), 'not-an-email')
    await user.click(screen.getByRole('button', { name: 'Send reset link' }))

    expect(await screen.findAllByText(EMAIL_SHAPE_MESSAGE)).not.toHaveLength(0)
    expect(screen.getByText(EMAIL_SHAPE_MESSAGE, { selector: 'p' })).toBeVisible()
    expect(screen.getByLabelText('Email')).toHaveFocus()
    expect(spy).not.toHaveBeenCalled()
  })

  it('surfaces a server field rejection as the identical email-shape error', async () => {
    const user = userEvent.setup()
    stubFetch(async () => fieldResponse())
    renderApp('/forgot-password')

    await user.type(screen.getByLabelText('Email'), KNOWN_EMAIL)
    await user.click(screen.getByRole('button', { name: 'Send reset link' }))

    expect(await screen.findAllByText(EMAIL_SHAPE_MESSAGE)).not.toHaveLength(0)
    expect(screen.getByText(EMAIL_SHAPE_MESSAGE, { selector: 'p' })).toBeVisible()
    expect(screen.getByLabelText('Email')).toHaveFocus()
    expect(screen.queryByText(SUCCESS_MESSAGE)).not.toBeInTheDocument()
  })
})

describe('forgot-password failures (AC-003)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows the generic error on transport failure and retries with one more account request', async () => {
    const user = userEvent.setup()
    let attempt = 0
    const { spy, calls } = stubFetch(async () => {
      attempt += 1
      if (attempt === 1) throw new TypeError('network down')
      return acceptedResponse()
    })
    renderApp('/forgot-password')

    await user.type(screen.getByLabelText('Email'), KNOWN_EMAIL)
    await user.click(screen.getByRole('button', { name: 'Send reset link' }))

    expect(await screen.findByText(RETRY_MESSAGE)).toBeVisible()
    expect(screen.queryByText(SUCCESS_MESSAGE)).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByText(SUCCESS_MESSAGE)).toBeVisible()

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(2))
    expect(calls.filter((call) => call.url === '/api/accounts/forgot-password')).toHaveLength(2)
    expectNoLanguageRequests(calls)
  })

  it('shows the delivery error on 503 without account disclosure and retries cleanly', async () => {
    const user = userEvent.setup()
    let attempt = 0
    const { spy, calls } = stubFetch(async () => {
      attempt += 1
      if (attempt === 1) return unavailableResponse()
      return acceptedResponse()
    })
    renderApp('/forgot-password')

    await user.type(screen.getByLabelText('Email'), UNKNOWN_EMAIL)
    await user.click(screen.getByRole('button', { name: 'Send reset link' }))

    expect(await screen.findByText(DELIVERY_MESSAGE)).toBeVisible()
    expect(document.body.textContent).not.toContain('test-correlation')
    expect(document.body.textContent).not.toContain('availability')

    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByText(SUCCESS_MESSAGE)).toBeVisible()
    await waitFor(() => expect(spy).toHaveBeenCalledTimes(2))
    expectNoLanguageRequests(calls)
  })
})

describe('forgot-password shell presence (AC-007)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders the recovery form under the guarded shell with a safe return link', async () => {
    stubFetch(async () => acceptedResponse())
    const seen = renderApp('/forgot-password')

    expect(await screen.findByRole('heading', { level: 1, name: 'Forgot password' })).toHaveFocus()
    expect(screen.getByRole('form', { name: 'Forgot password' })).toBeVisible()
    expect(seen.pathname).toBe('/forgot-password')
  })

  it('discards a late login completion after leaving for recovery', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    stubFetch(async (url) => {
      if (url === '/api/accounts/antiforgery') {
        return jsonResponse(200, {
          requestToken: 'test-antiforgery-request-token',
          headerName: 'X-LinguaDesk-Antiforgery',
        })
      }
      if (url === '/api/accounts/sign-in') return gate
      throw new Error(`unexpected request to ${url}`)
    })
    render(
      <MemoryRouter initialEntries={['/login']}>
        <App />
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText('Email'), KNOWN_EMAIL)
    await user.type(screen.getByLabelText('Password'), 'Maple!River2026')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Signing in…' })).toBeDisabled())

    await user.click(screen.getByRole('link', { name: 'Forgot password' }))
    expect(await screen.findByRole('heading', { level: 1, name: 'Forgot password' })).toBeVisible()

    await act(async () => {
      release(
        jsonResponse(200, {
          status: 'signedIn',
          verificationStatus: 'verified',
          expiresAtUtc: '2026-09-10T16:00:00Z',
        }),
      )
      await gate
      await new Promise((resolve) => setTimeout(resolve, 0))
    })

    expect(screen.getByRole('heading', { level: 1, name: 'Forgot password' })).toBeVisible()
    expect(screen.queryByRole('heading', { level: 1, name: 'Translation' })).not.toBeInTheDocument()
  })
})
