import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import {
  getAccountAntiforgeryToken,
  getLocalAccountSession,
  signOutLocalAccount,
} from '../api/accounts'
import { ForgotPasswordPage } from '../auth/ForgotPasswordPage'
import { RewritePage } from '../rewrite/RewritePage'
import { TranslatePage } from '../translate/TranslatePage'
import { WorkspaceStoreProvider } from './workspaces'
import { useWorkspaceStores } from './workspaceStores'
import { LoginPage, type SignInCompletionStatus } from '../auth/LoginPage'
import { RegisterPage } from '../auth/RegisterPage'
import { ResetPasswordPage } from '../auth/ResetPasswordPage'
import { VerifyEmailEntryPage } from '../auth/VerifyEmailEntryPage'
import { isProtectedPath, resolveSafeReturn } from '../auth/safeReturn'

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

type SessionState = 'loading' | 'verified' | 'unverified' | 'signedOut'
type SignOutPhase = 'idle' | 'pending' | 'failed'

function RememberReturn({
  path,
  onRemember,
}: {
  readonly path: string
  readonly onRemember: (path: string) => void
}) {
  useEffect(() => {
    onRemember(path)
  }, [path, onRemember])
  return null
}

function VerifyEmailPage({
  email,
  allowUnverifiedEntry,
  continuationPath,
}: {
  email: string | null
  allowUnverifiedEntry: boolean
  continuationPath: string
}) {
  return (
    <Page heading="Verify your email">
      <VerifyEmailEntryPage
        email={email}
        allowUnverifiedEntry={allowUnverifiedEntry}
        protectedContinuationPath={continuationPath}
      />
    </Page>
  )
}

function ForgotPasswordEntry() {
  return (
    <Page heading="Forgot password">
      <ForgotPasswordPage />
    </Page>
  )
}

function ResetPasswordEntry() {
  return (
    <Page heading="Reset password">
      <ResetPasswordPage />
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
  return (
    <WorkspaceStoreProvider>
      <Shell />
    </WorkspaceStoreProvider>
  )
}

function Shell() {
  const location = useLocation()
  const stores = useWorkspaceStores()
  const [initialProtected] = useState(() => isProtectedPath(location.pathname))
  const [auth, setAuth] = useState<SessionState>(() =>
    initialProtected ? 'loading' : 'signedOut',
  )
  const [pendingVerificationEmail, setPendingVerificationEmail] = useState<string | null>(null)
  const [safeReturn, setSafeReturn] = useState('/translate')
  const [expiryNotice, setExpiryNotice] = useState(false)
  const [signOutPhase, setSignOutPhase] = useState<SignOutPhase>('idle')

  const mountedRef = useRef(true)
  const signOutFlightRef = useRef(0)
  const storesRef = useRef(stores)
  useEffect(() => {
    storesRef.current = stores
  }, [stores])

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
    }
  }, [])

  useEffect(() => {
    if (!initialProtected) return
    let cancelled = false
    void (async () => {
      let result: Awaited<ReturnType<typeof getLocalAccountSession>>
      try {
        result = await getLocalAccountSession()
      } catch {
        result = { kind: 'retry', status: null }
      }
      if (cancelled || !mountedRef.current) return
      if (result.kind === 'verified') {
        setAuth('verified')
      } else if (result.kind === 'unverified') {
        setAuth('unverified')
      } else if (result.kind === 'signedOut') {
        // Session expiry clears lifted text before the login form renders
        // (MSG-037); late text callbacks are fenced by the clearing revision.
        storesRef.current?.resetAll()
        setAuth('signedOut')
        setPendingVerificationEmail(null)
        setSafeReturn('/translate')
        setExpiryNotice(true)
      } else {
        setAuth('signedOut')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [initialProtected])

  const handleRememberReturn = useCallback((path: string) => {
    setSafeReturn(resolveSafeReturn(path))
  }, [])

  const handleSignedIn = useCallback((status: SignInCompletionStatus, email: string) => {
    setExpiryNotice(false)
    if (status === 'verified') {
      setPendingVerificationEmail(null)
      setAuth('verified')
    } else {
      setPendingVerificationEmail(email)
      setAuth('unverified')
    }
  }, [])

  const handleExpiryShown = useCallback(() => {
    setExpiryNotice(false)
  }, [])

  const handleSignOut = useCallback(() => {
    if (signOutFlightRef.current > 0) return
    const flight = signOutFlightRef.current + 1
    signOutFlightRef.current = flight
    // Sign-out keeps the M013 teardown guarantee through the single M032
    // reset-all path: lifted text clears immediately while pending feature
    // flights are aborted, so late responses cannot restore cleared text.
    // Plain navigation never resets; see the lifted workspace store.
    stores?.resetAll()
    setSafeReturn('/translate')
    setPendingVerificationEmail(null)
    setExpiryNotice(false)
    setAuth('signedOut')
    setSignOutPhase('pending')

    void (async () => {
      const bootstrap = await getAccountAntiforgeryToken()
      if (!mountedRef.current || signOutFlightRef.current !== flight) return
      if (bootstrap.kind !== 'ok') {
        signOutFlightRef.current = 0
        setSignOutPhase('failed')
        return
      }
      let result: Awaited<ReturnType<typeof signOutLocalAccount>>
      try {
        result = await signOutLocalAccount({ antiforgeryToken: bootstrap.requestToken })
      } catch {
        result = { kind: 'retry', status: null }
      }
      if (!mountedRef.current || signOutFlightRef.current !== flight) return
      signOutFlightRef.current = 0
      setSignOutPhase(result.kind === 'signedOut' ? 'idle' : 'failed')
    })()
  }, [stores])

  const rootEntry =
    signOutPhase !== 'idle' ? (
      <Navigate to="/login" replace />
    ) : auth === 'verified' ? (
      <Navigate to="/translate" replace />
    ) : auth === 'unverified' || pendingVerificationEmail !== null ? (
      <Navigate to="/verify-email" replace />
    ) : (
      <Navigate to="/login" replace />
    )

  const loginEntry =
    signOutPhase === 'pending' ? (
      <Page heading="Sign in">
        <div role="status">
          <p>Signing out…</p>
        </div>
      </Page>
    ) : signOutPhase === 'failed' ? (
      <Page heading="Sign in">
        <div className="server-alert" role="alert">
          <p>Workspace cleared. Sign-out could not be confirmed. Try again.</p>
          <p>
            <button type="button" className="secondary-button" onClick={handleSignOut}>
              Try sign-out again
            </button>
          </p>
        </div>
      </Page>
    ) : auth === 'verified' ? (
      <Navigate to={safeReturn} replace />
    ) : (
      <Page heading="Sign in">
        <LoginPage
          onSignedIn={handleSignedIn}
          sessionExpired={expiryNotice}
          onExpiryShown={handleExpiryShown}
        />
      </Page>
    )

  const registerEntry =
    signOutPhase !== 'idle' ? (
      <Navigate to="/login" replace />
    ) : auth === 'verified' ? (
      <Navigate to={safeReturn} replace />
    ) : (
      <Page heading="Create account">
        <RegisterPage onRegistered={setPendingVerificationEmail} />
      </Page>
    )

  const verifyEntry =
    signOutPhase !== 'idle' ? (
      <Navigate to="/login" replace />
    ) : auth === 'verified' ? (
      <Navigate to={safeReturn} replace />
    ) : (
      <VerifyEmailPage
        email={pendingVerificationEmail}
        allowUnverifiedEntry={auth === 'unverified'}
        continuationPath={safeReturn}
      />
    )

  const featureEntry = (feature: 'translate' | 'rewrite', pathname: string) => {
    if (signOutPhase !== 'idle') {
      return (
        <>
          <RememberReturn path={pathname} onRemember={handleRememberReturn} />
          <Navigate to="/login" replace />
        </>
      )
    }
    if (auth === 'loading') {
      return (
        <Page heading={feature === 'translate' ? 'Translation' : 'Rewriting'}>
          <div role="status">
            <p>Loading…</p>
          </div>
        </Page>
      )
    }
    if (auth === 'verified') {
      if (feature === 'translate') {
        return (
          <Page heading="Translation">
            <TranslatePage onSignOut={handleSignOut} />
          </Page>
        )
      }
      return (
        <Page heading="Rewriting">
          <RewritePage onSignOut={handleSignOut} />
        </Page>
      )
    }
    if (auth === 'unverified' || pendingVerificationEmail !== null) {
      return (
        <>
          <RememberReturn path={pathname} onRemember={handleRememberReturn} />
          <Navigate to="/verify-email" replace />
        </>
      )
    }
    return (
      <>
        {expiryNotice ? null : (
          <RememberReturn path={pathname} onRemember={handleRememberReturn} />
        )}
        <Navigate to="/login" replace />
      </>
    )
  }

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
        <Route path="/" element={rootEntry} />
        <Route path="/login" element={loginEntry} />
        <Route path="/register" element={registerEntry} />
        <Route path="/verify-email" element={verifyEntry} />
        <Route path="/forgot-password" element={<ForgotPasswordEntry />} />
        <Route path="/reset-password" element={<ResetPasswordEntry />} />
        <Route path="/translate" element={featureEntry('translate', '/translate')} />
        <Route path="/rewrite" element={featureEntry('rewrite', '/rewrite')} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </div>
  )
}
