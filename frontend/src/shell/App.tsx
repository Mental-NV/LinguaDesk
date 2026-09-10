import { useEffect, useRef, useState, type ReactNode } from 'react'
import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { RegisterPage } from '../auth/RegisterPage'
import { VerifyEmailEntryPage } from '../auth/VerifyEmailEntryPage'

type PageProps = {
  children: ReactNode
  heading: string
}

function Page({ children, heading }: PageProps) {
  const location = useLocation()
  const headingRef = useRef<HTMLHeadingElement>(null)

  useEffect(() => {
    headingRef.current?.focus()
  }, [location.key, location.pathname])

  return (
    <main id="main-content" className="page-content" tabIndex={-1}>
      <div className="page-card">
        <h1 ref={headingRef} tabIndex={-1}>{heading}</h1>
        {children}
      </div>
    </main>
  )
}

function LoginPage() {
  return (
    <Page heading="Sign in">
      <p>Sign-in is not available in this build.</p>
      <Link className="primary-link" to="/register">Create account</Link>
    </Page>
  )
}

function VerifyEmailPage({ email }: { email: string | null }) {
  return (
    <Page heading="Verify your email">
      <VerifyEmailEntryPage email={email} />
    </Page>
  )
}

function NotFoundPage() {
  return (
    <Page heading="Page not found">
      <p>The page you requested does not exist.</p>
      <Link className="primary-link" to="/login">Go to sign in</Link>
    </Page>
  )
}

export function App() {
  const [pendingVerificationEmail, setPendingVerificationEmail] = useState<string | null>(null)

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">Skip to main content</a>
      <header className="site-header">
        <div className="header-inner">
          <Link className="wordmark" to="/" aria-label="LinguaDesk home">LinguaDesk</Link>
          <nav aria-label="Main navigation">
            <Link to="/translate">Translation</Link>
            <Link to="/rewrite">Rewriting</Link>
          </nav>
        </div>
      </header>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/register"
          element={
            <Page heading="Create account">
              <RegisterPage onRegistered={setPendingVerificationEmail} />
            </Page>
          }
        />
        <Route path="/verify-email" element={<VerifyEmailPage email={pendingVerificationEmail} />} />
        <Route path="/translate" element={<Navigate to="/login" replace />} />
        <Route path="/rewrite" element={<Navigate to="/login" replace />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </div>
  )
}
