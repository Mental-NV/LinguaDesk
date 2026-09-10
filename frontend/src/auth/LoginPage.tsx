import { useEffect, useId, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getAccountAntiforgeryToken, signInLocalAccount } from '../api/accounts'

const SUMMARY_MESSAGE = 'Check the highlighted fields.' as const
const EMAIL_REQUIRED_MESSAGE = 'Enter your email address.' as const
const EMAIL_SHAPE_MESSAGE = 'Enter a valid email address.' as const
const PASSWORD_REQUIRED_MESSAGE = 'Enter your password.' as const
const INVALID_CREDENTIALS_MESSAGE = 'Email or password is incorrect.' as const
const RETRY_MESSAGE = 'We couldn\u2019t complete this request. Try again.' as const
const EXPIRED_SESSION_MESSAGE = 'Your session expired. Sign in again to continue.' as const

const MAX_EMAIL_SCALARS = 254 as const

const EMAIL_SHAPE_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/u

type FieldName = 'email' | 'password'

interface FieldIssue {
  readonly field: FieldName
  readonly message: string
}

function countScalars(value: string): number | null {
  let scalars = 0
  let index = 0
  while (index < value.length) {
    const unit = value.charCodeAt(index)
    if (unit >= 0xd800 && unit <= 0xdbff) {
      if (index + 1 >= value.length) return null
      const next = value.charCodeAt(index + 1)
      if (next < 0xdc00 || next > 0xdfff) return null
      index += 2
    } else if (unit >= 0xdc00 && unit <= 0xdfff) {
      return null
    } else {
      index += 1
    }
    scalars += 1
  }
  return scalars
}

function isEmailShapeValid(email: string): boolean {
  if (!EMAIL_SHAPE_PATTERN.test(email)) return false
  const scalars = countScalars(email)
  return scalars !== null && scalars <= MAX_EMAIL_SCALARS
}

function validateLocal(email: string, password: string): FieldIssue[] {
  const found: FieldIssue[] = []
  if (email.length === 0) {
    found.push({ field: 'email', message: EMAIL_REQUIRED_MESSAGE })
  } else if (!isEmailShapeValid(email)) {
    found.push({ field: 'email', message: EMAIL_SHAPE_MESSAGE })
  }
  if (password.length === 0) {
    found.push({ field: 'password', message: PASSWORD_REQUIRED_MESSAGE })
  }
  return found
}

export type SignInCompletionStatus = 'verified' | 'unverified'

interface LoginPageProps {
  readonly onSignedIn: (status: SignInCompletionStatus, email: string) => void
  readonly sessionExpired: boolean
  readonly onExpiryShown?: () => void
}

export function LoginPage({ onSignedIn, sessionExpired, onExpiryShown }: LoginPageProps) {
  const navigate = useNavigate()
  const formId = useId()
  const emailId = `${formId}-email`
  const passwordId = `${formId}-password`
  const summaryId = `${formId}-summary`
  const serverAlertId = `${formId}-server-alert`
  const expiredAlertId = `${formId}-expired`

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [issues, setIssues] = useState<readonly FieldIssue[]>([])
  const [serverAlert, setServerAlert] = useState<'invalid' | 'retry' | null>(null)
  const [showExpired] = useState(sessionExpired)

  const emailRef = useRef<HTMLInputElement>(null)
  const passwordRef = useRef<HTMLInputElement>(null)
  const lastSecretRef = useRef(false)
  const pendingFocusRef = useRef<FieldName | null>(null)
  const mountedRef = useRef(true)
  const abortRef = useRef<AbortController | null>(null)
  const submissionRef = useRef(0)
  const revealStateRef = useRef<{ start: number | null; end: number | null } | null>(null)

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      abortRef.current?.abort()
    }
  }, [])

  useEffect(() => {
    if (showExpired) onExpiryShown?.()
  }, [showExpired, onExpiryShown])

  useEffect(() => {
    const handlePageShow = (): void => {
      setPassword('')
    }
    window.addEventListener('pageshow', handlePageShow)
    return () => {
      window.removeEventListener('pageshow', handlePageShow)
    }
  }, [])

  const focusField = (field: FieldName): void => {
    const target = field === 'email' ? emailRef.current : passwordRef.current
    target?.focus()
  }

  useEffect(() => {
    if (!submitting && pendingFocusRef.current !== null) {
      const target = pendingFocusRef.current
      pendingFocusRef.current = null
      focusField(target)
    }
  })

  useEffect(() => {
    const pending = revealStateRef.current
    if (pending === null) return
    revealStateRef.current = null
    const target = passwordRef.current
    if (target === null) return
    target.focus()
    if (pending.start !== null && pending.end !== null) {
      try {
        target.setSelectionRange(pending.start, pending.end)
      } catch {
        /* selection unsupported; focus alone preserves access */
      }
    }
  }, [showPassword])

  const issueMessage = (field: FieldName): string | undefined =>
    issues.find((issue) => issue.field === field)?.message

  const describedBy = (field: FieldName): string | undefined => {
    const parts: string[] = []
    if (issueMessage(field) !== undefined) parts.push(`${field === 'email' ? emailId : passwordId}-error`)
    else if (field === 'email' && serverAlert === 'invalid') parts.push(serverAlertId)
    return parts.length === 0 ? undefined : parts.join(' ')
  }

  const toggleShowPassword = (): void => {
    const secret = passwordRef.current
    if (secret !== null && (document.activeElement === secret || lastSecretRef.current)) {
      let start: number | null = null
      let end: number | null = null
      try {
        start = secret.selectionStart
        end = secret.selectionEnd
      } catch {
        start = null
        end = null
      }
      revealStateRef.current = { start, end }
    } else {
      revealStateRef.current = null
    }
    setShowPassword((current) => !current)
  }

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>): void => {
    event.preventDefault()
    if (submitting) return

    const localIssues = validateLocal(email, password)
    if (localIssues.length > 0) {
      setIssues(localIssues)
      setServerAlert(null)
      focusField(localIssues[0].field)
      return
    }

    const submissionId = submissionRef.current + 1
    submissionRef.current = submissionId
    abortRef.current?.abort()
    const controller = new AbortController()
    abortRef.current = controller
    setIssues([])
    setServerAlert(null)
    setSubmitting(true)

    void (async () => {
      const bootstrap = await getAccountAntiforgeryToken({ signal: controller.signal })
      if (!mountedRef.current || submissionRef.current !== submissionId) return
      if (bootstrap.kind !== 'ok') {
        setPassword('')
        setSubmitting(false)
        setServerAlert('retry')
        return
      }
      let result: Awaited<ReturnType<typeof signInLocalAccount>>
      try {
        result = await signInLocalAccount(
          { email, password },
          { signal: controller.signal, antiforgeryToken: bootstrap.requestToken },
        )
      } catch {
        result = { kind: 'retry', status: null }
      }
      if (!mountedRef.current || submissionRef.current !== submissionId) return
      if (result.kind === 'signedIn') {
        const completedEmail = email
        setPassword('')
        if (result.verificationStatus === 'verified') {
          onSignedIn('verified', completedEmail)
        } else {
          onSignedIn('unverified', completedEmail)
          navigate('/verify-email')
        }
        return
      }
      setPassword('')
      setSubmitting(false)
      if (result.kind === 'invalidCredentials') {
        setServerAlert('invalid')
        pendingFocusRef.current = 'email'
      } else {
        setServerAlert('retry')
      }
    })()
  }

  const emailIssue = issueMessage('email')
  const passwordIssue = issueMessage('password')
  const passwordType = showPassword ? 'text' : 'password'

  return (
    <form aria-label="Sign in" noValidate onSubmit={handleSubmit}>
      {showExpired ? (
        <div className="server-alert" id={expiredAlertId} role="alert">
          <p>{EXPIRED_SESSION_MESSAGE}</p>
        </div>
      ) : null}

      <div className="form-fields">
        <div className="field">
          <label htmlFor={emailId}>Email</label>
          <input
            ref={emailRef}
            id={emailId}
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            disabled={submitting}
            aria-invalid={emailIssue !== undefined || serverAlert === 'invalid'}
            aria-describedby={describedBy('email')}
            onChange={(event) => setEmail(event.target.value)}
          />
          {emailIssue === undefined ? null : (
            <p className="field-error" id={`${emailId}-error`}>
              {emailIssue}
            </p>
          )}
        </div>

        <div className="field">
          <label htmlFor={passwordId}>Password</label>
          <input
            ref={passwordRef}
            id={passwordId}
            name="password"
            type={passwordType}
            autoComplete="current-password"
            value={password}
            disabled={submitting}
            aria-invalid={passwordIssue !== undefined}
            aria-describedby={describedBy('password')}
            onFocus={() => {
              lastSecretRef.current = true
            }}
            onChange={(event) => setPassword(event.target.value)}
          />
          {passwordIssue === undefined ? null : (
            <p className="field-error" id={`${passwordId}-error`}>
              {passwordIssue}
            </p>
          )}
        </div>
      </div>

      <div className="form-actions">
        <button type="button" className="secondary-button" aria-pressed={showPassword} onClick={toggleShowPassword}>
          {showPassword ? 'Hide password' : 'Show password'}
        </button>
        <button type="submit" className="primary-button" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </div>

      {issues.length === 0 ? null : (
        <div className="error-summary" role="alert">
          <p id={summaryId}>{SUMMARY_MESSAGE}</p>
          <ul aria-labelledby={summaryId}>
            {issues.map((issue, index) => (
              <li key={`${issue.field}-${index}`}>
                <a
                  href={`#${issue.field === 'email' ? emailId : passwordId}`}
                  onClick={(event) => {
                    event.preventDefault()
                    focusField(issue.field)
                  }}
                >
                  {issue.message}
                </a>
              </li>
            ))}
          </ul>
        </div>
      )}

      {serverAlert === null ? null : (
        <div
          className="server-alert"
          id={serverAlertId}
          role="alert"
          tabIndex={-1}
          autoFocus={serverAlert === 'retry'}
        >
          <p>{serverAlert === 'invalid' ? INVALID_CREDENTIALS_MESSAGE : RETRY_MESSAGE}</p>
        </div>
      )}

      <p className="form-switch">
        <Link to="/forgot-password">Forgot password</Link>
      </p>
      <p className="form-switch">
        New to LinguaDesk? <Link to="/register">Create account</Link>
      </p>
    </form>
  )
}
