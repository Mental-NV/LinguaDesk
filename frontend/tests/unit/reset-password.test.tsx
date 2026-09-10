import { act, render, screen, waitFor } from '@testing-library/react'
import { useEffect } from 'react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, useLocation, useNavigate } from 'react-router-dom'
import { App } from '../../src/shell/App'
import { resetLocalAccountPassword } from '../../src/api/accounts'

const SYNTHETIC_USER_ID = 'm014-synthetic-user-id'
const SYNTHETIC_CODE = 'bTAxNC1zeW50aGV0aWMtY29kZQ'
const VALID_PASSWORD = 'Maple!River2026'
const ROTATED_PASSWORD = 'Cedar!Lake2026 Valley'
const SHORT_PASSWORD = 'TooShort1!'
const MISMATCH_PASSWORD = 'Birch!Forest2026 Grove'

const SUCCESS_MESSAGE = 'Password updated. Sign in with your new password.'
const INVALID_LINK_MESSAGE = 'This reset link is invalid or has expired.'
const SUMMARY_MESSAGE = 'Check the highlighted fields.'
const PASSWORD_REQUIRED_MESSAGE = 'Enter your password.'
const PASSWORD_POLICY_MESSAGE = 'Password must meet all requirements.'
const PASSWORD_MISMATCH_MESSAGE = 'Passwords do not match.'
const RETRY_MESSAGE = 'We couldn\u2019t complete this request. Try again.'

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function resetResponse(): Response {
  return jsonResponse(200, { status: 'passwordReset' })
}

function invalidResponse(): Response {
  return jsonResponse(400, {
    type: 'about:blank',
    title: 'Invalid or expired password reset',
    status: 400,
    detail: 'The password-reset link is invalid or expired.',
    category: 'invalidOrExpiredPasswordReset',
    correlationId: 'test-correlation',
  })
}

function fieldResponse(): Response {
  return jsonResponse(400, {
    type: 'about:blank',
    title: 'Invalid request',
    status: 400,
    detail: 'One or more account fields are invalid.',
    category: 'invalidRequest',
    correlationId: 'test-correlation',
    errors: { newPassword: ['Password must contain 15 to 128 well-formed Unicode characters.'] },
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

function deliveredPath(): string {
  return `/reset-password?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}`
}

function expectNoLanguageRequests(calls: readonly RecordedCall[]): void {
  for (const call of calls) {
    expect(call.url).not.toContain('/api/translate')
    expect(call.url).not.toContain('/api/rewrite')
    expect(call.url).not.toContain('/api/usage')
  }
}

function storedValues(): string {
  const values: string[] = []
  for (let index = 0; index < localStorage.length; index += 1) {
    const key = localStorage.key(index)
    if (key !== null) values.push(localStorage.getItem(key) ?? '')
  }
  for (let index = 0; index < sessionStorage.length; index += 1) {
    const key = sessionStorage.key(index)
    if (key !== null) values.push(sessionStorage.getItem(key) ?? '')
  }
  return values.join('\n')
}

async function fillResetForm(user: ReturnType<typeof userEvent.setup>, password = VALID_PASSWORD) {
  await user.type(screen.getByLabelText('New password'), password)
  await user.type(screen.getByLabelText('Confirm password'), password)
}

describe('reset-password wrapper mapping (T001)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('maps 200 to reset with the exact triple body', async () => {
    const { calls } = stubFetch(async () => resetResponse())
    const result = await resetLocalAccountPassword({
      userId: SYNTHETIC_USER_ID,
      code: SYNTHETIC_CODE,
      newPassword: ` ${VALID_PASSWORD} `,
    })
    expect(result).toEqual({ kind: 'reset' })
    expect(calls).toHaveLength(1)
    expect(calls[0].url).toBe('/api/accounts/reset-password')
    expect(calls[0].init?.method).toBe('POST')
    expect(Object.keys(callBody(calls[0]) as Record<string, unknown>).sort()).toEqual([
      'code',
      'newPassword',
      'userId',
    ])
    expect(callBody(calls[0])).toEqual({
      userId: SYNTHETIC_USER_ID,
      code: SYNTHETIC_CODE,
      newPassword: ` ${VALID_PASSWORD} `,
    })
  })

  it('maps invalidOrExpired and malformed 400s to invalid and policy 400s to field', async () => {
    stubFetch(async () => invalidResponse())
    expect(
      await resetLocalAccountPassword({
        userId: SYNTHETIC_USER_ID,
        code: SYNTHETIC_CODE,
        newPassword: VALID_PASSWORD,
      }),
    ).toEqual({ kind: 'invalid' })
    vi.unstubAllGlobals()

    stubFetch(async () =>
      jsonResponse(400, {
        type: 'about:blank',
        title: 'Invalid request',
        status: 400,
        detail: 'Only the required fields are accepted.',
        category: 'invalidRequest',
        correlationId: 'test-correlation',
      }),
    )
    expect(
      await resetLocalAccountPassword({
        userId: SYNTHETIC_USER_ID,
        code: SYNTHETIC_CODE,
        newPassword: VALID_PASSWORD,
      }),
    ).toEqual({ kind: 'invalid' })
    vi.unstubAllGlobals()

    stubFetch(async () => fieldResponse())
    expect(
      await resetLocalAccountPassword({
        userId: SYNTHETIC_USER_ID,
        code: SYNTHETIC_CODE,
        newPassword: SHORT_PASSWORD,
      }),
    ).toEqual({ kind: 'field' })
  })

  it('maps 503, unexpected statuses and transport failure to retry', async () => {
    for (const status of [500, 503]) {
      stubFetch(async () => jsonResponse(status, {}))
      expect(
        await resetLocalAccountPassword({
          userId: SYNTHETIC_USER_ID,
          code: SYNTHETIC_CODE,
          newPassword: VALID_PASSWORD,
        }),
      ).toEqual({ kind: 'retry', status })
      vi.unstubAllGlobals()
    }
    stubFetch(async () => {
      throw new TypeError('network down')
    })
    expect(
      await resetLocalAccountPassword({
        userId: SYNTHETIC_USER_ID,
        code: SYNTHETIC_CODE,
        newPassword: VALID_PASSWORD,
      }),
    ).toEqual({ kind: 'retry', status: null })
  })
})

describe('reset-password success (AC-004)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('submits exactly one reset request and shows success with cleared secrets and no session', async () => {
    const user = userEvent.setup()
    const { spy, calls } = stubFetch(async (url) => {
      if (url === '/api/accounts/reset-password') return resetResponse()
      throw new Error(`unexpected request to ${url}`)
    })
    const seen = renderApp(deliveredPath())

    expect(await screen.findByRole('heading', { level: 1, name: 'Reset password' })).toHaveFocus()
    await fillResetForm(user, ROTATED_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    expect(await screen.findByText(SUCCESS_MESSAGE)).toBeVisible()
    expect(screen.getByRole('heading', { level: 1, name: 'Reset password' })).toHaveFocus()
    expect(screen.getByRole('link', { name: 'Go to sign in' })).toHaveAttribute('href', '/login')
    expect(screen.queryByRole('form')).not.toBeInTheDocument()
    expect(document.body.textContent).not.toContain(ROTATED_PASSWORD)

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(calls).toHaveLength(1)
    expect(callBody(calls[0])).toEqual({
      userId: SYNTHETIC_USER_ID,
      code: SYNTHETIC_CODE,
      newPassword: ROTATED_PASSWORD,
    })
    expect(seen.pathname).toBe('/reset-password')
    expect(seen.search).toBe('')
    expect(storedValues()).not.toContain(SYNTHETIC_USER_ID)
    expect(storedValues()).not.toContain(SYNTHETIC_CODE)
    expectNoLanguageRequests(calls)
  })

  it('disables fields during flight and ignores duplicate click and Enter', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    const { spy, calls } = stubFetch(async () => gate)
    renderApp(deliveredPath())

    await fillResetForm(user)
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(screen.getByLabelText('New password')).toBeDisabled()
    expect(screen.getByLabelText('Confirm password')).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Resetting…' }))
    await user.type(screen.getByLabelText('Confirm password'), '{Enter}')
    expect(spy).toHaveBeenCalledTimes(1)

    release(resetResponse())
    expect(await screen.findByText(SUCCESS_MESSAGE)).toBeVisible()
    expect(calls.filter((call) => call.url === '/api/accounts/reset-password')).toHaveLength(1)
  })

  it('strips link material from the URL after reading', async () => {
    stubFetch(async () => resetResponse())
    const seen = renderApp(deliveredPath())

    await screen.findByRole('form', { name: 'Reset password' })
    await waitFor(() => expect(seen.search).toBe(''))
    expect(seen.pathname).toBe('/reset-password')
  })

  it('clears password fields on pageshow without submitting', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => resetResponse())
    renderApp(deliveredPath())

    await fillResetForm(user)
    expect(screen.getByLabelText('New password')).toHaveValue(VALID_PASSWORD)

    await act(async () => {
      window.dispatchEvent(new Event('pageshow'))
    })

    expect(screen.getByLabelText('New password')).toHaveValue('')
    expect(screen.getByLabelText('Confirm password')).toHaveValue('')
    expect(spy).not.toHaveBeenCalled()
  })
})

describe('reset-password invalid links (AC-005)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each([
    { name: 'missing link', path: '/reset-password' },
    { name: 'single key', path: `/reset-password?userId=${SYNTHETIC_USER_ID}` },
    { name: 'empty code', path: `/reset-password?userId=${SYNTHETIC_USER_ID}&code=` },
    {
      name: 'extra key',
      path: `/reset-password?userId=${SYNTHETIC_USER_ID}&code=${SYNTHETIC_CODE}&extra=1`,
    },
  ])('shows the recovery action with no fields for a $name', async ({ path }) => {
    const { spy } = stubFetch(async () => {
      throw new Error('no request is expected without link material')
    })
    const seen = renderApp(path)

    expect(await screen.findByText(INVALID_LINK_MESSAGE)).toBeVisible()
    expect(screen.getByRole('link', { name: 'Request a new reset link' })).toHaveAttribute(
      'href',
      '/forgot-password',
    )
    expect(screen.queryByLabelText('New password')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reset password' })).not.toBeInTheDocument()
    expect(spy).not.toHaveBeenCalled()
    await waitFor(() => expect(seen.search).toBe(''))
  })

  it('resolves a uniform server rejection to the same alert without disclosing material', async () => {
    const user = userEvent.setup()
    const { spy, calls } = stubFetch(async () => invalidResponse())
    renderApp(deliveredPath())

    await fillResetForm(user)
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    expect(await screen.findByText(INVALID_LINK_MESSAGE)).toBeVisible()
    expect(screen.getByRole('link', { name: 'Request a new reset link' })).toBeVisible()
    expect(screen.queryByLabelText('New password')).not.toBeInTheDocument()
    expect(document.body.textContent).not.toContain(SYNTHETIC_USER_ID)
    expect(document.body.textContent).not.toContain(SYNTHETIC_CODE)
    expect(document.body.textContent).not.toContain(VALID_PASSWORD)
    expect(document.body.textContent).not.toContain('test-correlation')
    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(calls).toHaveLength(1)
    expectNoLanguageRequests(calls)
  })
})

describe('reset-password validation (AC-006)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('requires a new password with linked errors and first-error focus', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => resetResponse())
    renderApp(deliveredPath())

    await user.click(await screen.findByRole('button', { name: 'Reset password' }))

    expect(await screen.findByText(SUMMARY_MESSAGE)).toBeVisible()
    expect(screen.getByText(PASSWORD_REQUIRED_MESSAGE, { selector: 'p' })).toBeVisible()
    expect(screen.getByLabelText('New password')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText('New password')).toHaveFocus()
    expect(spy).not.toHaveBeenCalled()
  })

  it('rejects a policy-violating password without a request', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => resetResponse())
    renderApp(deliveredPath())

    await user.type(screen.getByLabelText('New password'), SHORT_PASSWORD)
    await user.type(screen.getByLabelText('Confirm password'), SHORT_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    expect(await screen.findAllByText(PASSWORD_POLICY_MESSAGE)).not.toHaveLength(0)
    expect(screen.getByText(PASSWORD_POLICY_MESSAGE, { selector: 'p' })).toBeVisible()
    expect(screen.getByLabelText('New password')).toHaveFocus()
    expect(spy).not.toHaveBeenCalled()
  })

  it('rejects a mismatched confirmation with confirmation focus', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => resetResponse())
    renderApp(deliveredPath())

    await user.type(screen.getByLabelText('New password'), VALID_PASSWORD)
    await user.type(screen.getByLabelText('Confirm password'), MISMATCH_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    expect(await screen.findAllByText(PASSWORD_MISMATCH_MESSAGE)).not.toHaveLength(0)
    expect(screen.getByText(PASSWORD_MISMATCH_MESSAGE, { selector: 'p' })).toBeVisible()
    expect(screen.getByLabelText('Confirm password')).toHaveFocus()
    expect(spy).not.toHaveBeenCalled()
  })

  it('surfaces a server policy rejection as the identical policy error', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => fieldResponse())
    renderApp(deliveredPath())

    await fillResetForm(user)
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    expect(await screen.findAllByText(PASSWORD_POLICY_MESSAGE)).not.toHaveLength(0)
    expect(screen.getByText(PASSWORD_POLICY_MESSAGE, { selector: 'p' })).toBeVisible()
    expect(screen.getByLabelText('New password')).toHaveValue('')
    expect(screen.getByLabelText('New password')).toHaveFocus()
    expect(document.body.textContent).not.toContain(VALID_PASSWORD)
    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
  })

  it('preserves caret and selection when toggling password visibility', async () => {
    const user = userEvent.setup()
    stubFetch(async () => resetResponse())
    renderApp(deliveredPath())

    const secret = await screen.findByLabelText('New password')
    await user.type(secret, VALID_PASSWORD)
    const input = secret as HTMLInputElement
    input.focus()
    input.setSelectionRange(2, 6)

    await user.click(screen.getByRole('button', { name: 'Show password' }))

    const revealed = screen.getByLabelText('New password') as HTMLInputElement
    expect(revealed).toHaveFocus()
    expect(revealed.selectionStart).toBe(2)
    expect(revealed.selectionEnd).toBe(6)
  })

  it('shows the generic error on transport failure with an auth-only retry', async () => {
    const user = userEvent.setup()
    let attempt = 0
    const { spy, calls } = stubFetch(async (url) => {
      if (url !== '/api/accounts/reset-password') {
        throw new Error(`unexpected request to ${url}`)
      }
      attempt += 1
      if (attempt === 1) throw new TypeError('network down')
      return resetResponse()
    })
    renderApp(deliveredPath())

    await fillResetForm(user, ROTATED_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    expect(await screen.findByText(RETRY_MESSAGE)).toBeVisible()
    expect(screen.getByLabelText('New password')).toHaveValue('')

    await user.type(screen.getByLabelText('New password'), ROTATED_PASSWORD)
    await user.type(screen.getByLabelText('Confirm password'), ROTATED_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    expect(await screen.findByText(SUCCESS_MESSAGE)).toBeVisible()
    await waitFor(() => expect(spy).toHaveBeenCalledTimes(2))
    expect(calls.every((call) => call.url === '/api/accounts/reset-password')).toBe(true)
    expectNoLanguageRequests(calls)
  })
})

describe('reset-password shell race (AC-007)', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  let probeNavigate: (path: string) => void = () => {}

  function NavigateProbe() {
    const navigate = useNavigate()
    useEffect(() => {
      probeNavigate = navigate
    }, [navigate])
    return null
  }

  it('discards a late login completion after leaving for the reset route', async () => {
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
        <NavigateProbe />
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText('Email'), 'known-visitor@example.test')
    await user.type(screen.getByLabelText('Password'), VALID_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Sign in' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Signing in…' })).toBeDisabled())

    act(() => {
      probeNavigate(deliveredPath())
    })
    expect(await screen.findByRole('heading', { level: 1, name: 'Reset password' })).toBeVisible()

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

    expect(screen.getByRole('heading', { level: 1, name: 'Reset password' })).toBeVisible()
    expect(screen.queryByRole('heading', { level: 1, name: 'Translation' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()
  })
})
