import { act, render, screen, waitFor } from '@testing-library/react'
import { useEffect } from 'react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, useNavigate } from 'react-router-dom'
import { App } from '../../src/shell/App'
import { resolveSafeReturn } from '../../src/auth/safeReturn'

const VALID_EMAIL = 'returning-visitor@example.test'
const VALID_PASSWORD = 'Maple!River2026'
const REQUEST_TOKEN = 'test-antiforgery-request-token'
const EXPIRES_AT = '2026-09-10T16:00:00Z'

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function emptyResponse(status: number): Response {
  return new Response(null, { status })
}

function antiforgeryResponse(): Response {
  return jsonResponse(200, { requestToken: REQUEST_TOKEN, headerName: 'X-LinguaDesk-Antiforgery' })
}

function verifiedSignInResponse(): Response {
  return jsonResponse(200, {
    status: 'signedIn',
    verificationStatus: 'verified',
    expiresAtUtc: EXPIRES_AT,
  })
}

function unverifiedSignInResponse(): Response {
  return jsonResponse(200, {
    status: 'signedIn',
    verificationStatus: 'verificationRequired',
    expiresAtUtc: EXPIRES_AT,
  })
}

function invalidCredentialsResponse(): Response {
  return jsonResponse(401, {
    type: 'about:blank',
    title: 'Invalid credentials',
    status: 401,
    detail: 'The supplied credentials are invalid.',
    category: 'invalidCredentials',
    correlationId: 'test-correlation',
  })
}

function sessionUnauthorizedResponse(): Response {
  return jsonResponse(401, {
    type: 'about:blank',
    title: 'Authentication required',
    status: 401,
    detail: 'Sign in to continue.',
    category: 'authenticationRequired',
    correlationId: 'test-correlation',
  })
}

function verifiedSessionResponse(): Response {
  return jsonResponse(200, {
    status: 'signedIn',
    verificationStatus: 'verified',
    expiresAtUtc: EXPIRES_AT,
  })
}

function unverifiedSessionResponse(): Response {
  return jsonResponse(200, {
    status: 'signedIn',
    verificationStatus: 'verificationRequired',
    expiresAtUtc: EXPIRES_AT,
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

function callHeader(call: RecordedCall, name: string): string | undefined {
  const headers = call.init?.headers as Record<string, string> | undefined
  return headers?.[name]
}

function renderApp(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

let probeNavigate: (path: string) => void = () => {}

function NavigateProbe() {
  const navigate = useNavigate()
  useEffect(() => {
    probeNavigate = navigate
  }, [navigate])
  return null
}

function renderAppWithProbe(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
      <NavigateProbe />
    </MemoryRouter>,
  )
}

function goTo(path: string): void {
  act(() => {
    probeNavigate(path)
  })
}

async function fillLoginForm(
  user: ReturnType<typeof userEvent.setup>,
  email = VALID_EMAIL,
  password = VALID_PASSWORD,
) {
  await user.type(screen.getByLabelText('Email'), email)
  await user.type(screen.getByLabelText('Password'), password)
}

async function submitLoginForm(
  user: ReturnType<typeof userEvent.setup>,
  email = VALID_EMAIL,
  password = VALID_PASSWORD,
) {
  await fillLoginForm(user, email, password)
  await user.click(screen.getByRole('button', { name: 'Sign in' }))
}

function expectNoLanguageRequests(calls: readonly RecordedCall[]): void {
  for (const call of calls) {
    expect(call.url).not.toContain('/api/translate')
    expect(call.url).not.toContain('/api/rewrite')
    expect(call.url).not.toContain('/api/usage')
  }
}

describe('verified sign-in (AC-001)', () => {
  it('submits one bootstrap plus one email/password-only POST and reaches the default route with heading focus', async () => {
    const user = userEvent.setup()
    const { spy, calls } = stubFetch(async (url) => {
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') return verifiedSignInResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    renderApp('/login')

    expect(await screen.findByRole('heading', { level: 1, name: 'Sign in' })).toHaveFocus()
    await submitLoginForm(user)

    const destinationHeading = await screen.findByRole('heading', { level: 1, name: 'Translation' })
    expect(destinationHeading).toHaveFocus()
    expect(await screen.findByText('The translation workspace arrives in a later update.')).toBeVisible()

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(2))
    expect(calls[0].url).toBe('/api/accounts/antiforgery')
    expect(calls[0].init?.method).toBe('GET')
    expect(calls[1].url).toBe('/api/accounts/sign-in')
    expect(calls[1].init?.method).toBe('POST')
    expect(Object.keys(callBody(calls[1]) as Record<string, unknown>).sort()).toEqual([
      'email',
      'password',
    ])
    expect(callBody(calls[1])).toEqual({ email: VALID_EMAIL, password: VALID_PASSWORD })
    expect(callHeader(calls[1], 'X-LinguaDesk-Antiforgery')).toBe(REQUEST_TOKEN)

    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()
    expect(document.body.textContent).not.toContain(VALID_PASSWORD)
    expect(document.body.textContent).not.toContain(REQUEST_TOKEN)
    expect(spy).toHaveBeenCalledTimes(2)
    expectNoLanguageRequests(calls)
  })

  it('returns to the remembered safe protected route after sign-in', async () => {
    const user = userEvent.setup()
    stubFetch(async (url) => {
      if (url === '/api/accounts/session') return unverifiedSessionResponse()
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') return verifiedSignInResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    renderAppWithProbe('/rewrite')

    expect(await screen.findByRole('heading', { level: 1, name: 'Verify your email' })).toBeVisible()

    goTo('/login')
    expect(await screen.findByRole('form', { name: 'Sign in' })).toBeVisible()
    await submitLoginForm(user)

    const destinationHeading = await screen.findByRole('heading', { level: 1, name: 'Rewriting' })
    expect(destinationHeading).toHaveFocus()
    expect(
      await screen.findByText('The rewriting workspace arrives in a later update.'),
    ).toBeVisible()
  })

  it('disables fields during flight and ignores duplicate click and Enter', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    const { spy, calls } = stubFetch(async (url) => {
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') return gate
      throw new Error(`unexpected request to ${url}`)
    })
    renderApp('/login')

    await fillLoginForm(user)
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(2))
    expect(screen.getByLabelText('Email')).toBeDisabled()
    expect(screen.getByLabelText('Password')).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Signing in…' }))
    await user.type(screen.getByLabelText('Password'), '{Enter}')
    expect(spy).toHaveBeenCalledTimes(2)

    release(verifiedSignInResponse())
    expect(await screen.findByRole('heading', { level: 1, name: 'Translation' })).toBeVisible()
    expect(calls.filter((call) => call.url === '/api/accounts/sign-in')).toHaveLength(1)
  })

  it('submits once with Enter from the password field', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async (url) => {
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') return verifiedSignInResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    renderApp('/login')

    await user.type(screen.getByLabelText('Email'), VALID_EMAIL)
    await user.type(screen.getByLabelText('Password'), `${VALID_PASSWORD}[Enter]`)

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(2))
    expect(await screen.findByRole('heading', { level: 1, name: 'Translation' })).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(2)
  })
})

describe('invalid and unverified credentials (AC-002)', () => {
  it.each([
    { name: 'unknown email', email: 'absent-visitor@example.test', password: VALID_PASSWORD },
    { name: 'wrong password', email: VALID_EMAIL, password: 'WrongPassword15!' },
  ])(
    'shows the shared invalid message for $name with cleared password, kept email and email focus',
    async ({ email, password }) => {
      const user = userEvent.setup()
      const { spy, calls } = stubFetch(async (url) => {
        if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
        if (url === '/api/accounts/sign-in') return invalidCredentialsResponse()
        throw new Error(`unexpected request to ${url}`)
      })
      renderApp('/login')

      await submitLoginForm(user, email, password)

      expect(await screen.findByText('Email or password is incorrect.')).toBeVisible()
      expect(screen.getByLabelText('Password')).toHaveValue('')
      expect(screen.getByLabelText('Email')).toHaveValue(email)
      expect(screen.getByLabelText('Email')).toHaveAttribute('aria-invalid', 'true')
      expect(screen.getByLabelText('Email')).toHaveFocus()
      expect(screen.queryByRole('heading', { level: 1, name: 'Translation' })).not.toBeInTheDocument()
      expect(document.body.textContent).not.toContain(password)
      expect(document.body.textContent).not.toContain('absent-visitor')
      await waitFor(() => expect(spy).toHaveBeenCalledTimes(2))
      expect(calls[1].url).toBe('/api/accounts/sign-in')
      expectNoLanguageRequests(calls)
    },
  )

  it('routes an unverified sign-in to verification with the typed email and no language work', async () => {
    const user = userEvent.setup()
    const { spy, calls } = stubFetch(async (url) => {
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') return unverifiedSignInResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    renderApp('/login')

    await submitLoginForm(user)

    expect(await screen.findByRole('heading', { level: 1, name: 'Verify your email' })).toBeVisible()
    expect(screen.getByDisplayValue(VALID_EMAIL)).toBeVisible()
    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()
    expect(document.body.textContent).not.toContain(VALID_PASSWORD)
    await waitFor(() => expect(spy).toHaveBeenCalledTimes(2))
    expectNoLanguageRequests(calls)
  })

  it('preserves the remembered route through verification to the protected continuation', async () => {
    const user = userEvent.setup()
    let sessionVerified = false
    stubFetch(async (url) => {
      if (url === '/api/accounts/session') {
        return sessionVerified ? verifiedSessionResponse() : unverifiedSessionResponse()
      }
      throw new Error(`unexpected request to ${url}`)
    })
    renderApp('/rewrite')

    expect(await screen.findByRole('heading', { level: 1, name: 'Verify your email' })).toBeVisible()

    sessionVerified = true
    await user.click(screen.getByRole('button', { name: 'I’ve verified my email' }))

    expect(await screen.findByText('Your email is verified.')).toBeVisible()
    expect(screen.getByRole('link', { name: 'Continue to Rewriting' })).toHaveAttribute(
      'href',
      '/rewrite',
    )
  })
})

describe('session expiry teardown (AC-003)', () => {
  it('clears state on an observed 401, shows MSG-037 and resets the safe return to default', async () => {
    const user = userEvent.setup()
    const { spy, calls } = stubFetch(async (url) => {
      if (url === '/api/accounts/session') return sessionUnauthorizedResponse()
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') return verifiedSignInResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    renderApp('/rewrite')

    expect(
      await screen.findByText('Your session expired. Sign in again to continue.'),
    ).toBeVisible()
    expect(await screen.findByRole('form', { name: 'Sign in' })).toBeVisible()
    expect(screen.getByLabelText('Password')).toHaveValue('')

    await submitLoginForm(user)

    const destinationHeading = await screen.findByRole('heading', { level: 1, name: 'Translation' })
    expect(destinationHeading).toHaveFocus()
    expect(
      screen.queryByText('Your session expired. Sign in again to continue.'),
    ).not.toBeInTheDocument()
    expect(spy).toHaveBeenCalled()
    expectNoLanguageRequests(calls)
  })

  it('discards a late sign-in completion after leaving for recovery without restoring state', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    stubFetch(async (url) => {
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') return gate
      throw new Error(`unexpected request to ${url}`)
    })
    renderApp('/login')

    await fillLoginForm(user)
    await user.click(screen.getByRole('button', { name: 'Sign in' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Signing in…' })).toBeDisabled())

    await user.click(screen.getByRole('link', { name: 'Forgot password' }))
    expect(await screen.findByRole('heading', { level: 1, name: 'Forgot password' })).toBeVisible()

    release(verifiedSignInResponse())
    await gate
    await new Promise((resolve) => setTimeout(resolve, 0))

    expect(screen.getByRole('heading', { level: 1, name: 'Forgot password' })).toBeVisible()
    expect(screen.queryByRole('heading', { level: 1, name: 'Translation' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()
  })
})

describe('inline sign-out (AC-004)', () => {
  function stubSignedInSession(signOut: () => Promise<Response>) {
    return stubFetch(async (url) => {
      if (url === '/api/accounts/session') return verifiedSessionResponse()
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-out') return signOut()
      throw new Error(`unexpected request to ${url}`)
    })
  }

  it('clears immediately with Signing out and exposes the ordinary form on the real 204', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    const { spy, calls } = stubSignedInSession(() => gate)
    renderApp('/translate')

    await waitFor(() =>
      expect(screen.getByRole('heading', { level: 1, name: 'Translation' })).toHaveFocus(),
    )
    await user.click(screen.getByRole('button', { name: 'Sign out' }))

    expect(await screen.findByText('Signing out…')).toBeVisible()
    expect(screen.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()

    release(emptyResponse(204))
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Sign in' })).toBeVisible(),
    )
    expect(screen.queryByText('Signing out…')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toBeVisible()

    for (const call of calls) {
      expect(
        [
          '/api/accounts/session',
          '/api/accounts/antiforgery',
          '/api/accounts/sign-out',
        ].includes(call.url),
      ).toBe(true)
    }
    const signOutCalls = calls.filter((call) => call.url === '/api/accounts/sign-out')
    expect(signOutCalls).toHaveLength(1)
    expect(signOutCalls[0].init?.method).toBe('POST')
    expect(signOutCalls[0].init?.body).toBeUndefined()
    expect(callHeader(signOutCalls[0], 'X-LinguaDesk-Antiforgery')).toBe(REQUEST_TOKEN)
    expect(spy).toHaveBeenCalled()
    expectNoLanguageRequests(calls)

    await user.click(screen.getByRole('link', { name: 'Translation' }))
    await waitFor(() =>
      expect(screen.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible(),
    )
    expect(
      screen.queryByText('The translation workspace arrives in a later update.'),
    ).not.toBeInTheDocument()
  })

  it('shows the failure text with an auth-only retry that sends a single fresh sign-out', async () => {
    const user = userEvent.setup()
    let attempt = 0
    const { spy, calls } = stubSignedInSession(async () => {
      attempt += 1
      return attempt === 1 ? emptyResponse(500) : emptyResponse(204)
    })
    renderApp('/translate')

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeVisible()
    await user.click(screen.getByRole('button', { name: 'Sign out' }))

    expect(
      await screen.findByText('Workspace cleared. Sign-out could not be confirmed. Try again.'),
    ).toBeVisible()
    expect(screen.getByRole('button', { name: 'Try sign-out again' })).toBeVisible()

    await user.click(screen.getByRole('button', { name: 'Try sign-out again' }))
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Sign in' })).toBeVisible(),
    )

    const signOutCalls = calls.filter((call) => call.url === '/api/accounts/sign-out')
    expect(signOutCalls).toHaveLength(2)
    for (const call of calls) {
      expect(
        [
          '/api/accounts/session',
          '/api/accounts/antiforgery',
          '/api/accounts/sign-out',
        ].includes(call.url),
      ).toBe(true)
    }
    expect(spy).toHaveBeenCalled()
    expectNoLanguageRequests(calls)
  })

  it('suppresses authenticated redirect while sign-out confirmation is pending', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    stubSignedInSession(() => gate)
    renderAppWithProbe('/translate')

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeVisible()
    await user.click(screen.getByRole('button', { name: 'Sign out' }))
    expect(await screen.findByText('Signing out…')).toBeVisible()

    goTo('/translate')
    await new Promise((resolve) => setTimeout(resolve, 0))
    expect(screen.getByText('Signing out…')).toBeVisible()
    expect(
      screen.queryByText('The translation workspace arrives in a later update.'),
    ).not.toBeInTheDocument()

    release(emptyResponse(204))
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Sign in' })).toBeVisible(),
    )
  })
})

describe('local validation and ordering (AC-005)', () => {
  it.each([
    {
      name: 'missing email',
      email: '',
      password: VALID_PASSWORD,
      message: 'Enter your email address.',
      focused: 'Email',
    },
    {
      name: 'invalid email shape',
      email: 'not-an-email',
      password: VALID_PASSWORD,
      message: 'Enter a valid email address.',
      focused: 'Email',
    },
    {
      name: 'missing password',
      email: VALID_EMAIL,
      password: '',
      message: 'Enter your password.',
      focused: 'Password',
    },
  ])(
    'sends no request for $name with linked errors and first-invalid focus',
    async ({ email, password, message, focused }) => {
      const user = userEvent.setup()
      const { spy } = stubFetch(async () => verifiedSignInResponse())
      renderApp('/login')

      if (email.length > 0) await user.type(screen.getByLabelText('Email'), email)
      if (password.length > 0) await user.type(screen.getByLabelText('Password'), password)
      await user.click(screen.getByRole('button', { name: 'Sign in' }))

      expect(await screen.findByText('Check the highlighted fields.')).toBeVisible()
      const linked = screen.getAllByText(message)
      expect(linked.length).toBeGreaterThanOrEqual(2)
      expect(linked.some((node) => node.tagName.toLowerCase() === 'a')).toBe(true)
      expect(spy).not.toHaveBeenCalled()
      const invalidField = screen.getByLabelText(focused)
      expect(invalidField).toHaveAttribute('aria-invalid', 'true')
      expect(invalidField).toHaveFocus()
      const describedBy = invalidField.getAttribute('aria-describedby') ?? ''
      for (const id of describedBy.split(' ').filter(Boolean)) {
        expect(document.getElementById(id)).not.toBeNull()
      }
    },
  )

  it('preserves caret and selection when revealing the password', async () => {
    const user = userEvent.setup()
    stubFetch(async () => verifiedSignInResponse())
    renderApp('/login')

    const passwordField = screen.getByLabelText('Password') as HTMLInputElement
    await user.type(passwordField, VALID_PASSWORD)
    passwordField.focus()
    passwordField.setSelectionRange(2, 7)

    await user.click(screen.getByRole('button', { name: 'Show password' }))

    const revealed = screen.getByLabelText('Password') as HTMLInputElement
    expect(revealed.type).toBe('text')
    expect(revealed).toHaveFocus()
    expect(revealed.selectionStart).toBe(2)
    expect(revealed.selectionEnd).toBe(7)
    expect(screen.getByRole('button', { name: 'Hide password' })).toHaveAttribute(
      'aria-pressed',
      'true',
    )
  })

  it('redirects an already-signed-in verified visit to /login back to the protected route', async () => {
    const user = userEvent.setup()
    stubFetch(async (url) => {
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') return verifiedSignInResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    renderAppWithProbe('/login')

    await submitLoginForm(user)
    expect(await screen.findByRole('heading', { level: 1, name: 'Translation' })).toBeVisible()

    goTo('/login')
    await waitFor(() =>
      expect(screen.getByRole('heading', { level: 1, name: 'Translation' })).toBeVisible(),
    )
  })
})

describe('account transport failure (AC-006)', () => {
  it('shows the generic retry message and sends one account request per explicit retry', async () => {
    const user = userEvent.setup()
    let attempt = 0
    const { spy, calls } = stubFetch(async (url) => {
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') {
        attempt += 1
        if (attempt === 1) throw new TypeError('network down')
        return verifiedSignInResponse()
      }
      throw new Error(`unexpected request to ${url}`)
    })
    renderApp('/login')

    await submitLoginForm(user)

    expect(await screen.findByText('We couldn’t complete this request. Try again.')).toBeVisible()
    expect(
      screen.queryByRole('heading', { level: 1, name: 'Translation' }),
    ).not.toBeInTheDocument()
    expect(screen.getByLabelText('Password')).toHaveValue('')
    expect(screen.getByLabelText('Email')).toHaveValue(VALID_EMAIL)

    await user.type(screen.getByLabelText('Password'), VALID_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Translation' })).toBeVisible()
    expect(calls.filter((call) => call.url === '/api/accounts/sign-in')).toHaveLength(2)
    expect(spy).toHaveBeenCalledTimes(4)
    expectNoLanguageRequests(calls)
  })
})

describe('privacy boundary (AC-007)', () => {
  it('keeps passwords, tokens and request material out of storage, history and the page', async () => {
    const user = userEvent.setup()
    stubFetch(async (url) => {
      if (url === '/api/accounts/antiforgery') return antiforgeryResponse()
      if (url === '/api/accounts/sign-in') return invalidCredentialsResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    renderApp('/login')

    await submitLoginForm(user)
    expect(await screen.findByText('Email or password is incorrect.')).toBeVisible()

    expect(localStorage.length).toBe(0)
    expect(sessionStorage.length).toBe(0)
    expect(window.location.href).not.toContain(VALID_PASSWORD)
    expect(window.location.href).not.toContain(REQUEST_TOKEN)
    expect(window.location.search).toBe('')
    expect(document.body.textContent).not.toContain(VALID_PASSWORD)
    expect(document.body.textContent).not.toContain(REQUEST_TOKEN)
  })

  it('clears the password field on pageshow without submitting', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => verifiedSignInResponse())
    renderApp('/login')

    await user.type(screen.getByLabelText('Password'), VALID_PASSWORD)
    expect(screen.getByLabelText('Password')).toHaveValue(VALID_PASSWORD)

    act(() => {
      window.dispatchEvent(new Event('pageshow'))
    })

    expect(screen.getByLabelText('Password')).toHaveValue('')
    expect(spy).not.toHaveBeenCalled()
  })
})

describe('safe return guard', () => {
  it.each([
    { path: '/translate', expected: '/translate' },
    { path: '/rewrite', expected: '/rewrite' },
    { path: '/verify-email', expected: '/translate' },
    { path: '/login', expected: '/translate' },
    { path: 'https://evil.example/', expected: '/translate' },
    { path: '//evil.example/translate', expected: '/translate' },
    { path: '', expected: '/translate' },
    { path: null, expected: '/translate' },
    { path: undefined, expected: '/translate' },
  ])('resolves $path to $expected', ({ path, expected }) => {
    expect(resolveSafeReturn(path)).toBe(expected)
  })
})
