import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { useEffect } from 'react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, useLocation } from 'react-router-dom'
import { App } from '../../src/shell/App'

const VALID_EMAIL = 'verify-visitor@example.test'
const VALID_PASSWORD = 'Maple!River2026'
const SYNTHETIC_USER_ID = 'synthetic-user-id'
const SYNTHETIC_CODE = 'c3ludGhldGljLWNvZGU'

const STATUS_MESSAGE = 'Check your email to verify your account.'
const RESEND_SUCCESS_MESSAGE = 'Verification email sent.'
const INVALID_LINK_MESSAGE = 'This verification link is invalid or has expired.'
const RETRY_MESSAGE = 'We couldn\u2019t complete this request. Try again.'
const VERIFIED_MESSAGE = 'Your email is verified.'

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function registrationResponse(): Response {
  return jsonResponse(202, { status: 'verificationRequired' })
}

function resendResponse(retryAfterSeconds = 60): Response {
  return jsonResponse(202, { status: 'verificationRequested', retryAfterSeconds })
}

function confirmResponse(): Response {
  return jsonResponse(200, { status: 'verified' })
}

function invalidLinkResponse(): Response {
  return jsonResponse(400, {
    type: 'about:blank',
    title: 'Invalid or expired verification',
    status: 400,
    detail: 'The verification link is invalid or expired.',
    category: 'invalidOrExpiredVerification',
    correlationId: 'test-correlation',
  })
}

function verifiedSessionResponse(): Response {
  return jsonResponse(200, {
    status: 'signedIn',
    verificationStatus: 'verified',
    expiresAtUtc: '2026-09-08T01:40:00Z',
  })
}

function unverifiedSessionResponse(): Response {
  return jsonResponse(200, {
    status: 'signedIn',
    verificationStatus: 'verificationRequired',
    expiresAtUtc: '2026-09-08T01:40:00Z',
  })
}

function stubFetch(implementation: (url: string, init?: RequestInit) => Promise<Response>) {
  const calls: { url: string; init?: RequestInit }[] = []
  const spy = vi.fn(async (url: string, init?: RequestInit) => {
    calls.push({ url, init })
    return implementation(url, init)
  })
  vi.stubGlobal('fetch', spy)
  return { spy, calls }
}

function observedLocation() {
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
  return { seen, Probe }
}

function renderApp(path: string) {
  const { seen, Probe } = observedLocation()
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
      <Probe />
    </MemoryRouter>,
  )
  return seen
}

async function registerAndEnterVerifyPage(user: ReturnType<typeof userEvent.setup>) {
  renderApp('/register')
  await user.type(screen.getByLabelText('Email'), VALID_EMAIL)
  await user.type(screen.getByLabelText('Password'), VALID_PASSWORD)
  await user.type(screen.getByLabelText('Confirm password'), VALID_PASSWORD)
  await user.click(screen.getByRole('button', { name: 'Create account' }))
  expect(await screen.findByText(STATUS_MESSAGE)).toBeVisible()
  expect(await screen.findByRole('heading', { level: 1, name: 'Verify your email' })).toHaveFocus()
}

function expectAccountCallsOnly(calls: { url: string }[]) {
  expect(calls.length).toBeGreaterThan(0)
  for (const call of calls) {
    expect(call.url.startsWith('/api/accounts/')).toBe(true)
  }
}

async function flushPromises(rounds = 30) {
  for (let index = 0; index < rounds; index += 1) {
    await Promise.resolve()
  }
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.useRealTimers()
})

describe('verification status and guarded navigation (AC-001)', () => {
  it('shows the in-memory email and redirects unverified protected navigation without language work', async () => {
    const user = userEvent.setup()
    const { spy, calls } = stubFetch(async () => registrationResponse())
    await registerAndEnterVerifyPage(user)

    expect(screen.getByText(VALID_EMAIL)).toBeVisible()

    await user.click(screen.getByRole('link', { name: 'Translation' }))
    await waitFor(() =>
      expect(screen.getByRole('heading', { level: 1, name: 'Verify your email' })).toBeInTheDocument(),
    )
    expect(screen.queryByRole('heading', { name: 'Translation' })).not.toBeInTheDocument()
    expectAccountCallsOnly(calls)
    expect(spy).toHaveBeenCalledTimes(1)
  })
})

describe('verification resend (AC-002)', () => {
  it('sends exactly one email-only request, shows success, honors the server cooldown, then reenables', async () => {
    const user = userEvent.setup()
    stubFetch(async () => registrationResponse())
    await registerAndEnterVerifyPage(user)

    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'setInterval', 'clearInterval', 'Date'] })
    const { spy, calls } = stubFetch(
      async () =>
        ({
          status: 202,
          json: async () => ({ status: 'verificationRequested', retryAfterSeconds: 60 }),
        }) as Response,
    )
    const sendButton = screen.getByRole('button', { name: 'Send a new verification email' })
    await act(async () => {
      fireEvent.click(sendButton)
      await flushPromises()
    })

    expect(spy).toHaveBeenCalledTimes(1)
    expect(calls[0].url).toBe('/api/accounts/resend-verification')
    expect(calls[0].init?.method).toBe('POST')
    expect(Object.keys(JSON.parse(String(calls[0].init?.body))).sort()).toEqual(['email'])
    expect(JSON.parse(String(calls[0].init?.body))).toEqual({ email: VALID_EMAIL })

    expect(screen.getByText(RESEND_SUCCESS_MESSAGE)).toBeVisible()
    expect(sendButton).toBeDisabled()
    expect(screen.getByText('You can request another email in 60 seconds.')).toBeVisible()
    expect(document.activeElement?.textContent).toContain(RESEND_SUCCESS_MESSAGE)

    await act(async () => {
      await vi.advanceTimersByTimeAsync(59_000)
    })
    expect(sendButton).toBeDisabled()
    expect(screen.getByText('You can request another email in 1 seconds.')).toBeVisible()

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1000)
    })
    expect(sendButton).toBeEnabled()
    expectAccountCallsOnly(calls)
    expect(spy).toHaveBeenCalledTimes(1)
  })

  it('disables resend during flight and ignores duplicate activation', async () => {
    const user = userEvent.setup()
    stubFetch(async () => registrationResponse())
    await registerAndEnterVerifyPage(user)

    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    const { spy } = stubFetch(() => gate)

    const sendButton = screen.getByRole('button', { name: 'Send a new verification email' })
    fireEvent.click(sendButton)
    fireEvent.click(sendButton)
    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(sendButton).toBeDisabled()

    release(resendResponse(60))
    expect(await screen.findByText(RESEND_SUCCESS_MESSAGE)).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(1)
  })

  it('sends no request for a syntactically invalid email with linked errors and field focus', async () => {
    const user = userEvent.setup()
    stubFetch(async () => registrationResponse())
    await registerAndEnterVerifyPage(user)

    const { spy } = stubFetch(async () => resendResponse())
    const emailField = screen.getByLabelText('Email')
    await user.clear(emailField)
    await user.type(emailField, 'not-an-email')
    await user.click(screen.getByRole('button', { name: 'Send a new verification email' }))

    const matches = await screen.findAllByText('Enter a valid email address.')
    expect(matches.length).toBeGreaterThanOrEqual(2)
    for (const match of matches) expect(match).toBeVisible()
    const linked = matches
    expect(linked.some((node) => node.tagName.toLowerCase() === 'a')).toBe(true)
    expect(emailField).toHaveAttribute('aria-invalid', 'true')
    expect(emailField).toHaveFocus()
    expect(spy).not.toHaveBeenCalled()
  })
})

describe('verification link consumption (AC-003)', () => {
  it('posts delivered material once as a JSON body, strips the query and offers signed-out continuation', async () => {
    const { spy, calls } = stubFetch(async (url) => {
      if (url === '/api/accounts/confirm-email') return confirmResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    const seen = renderApp(`/verify-email?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`)

    expect(await screen.findByText(VERIFIED_MESSAGE)).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(1)
    expect(calls[0].url).toBe('/api/accounts/confirm-email')
    expect(calls[0].init?.method).toBe('POST')
    expect(Object.keys(JSON.parse(String(calls[0].init?.body))).sort()).toEqual(['code', 'userId'])
    expect(JSON.parse(String(calls[0].init?.body))).toEqual({
      userId: SYNTHETIC_USER_ID,
      code: SYNTHETIC_CODE,
    })

    expect(seen.search).toBe('')
    expect(seen.pathname).toBe('/verify-email')
    const continuation = screen.getByRole('link', { name: 'Go to sign in' })
    expect(continuation).toHaveAttribute('href', '/login')
    expect(document.activeElement?.textContent).toContain(VERIFIED_MESSAGE)
    expect(localStorage.length).toBe(0)
    expect(sessionStorage.length).toBe(0)
    expect(document.body.textContent).not.toContain(SYNTHETIC_USER_ID)
    expect(document.body.textContent).not.toContain(SYNTHETIC_CODE)
  })

  it('offers protected continuation after a verified session read with a single bodyless GET', async () => {
    const user = userEvent.setup()
    stubFetch(async () => registrationResponse())
    await registerAndEnterVerifyPage(user)

    const { spy, calls } = stubFetch(async (url, init) => {
      if (url === '/api/accounts/session') return verifiedSessionResponse()
      throw new Error(`unexpected request ${String(init?.method)} ${url}`)
    })
    await user.click(screen.getByRole('button', { name: 'I’ve verified my email' }))

    expect(await screen.findByText(VERIFIED_MESSAGE)).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(1)
    expect(calls[0].url).toBe('/api/accounts/session')
    expect(calls[0].init?.method).toBe('GET')
    expect(calls[0].init?.body).toBeUndefined()
    expect(screen.getByRole('link', { name: 'Continue to Translation' })).toHaveAttribute(
      'href',
      '/translate',
    )
  })
})

describe('verification invalid and status variants (AC-004)', () => {
  it.each([
    { name: 'expired material', path: `/verify-email?userId=${SYNTHETIC_USER_ID}&code=ZXhwaXJlZA` },
    { name: 'mismatched material', path: '/verify-email?userId=someone-else&code=bWlzbWF0Y2hlZA' },
  ])('shows the invalid-link message with a resend path for $name', async ({ path }) => {
    const { spy } = stubFetch(async () => invalidLinkResponse())
    renderApp(path)

    expect(await screen.findByText(INVALID_LINK_MESSAGE)).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(1)
    expect(
      screen.getByRole('button', { name: 'Send a new verification email' }),
    ).toBeInTheDocument()
    expect(document.activeElement?.textContent).toContain('invalid or has expired')
  })

  it.each([
    { name: 'unknown query shape', path: '/verify-email?token=abc123' },
    { name: 'missing code', path: `/verify-email?userId=${SYNTHETIC_USER_ID}` },
    { name: 'empty material', path: '/verify-email?userId=&code=' },
  ])('treats $name as malformed without posting confirmation', async ({ path }) => {
    const { spy } = stubFetch(async () => invalidLinkResponse())
    const seen = renderApp(path)

    expect(await screen.findByText(INVALID_LINK_MESSAGE)).toBeVisible()
    expect(spy).not.toHaveBeenCalled()
    expect(seen.search).toBe('')
    expect(
      screen.getByRole('button', { name: 'Send a new verification email' }),
    ).toBeInTheDocument()
  })

  it('keeps a still-unverified session on the page with guidance after one session read', async () => {
    const user = userEvent.setup()
    stubFetch(async () => registrationResponse())
    await registerAndEnterVerifyPage(user)

    const { spy, calls } = stubFetch(async () => unverifiedSessionResponse())
    await user.click(screen.getByRole('button', { name: 'I’ve verified my email' }))

    expect(
      await screen.findByText(
        'Your email is still unverified. Open the link in your inbox, or send a new verification email below.',
      ),
    ).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(1)
    expect(calls[0].url).toBe('/api/accounts/session')
    expect(screen.queryByText(VERIFIED_MESSAGE)).not.toBeInTheDocument()
    expectAccountCallsOnly(calls)
  })
})

describe('verification failure and ordering (AC-005)', () => {
  it('shows the generic retry message without false success and permits one explicit retry', async () => {
    const user = userEvent.setup()
    stubFetch(async () => registrationResponse())
    await registerAndEnterVerifyPage(user)

    const { spy } = stubFetch(async () => {
      throw new TypeError('network down')
    })
    await user.click(screen.getByRole('button', { name: 'Send a new verification email' }))

    expect(await screen.findByText(RETRY_MESSAGE)).toBeVisible()
    expect(screen.queryByText(RESEND_SUCCESS_MESSAGE)).not.toBeInTheDocument()
    expect(spy).toHaveBeenCalledTimes(1)

    const { spy: retrySpy } = stubFetch(async () => resendResponse(60))
    await user.click(screen.getByRole('button', { name: 'Send a new verification email' }))
    expect(await screen.findByText(RESEND_SUCCESS_MESSAGE)).toBeVisible()
    expect(retrySpy).toHaveBeenCalledTimes(1)
  })

  it('discards a stale confirmation that resolves after navigation without restoring query state', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    const { spy } = stubFetch(async () => gate)
    const seen = renderApp(`/verify-email?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`)

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(screen.getByText('Confirming your email…')).toBeVisible()

    await user.click(screen.getByRole('link', { name: 'Translation' }))
    await waitFor(() => expect(seen.pathname).toBe('/login'))

    release(confirmResponse())
    await gate
    await waitFor(() => expect(seen.pathname).toBe('/login'))
    expect(seen.search).toBe('')
    expect(screen.queryByText(VERIFIED_MESSAGE)).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'Sign in' })).toBeInTheDocument()
    expect(spy).toHaveBeenCalledTimes(1)
  })

  it('routes direct no-material entry back to registration', async () => {
    stubFetch(async () => registrationResponse())
    const seen = renderApp('/verify-email')

    expect(await screen.findByRole('heading', { level: 1, name: 'Create account' })).toBeVisible()
    expect(seen.pathname).toBe('/register')
  })
})

describe('verification keyboard and focus (AC-006)', () => {
  it('submits resend once with Enter and keeps native controls operable', async () => {
    const user = userEvent.setup()
    stubFetch(async () => registrationResponse())
    await registerAndEnterVerifyPage(user)

    const { spy } = stubFetch(async () => resendResponse(60))
    const emailField = screen.getByLabelText('Email')
    emailField.focus()
    await user.keyboard('{Enter}')

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(await screen.findByText(RESEND_SUCCESS_MESSAGE)).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(1)
    expect(screen.getByRole('button', { name: 'I’ve verified my email' })).toBeEnabled()
  })
})

describe('verification privacy boundary (AC-007)', () => {
  it('keeps delivered material out of storage, history entries and the rendered page', async () => {
    const { spy } = stubFetch(async (url) => {
      if (url === '/api/accounts/confirm-email') return confirmResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    const seen = renderApp(`/verify-email?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`)

    expect(await screen.findByText(VERIFIED_MESSAGE)).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(1)
    expect(localStorage.length).toBe(0)
    expect(sessionStorage.length).toBe(0)
    expect(seen.search).toBe('')
    expect(window.location.href).not.toContain(SYNTHETIC_USER_ID)
    expect(window.location.href).not.toContain(SYNTHETIC_CODE)
    expect(document.body.textContent).not.toContain(SYNTHETIC_USER_ID)
    expect(document.body.textContent).not.toContain(SYNTHETIC_CODE)
  })
})
