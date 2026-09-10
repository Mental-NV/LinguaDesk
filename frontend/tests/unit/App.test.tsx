import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { App } from '../../src/shell/App'

function renderRoute(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

describe('signed-out shell routes', () => {
  it.each([
    ['/', 'Sign in', 'Sign-in is not available in this build.'],
    ['/login', 'Sign in', 'Sign-in is not available in this build.'],
    ['/translate', 'Sign in', 'Sign-in is not available in this build.'],
    ['/rewrite', 'Sign in', 'Sign-in is not available in this build.'],
    ['/unknown-page', 'Page not found', 'The page you requested does not exist.'],
  ])('renders %s without exposing unavailable forms', async (path, heading, message) => {
    renderRoute(path)

    const destinationHeading = await screen.findByRole('heading', { level: 1, name: heading })
    expect(destinationHeading).toHaveFocus()
    expect(screen.getByText(message)).toBeVisible()
    expect(screen.getAllByRole('main')).toHaveLength(1)
    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1)
    expect(screen.queryByRole('form')).not.toBeInTheDocument()
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
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

    const accountLink = await screen.findByRole('link', { name: 'Create account' })
    expect(accountLink).toHaveAttribute('href', '/register')
    expect(screen.getByRole('link', { name: 'Translation' })).toHaveAttribute('href', '/translate')
    expect(screen.getByRole('link', { name: 'Rewriting' })).toHaveAttribute('href', '/rewrite')

    await user.click(accountLink)
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Create account' })).toHaveFocus())
  })

  it('provides a skip link to the single main landmark', async () => {
    renderRoute('/login')

    const skipLink = screen.getByRole('link', { name: 'Skip to main content' })
    expect(skipLink).toHaveAttribute('href', '#main-content')
    expect(await screen.findByRole('main')).toHaveAttribute('id', 'main-content')
  })
})
