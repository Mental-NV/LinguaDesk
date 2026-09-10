import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { App } from '../../src/shell/App'
import { RegisterPage } from '../../src/auth/RegisterPage'

const VALID_EMAIL = 'new-visitor@example.test'
const VALID_PASSWORD = 'Maple!River2026'

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function acceptanceResponse(): Response {
  return jsonResponse(202, { status: 'verificationRequired' })
}

function fieldRejectionResponse(): Response {
  return jsonResponse(400, {
    type: 'about:blank',
    title: 'Invalid request',
    status: 400,
    detail: 'One or more account fields are invalid.',
    category: 'invalidRequest',
    correlationId: 'test-correlation',
    errors: { password: ['Password must contain 15 to 128 well-formed Unicode characters.'] },
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

function renderApp(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

async function fillValidForm(user: ReturnType<typeof userEvent.setup>, email = VALID_EMAIL) {
  await user.type(screen.getByLabelText('Email'), email)
  await user.type(screen.getByLabelText('Password'), VALID_PASSWORD)
  await user.type(screen.getByLabelText('Confirm password'), VALID_PASSWORD)
}

async function submitValidForm(user: ReturnType<typeof userEvent.setup>, email = VALID_EMAIL) {
  await fillValidForm(user, email)
  await user.click(screen.getByRole('button', { name: 'Create account' }))
}

describe('registration valid submission', () => {
  it('sends exactly one register request with only email and password, then shows verification entry', async () => {
    const user = userEvent.setup()
    const { spy, calls } = stubFetch(async () => acceptanceResponse())
    renderApp('/register')

    await submitValidForm(user)

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    const [url, init] = [calls[0].url, calls[0].init]
    expect(url).toBe('/api/accounts/register')
    expect(init?.method).toBe('POST')
    expect(Object.keys(JSON.parse(String(init?.body)))).toEqual(['email', 'password'])
    expect(JSON.parse(String(init?.body))).toEqual({ email: VALID_EMAIL, password: VALID_PASSWORD })

    expect(await screen.findByText('Check your email to verify your account.')).toBeVisible()
    expect(screen.getByText(VALID_EMAIL)).toBeVisible()
    expect(await screen.findByRole('heading', { level: 1, name: 'Verify your email' })).toHaveFocus()
    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Confirm password')).not.toBeInTheDocument()
    expect(spy).toHaveBeenCalledTimes(1)
  })

  it('disables fields during flight and ignores duplicate click and Enter', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    const { spy } = stubFetch(() => gate)
    renderApp('/register')

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(screen.getByLabelText('Email')).toBeDisabled()
    expect(screen.getByLabelText('Password')).toBeDisabled()
    expect(screen.getByLabelText('Confirm password')).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Creating account…' }))
    await user.type(screen.getByLabelText('Confirm password'), '{Enter}')
    expect(spy).toHaveBeenCalledTimes(1)

    release(acceptanceResponse())
    expect(await screen.findByText('Check your email to verify your account.')).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(1)
  })
})

describe('registration local validation', () => {
  it.each([
    {
      name: 'missing email',
      email: '',
      password: VALID_PASSWORD,
      confirm: VALID_PASSWORD,
      message: 'Enter your email address.',
      focused: 'Email',
    },
    {
      name: 'invalid email shape',
      email: 'not-an-email',
      password: VALID_PASSWORD,
      confirm: VALID_PASSWORD,
      message: 'Enter a valid email address.',
      focused: 'Email',
    },
    {
      name: 'missing password',
      email: VALID_EMAIL,
      password: '',
      confirm: '',
      message: 'Enter your password.',
      focused: 'Password',
    },
    {
      name: 'short password',
      email: VALID_EMAIL,
      password: 'short',
      confirm: 'short',
      message: 'Password must meet all requirements.',
      focused: 'Password',
    },
    {
      name: '14-scalar password below the boundary',
      email: VALID_EMAIL,
      password: 'Maple!River202',
      confirm: 'Maple!River202',
      message: 'Password must meet all requirements.',
      focused: 'Password',
    },
    {
      name: '129-scalar password above the boundary',
      email: VALID_EMAIL,
      password: 'a'.repeat(129),
      confirm: 'a'.repeat(129),
      message: 'Password must meet all requirements.',
      focused: 'Password',
    },
    {
      name: 'mismatched confirmation',
      email: VALID_EMAIL,
      password: VALID_PASSWORD,
      confirm: 'Maple!River2027',
      message: 'Passwords do not match.',
      focused: 'Confirm password',
    },
  ])('sends no request for $name with linked errors and first-invalid focus', async ({
    email,
    password,
    confirm,
    message,
    focused,
  }) => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => acceptanceResponse())
    renderApp('/register')

    if (email.length > 0) await user.type(screen.getByLabelText('Email'), email)
    if (password.length > 0) await user.type(screen.getByLabelText('Password'), password)
    if (confirm.length > 0) await user.type(screen.getByLabelText('Confirm password'), confirm)
    await user.click(screen.getByRole('button', { name: 'Create account' }))

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
  })

  it('submits the platform-sanitized email when surrounding whitespace is typed', async () => {
    const user = userEvent.setup()
    const { spy, calls } = stubFetch(async () => acceptanceResponse())
    renderApp('/register')

    await user.type(screen.getByLabelText('Email'), ` ${VALID_EMAIL} `)
    await user.type(screen.getByLabelText('Password'), VALID_PASSWORD)
    await user.type(screen.getByLabelText('Confirm password'), VALID_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(JSON.parse(String(calls[0].init?.body))).toEqual({ email: VALID_EMAIL, password: VALID_PASSWORD })
    expect(await screen.findByText('Check your email to verify your account.')).toBeVisible()
  })

  it('rejects malformed Unicode passwords without a request', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => acceptanceResponse())
    const { fireEvent } = await import('@testing-library/react')
    renderApp('/register')

    await user.type(screen.getByLabelText('Email'), VALID_EMAIL)
    const malformed = VALID_PASSWORD + '\uD800'
    expect(malformed.charCodeAt(malformed.length - 1)).toBe(0xd800)
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: malformed } })
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: malformed } })
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    expect((await screen.findAllByText('Password must meet all requirements.')).length).toBeGreaterThanOrEqual(2)
    expect(spy).not.toHaveBeenCalled()
  })

  it('keeps the 15/128 scalar checklist truthful for boundary passwords', async () => {
    const user = userEvent.setup()
    stubFetch(async () => acceptanceResponse())
    renderApp('/register')

    const checklist = screen.getByRole('list', { name: 'Password requirements' })
    expect(checklist).toHaveTextContent('(not met)')

    await user.type(screen.getByLabelText('Password'), 'Maple!River202')
    expect(checklist).toHaveTextContent('(not met)')

    await user.type(screen.getByLabelText('Password'), '6')
    expect(checklist).toHaveTextContent('(met)')

    await user.clear(screen.getByLabelText('Password'))
    await user.type(screen.getByLabelText('Password'), 'a'.repeat(128))
    expect(checklist).toHaveTextContent('(met)')

    await user.type(screen.getByLabelText('Password'), 'a')
    expect(checklist).toHaveTextContent('(not met)')
  })
})

describe('registration server rejection', () => {
  it('clears passwords, keeps email, focuses the first error and permits one explicit retry', async () => {
    const user = userEvent.setup()
    const { spy, calls } = stubFetch(async () => fieldRejectionResponse())
    renderApp('/register')

    await submitValidForm(user)

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(
      await screen.findByText(
        'We couldn\u2019t create an account with these details. Try signing in or use a different email.',
      ),
    ).toBeVisible()
    expect(screen.getByLabelText('Password')).toHaveValue('')
    expect(screen.getByLabelText('Confirm password')).toHaveValue('')
    expect(screen.getByLabelText('Email')).toHaveValue(VALID_EMAIL)
    expect(screen.getByLabelText('Password')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText('Password')).toHaveFocus()
    expect(document.body.textContent).not.toContain(VALID_PASSWORD)

    const { spy: retrySpy } = stubFetch(async () => acceptanceResponse())
    void spy
    await user.type(screen.getByLabelText('Password'), VALID_PASSWORD)
    await user.type(screen.getByLabelText('Confirm password'), VALID_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Create account' }))

    await waitFor(() => expect(retrySpy).toHaveBeenCalledTimes(1))
    expect(JSON.parse(String(retrySpy.mock.calls[0][1]?.body))).toEqual({
      email: VALID_EMAIL,
      password: VALID_PASSWORD,
    })
    expect(await screen.findByText('Check your email to verify your account.')).toBeVisible()
    expect(calls.every((call) => call.url === '/api/accounts/register')).toBe(true)
  })
})

describe('registration transport failure and ordering', () => {
  it('shows the generic retry message without false success and keeps only the email', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => {
      throw new TypeError('network down')
    })
    renderApp('/register')

    await submitValidForm(user)

    expect(await screen.findByText('We couldn\u2019t complete this request. Try again.')).toBeVisible()
    expect(screen.queryByText('Check your email to verify your account.')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Password')).toHaveValue('')
    expect(screen.getByLabelText('Confirm password')).toHaveValue('')
    expect(screen.getByLabelText('Email')).toHaveValue(VALID_EMAIL)
    expect(spy).toHaveBeenCalledTimes(1)
  })

  it('discards a stale completion that resolves after unmount without restoring passwords', async () => {
    const user = userEvent.setup()
    let release!: (value: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    stubFetch(() => gate)
    const onRegistered = vi.fn()
    const tree = render(
      <MemoryRouter initialEntries={['/register']}>
        <Routes>
          <Route path="/register" element={<RegisterPage onRegistered={onRegistered} />} />
          <Route path="/verify-email" element={<p>Verify your email</p>} />
        </Routes>
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText('Email'), VALID_EMAIL)
    await user.type(screen.getByLabelText('Password'), VALID_PASSWORD)
    await user.type(screen.getByLabelText('Confirm password'), VALID_PASSWORD)
    await user.click(screen.getByRole('button', { name: 'Create account' }))
    tree.unmount()

    release(acceptanceResponse())
    await gate
    await new Promise((resolve) => setTimeout(resolve, 0))
    expect(onRegistered).not.toHaveBeenCalled()
  })
})

describe('registration keyboard and reveal behavior', () => {
  it('submits once with Enter and keeps heading, skip link and native labels', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => acceptanceResponse())
    renderApp('/register')

    expect(await screen.findByRole('heading', { level: 1, name: 'Create account' })).toHaveFocus()
    expect(screen.getByRole('link', { name: 'Skip to main content' })).toHaveAttribute('href', '#main-content')

    await user.type(screen.getByLabelText('Email'), VALID_EMAIL)
    await user.type(screen.getByLabelText('Password'), VALID_PASSWORD)
    await user.type(screen.getByLabelText('Confirm password'), `${VALID_PASSWORD}[Enter]`)

    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(await screen.findByText('Check your email to verify your account.')).toBeVisible()
    expect(spy).toHaveBeenCalledTimes(1)
  })

  it('preserves caret and selection when revealing the password', async () => {
    const user = userEvent.setup()
    stubFetch(async () => acceptanceResponse())
    renderApp('/register')

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
    expect(screen.getByRole('button', { name: 'Hide password' })).toHaveAttribute('aria-pressed', 'true')
  })
})

describe('registration privacy boundary', () => {
  it('keeps passwords out of storage, history and the rendered page', async () => {
    const user = userEvent.setup()
    const { spy } = stubFetch(async () => fieldRejectionResponse())
    renderApp('/register')

    await submitValidForm(user)
    await waitFor(() => expect(spy).toHaveBeenCalledTimes(1))
    expect(await screen.findByText(/We couldn\u2019t create an account/)).toBeVisible()

    expect(localStorage.length).toBe(0)
    expect(sessionStorage.length).toBe(0)
    expect(window.location.href).not.toContain(VALID_PASSWORD)
    expect(window.location.href).not.toContain('password')
    expect(document.body.textContent).not.toContain(VALID_PASSWORD)
  })
})
