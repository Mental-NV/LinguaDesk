import { render, screen, waitFor } from '@testing-library/react'
import { useEffect } from 'react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, useLocation } from 'react-router-dom'
import { App } from '../../src/shell/App'

function renderRoute(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

describe('signed-out shell routes', () => {
  it('renders /login with the sign-in form and heading focus', async () => {
    renderRoute('/login')

    const destinationHeading = await screen.findByRole('heading', { level: 1, name: 'Sign in' })
    expect(destinationHeading).toHaveFocus()
    expect(screen.getByRole('form', { name: 'Sign in' })).toBeVisible()
    expect(screen.getByLabelText('Email')).toBeVisible()
    expect(screen.getByLabelText('Password')).toBeVisible()
    expect(screen.getByRole('button', { name: 'Show password' })).toBeVisible()
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Forgot password' })).toHaveAttribute(
      'href',
      '/forgot-password',
    )
    expect(screen.getByRole('link', { name: 'Create account' })).toHaveAttribute('href', '/register')
    expect(screen.queryByText('Signing out…')).not.toBeInTheDocument()
    expect(screen.getAllByRole('main')).toHaveLength(1)
    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1)
  })

  it.each([['/'], ['/translate'], ['/rewrite']])(
    'redirects signed-out %s to the sign-in form without a session',
    async (path) => {
      renderRoute(path)

      const destinationHeading = await screen.findByRole('heading', { level: 1, name: 'Sign in' })
      expect(destinationHeading).toBeInTheDocument()
      expect(screen.getByRole('form', { name: 'Sign in' })).toBeVisible()
      expect(screen.queryByRole('textbox', { name: 'Source text' })).not.toBeInTheDocument()
    },
  )

  it('renders /forgot-password as a staged state without credential fields', async () => {
    renderRoute('/forgot-password')

    const destinationHeading = await screen.findByRole('heading', {
      level: 1,
      name: 'Forgot password',
    })
    expect(destinationHeading).toHaveFocus()
    expect(screen.getByText('Password recovery is not available in this build.')).toBeVisible()
    expect(screen.getByRole('link', { name: 'Back to sign in' })).toHaveAttribute('href', '/login')
    expect(screen.queryByRole('form')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument()
  })

  it('renders unknown paths with a sign-in return action', async () => {
    renderRoute('/unknown-page')

    const destinationHeading = await screen.findByRole('heading', { level: 1, name: 'Page not found' })
    expect(destinationHeading).toHaveFocus()
    expect(screen.getByText('The page you requested does not exist.')).toBeVisible()
    expect(screen.getByRole('link', { name: 'Go to sign in' })).toHaveAttribute('href', '/login')
  })

  it('renders /register with the registration form and no protected workspace', async () => {
    renderRoute('/register')

    const destinationHeading = await screen.findByRole('heading', { level: 1, name: 'Create account' })
    expect(destinationHeading).toHaveFocus()
    expect(screen.getByRole('form')).toBeVisible()
    expect(screen.getByLabelText('Email')).toBeVisible()
    expect(screen.getByLabelText('Password')).toBeVisible()
    expect(screen.getByLabelText('Confirm password')).toBeVisible()
    expect(screen.getByRole('button', { name: 'Create account' })).toBeVisible()
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
  })

  it('redirects /verify-email to /register without an in-memory registration', async () => {
    renderRoute('/verify-email')

    expect(await screen.findByRole('heading', { level: 1, name: 'Create account' })).toBeInTheDocument()
    expect(screen.getByRole('form')).toBeVisible()
  })

  it('uses native links for shell navigation', async () => {
    const user = userEvent.setup()
    renderRoute('/login')

    const recoveryLink = await screen.findByRole('link', { name: 'Forgot password' })
    expect(recoveryLink).toHaveAttribute('href', '/forgot-password')
    expect(screen.getByRole('link', { name: 'Translation' })).toHaveAttribute('href', '/translate')
    expect(screen.getByRole('link', { name: 'Rewriting' })).toHaveAttribute('href', '/rewrite')

    await user.click(recoveryLink)
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Forgot password' })).toHaveFocus())

    await user.click(screen.getByRole('link', { name: 'Back to sign in' }))
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Sign in' })).toHaveFocus())
  })

  it('provides a skip link to the single main landmark', async () => {
    renderRoute('/login')

    const skipLink = screen.getByRole('link', { name: 'Skip to main content' })
    expect(skipLink).toHaveAttribute('href', '#main-content')
    expect(await screen.findByRole('main')).toHaveAttribute('id', 'main-content')
  })
})

describe('unverified verification guard', () => {
  it('keeps guarded entries on /verify-email while an unverified registration is pending', async () => {
    const user = userEvent.setup()
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        new Response(JSON.stringify({ status: 'verificationRequired' }), {
          status: 202,
          headers: { 'Content-Type': 'application/json' },
        }),
      ),
    )
    try {
      const seen = { pathname: '' }
      function Probe() {
        const location = useLocation()
        const { pathname } = location
        useEffect(() => {
          seen.pathname = pathname
        }, [pathname])
        return null
      }
      render(
        <MemoryRouter initialEntries={['/register']}>
          <App />
          <Probe />
        </MemoryRouter>,
      )

      await user.type(screen.getByLabelText('Email'), 'guard-visitor@example.test')
      await user.type(screen.getByLabelText('Password'), 'Maple!River2026')
      await user.type(screen.getByLabelText('Confirm password'), 'Maple!River2026')
      await user.click(screen.getByRole('button', { name: 'Create account' }))
      expect(await screen.findByRole('heading', { level: 1, name: 'Verify your email' })).toBeInTheDocument()

      for (const linkName of ['Translation', 'Rewriting', 'LinguaDesk home'] as const) {
        await user.click(screen.getByRole('link', { name: linkName }))
        await waitFor(() => expect(seen.pathname).toBe('/verify-email'))
        expect(
          screen.getByRole('heading', { level: 1, name: 'Verify your email' }),
        ).toBeInTheDocument()
      }
    } finally {
      vi.unstubAllGlobals()
    }
  })
})
