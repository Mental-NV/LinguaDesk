import { useEffect, useId, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { analyzeInput } from '../api/inputPolicy'
import { registerLocalAccount } from '../api/accounts'

const SUMMARY_MESSAGE = 'Check the highlighted fields.' as const
const EMAIL_REQUIRED_MESSAGE = 'Enter your email address.' as const
const EMAIL_SHAPE_MESSAGE = 'Enter a valid email address.' as const
const PASSWORD_REQUIRED_MESSAGE = 'Enter your password.' as const
const PASSWORD_MISMATCH_MESSAGE = 'Passwords do not match.' as const
const PASSWORD_POLICY_MESSAGE = 'Password must meet all requirements.' as const
const CONFLICT_MESSAGE =
  'We couldn\u2019t create an account with these details. Try signing in or use a different email.' as const
const RETRY_MESSAGE = 'We couldn\u2019t complete this request. Try again.' as const

const MIN_PASSWORD_SCALARS = 15 as const
const MAX_PASSWORD_SCALARS = 128 as const
const MAX_EMAIL_SCALARS = 254 as const

const EMAIL_SHAPE_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/u

type FieldName = 'email' | 'password' | 'confirm'

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

function isPasswordPolicyMet(password: string): boolean {
  if (password.length === 0) return false
  const analysis = analyzeInput(password, MAX_PASSWORD_SCALARS)
  return (
    analysis.isUnicodeValid &&
    analysis.scalarCount !== null &&
    analysis.scalarCount >= MIN_PASSWORD_SCALARS &&
    !analysis.isOversized
  )
}

function validateLocal(email: string, password: string, confirm: string): FieldIssue[] {
  const found: FieldIssue[] = []
  if (email.length === 0) {
    found.push({ field: 'email', message: EMAIL_REQUIRED_MESSAGE })
  } else if (!isEmailShapeValid(email)) {
    found.push({ field: 'email', message: EMAIL_SHAPE_MESSAGE })
  }
  if (password.length === 0) {
    found.push({ field: 'password', message: PASSWORD_REQUIRED_MESSAGE })
  } else if (!isPasswordPolicyMet(password)) {
    found.push({ field: 'password', message: PASSWORD_POLICY_MESSAGE })
  }
  if (confirm !== password) {
    found.push({ field: 'confirm', message: PASSWORD_MISMATCH_MESSAGE })
  }
  return found
}

interface RegisterPageProps {
  readonly onRegistered: (email: string) => void
}

export function RegisterPage({ onRegistered }: RegisterPageProps) {
  const navigate = useNavigate()
  const formId = useId()
  const emailId = `${formId}-email`
  const passwordId = `${formId}-password`
  const confirmId = `${formId}-confirm`
  const summaryId = `${formId}-summary`
  const serverAlertId = `${formId}-server-alert`
  const checklistId = `${formId}-checklist`

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [issues, setIssues] = useState<readonly FieldIssue[]>([])
  const [serverFields, setServerFields] = useState<readonly ('email' | 'password')[]>([])
  const [serverAlert, setServerAlert] = useState<'conflict' | 'retry' | null>(null)

  const emailRef = useRef<HTMLInputElement>(null)
  const passwordRef = useRef<HTMLInputElement>(null)
  const confirmRef = useRef<HTMLInputElement>(null)
  const lastSecretRef = useRef<'password' | 'confirm' | null>(null)
  const serverAlertRef = useRef<HTMLDivElement>(null)
  const submissionRef = useRef(0)
  const pendingFocusRef = useRef<FieldName | null>(null)
  const mountedRef = useRef(true)
  const abortRef = useRef<AbortController | null>(null)
  const revealStateRef = useRef<{ name: 'password' | 'confirm'; start: number | null; end: number | null } | null>(
    null,
  )

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      abortRef.current?.abort()
    }
  }, [])

  const fieldId = (field: FieldName): string =>
    field === 'email' ? emailId : field === 'password' ? passwordId : confirmId

  const focusField = (field: FieldName): void => {
    const target =
      field === 'email' ? emailRef.current : field === 'password' ? passwordRef.current : confirmRef.current
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
    const target = pending.name === 'password' ? passwordRef.current : confirmRef.current
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

  const isServerFlagged = (field: 'email' | 'password'): boolean => serverFields.includes(field)

  const describedBy = (field: FieldName): string | undefined => {
    const parts: string[] = []
    if (field === 'password') parts.push(checklistId)
    if (issueMessage(field) !== undefined) parts.push(`${fieldId(field)}-error`)
    else if (field !== 'confirm' && isServerFlagged(field)) parts.push(serverAlertId)
    return parts.length === 0 ? undefined : parts.join(' ')
  }

  const toggleShowPassword = (): void => {
    const active = document.activeElement
    const secret =
      active === passwordRef.current
        ? passwordRef.current
        : active === confirmRef.current
          ? confirmRef.current
          : lastSecretRef.current === 'password'
            ? passwordRef.current
            : lastSecretRef.current === 'confirm'
              ? confirmRef.current
              : null
    if (secret !== null) {
      const input = secret as HTMLInputElement
      let start: number | null = null
      let end: number | null = null
      try {
        start = input.selectionStart
        end = input.selectionEnd
      } catch {
        start = null
        end = null
      }
      revealStateRef.current = {
        name: secret === confirmRef.current ? 'confirm' : 'password',
        start,
        end,
      }
    } else {
      revealStateRef.current = null
    }
    setShowPassword((current) => !current)
  }

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>): void => {
    event.preventDefault()
    if (submitting) return

    const localIssues = validateLocal(email, password, confirm)
    if (localIssues.length > 0) {
      setIssues(localIssues)
      setServerFields([])
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
    setServerFields([])
    setServerAlert(null)
    setSubmitting(true)

    void (async () => {
      let result: Awaited<ReturnType<typeof registerLocalAccount>>
      try {
        result = await registerLocalAccount({ email, password }, { signal: controller.signal })
      } catch {
        result = { kind: 'retry', status: null }
      }
      if (!mountedRef.current || submissionRef.current !== submissionId) return
      if (result.kind === 'accepted') {
        onRegistered(email)
        navigate('/verify-email')
        return
      }
      setPassword('')
      setConfirm('')
      setSubmitting(false)
      if (result.kind === 'field') {
        const flagged = result.fields.length > 0 ? result.fields : (['email'] as const)
        setServerFields(flagged)
        setServerAlert('conflict')
        pendingFocusRef.current = flagged[0]
      } else {
        setServerFields([])
        setServerAlert('retry')
      }
    })()
  }

  const passwordMet = isPasswordPolicyMet(password)
  const emailIssue = issueMessage('email')
  const passwordIssue = issueMessage('password')
  const confirmIssue = issueMessage('confirm')
  const passwordType = showPassword ? 'text' : 'password'

  return (
    <form aria-label="Create account" noValidate onSubmit={handleSubmit}>
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
            aria-invalid={emailIssue !== undefined || isServerFlagged('email')}
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
            autoComplete="new-password"
            value={password}
            disabled={submitting}
            aria-invalid={passwordIssue !== undefined || isServerFlagged('password')}
            aria-describedby={describedBy('password')}
            onFocus={() => {
              lastSecretRef.current = 'password'
            }}
            onChange={(event) => setPassword(event.target.value)}
          />
          <ul className="checklist" id={checklistId} aria-label="Password requirements">
            <li data-met={passwordMet}>
              <span aria-hidden="true">{passwordMet ? '✓' : '○'}</span> Use 15 to 128 characters
              <span className="visually-hidden">{passwordMet ? ' (met)' : ' (not met)'}</span>
            </li>
          </ul>
          {passwordIssue === undefined ? null : (
            <p className="field-error" id={`${passwordId}-error`}>
              {passwordIssue}
            </p>
          )}
        </div>

        <div className="field">
          <label htmlFor={confirmId}>Confirm password</label>
          <input
            ref={confirmRef}
            id={confirmId}
            name="confirmPassword"
            type={passwordType}
            autoComplete="new-password"
            value={confirm}
            disabled={submitting}
            aria-invalid={confirmIssue !== undefined}
            aria-describedby={describedBy('confirm')}
            onFocus={() => {
              lastSecretRef.current = 'confirm'
            }}
            onChange={(event) => setConfirm(event.target.value)}
          />
          {confirmIssue === undefined ? null : (
            <p className="field-error" id={`${confirmId}-error`}>
              {confirmIssue}
            </p>
          )}
        </div>
      </div>

      <div className="form-actions">
        <button type="button" className="secondary-button" aria-pressed={showPassword} onClick={toggleShowPassword}>
          {showPassword ? 'Hide password' : 'Show password'}
        </button>
        <button type="submit" className="primary-button" disabled={submitting}>
          {submitting ? 'Creating account…' : 'Create account'}
        </button>
      </div>

      {issues.length === 0 ? null : (
        <div className="error-summary" role="alert">
          <p id={summaryId}>{SUMMARY_MESSAGE}</p>
          <ul aria-labelledby={summaryId}>
            {issues.map((issue, index) => (
              <li key={`${issue.field}-${index}`}>
                <a
                  href={`#${fieldId(issue.field)}`}
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
          ref={serverAlertRef}
          role="alert"
          tabIndex={-1}
          autoFocus={serverAlert === 'retry'}
        >
          <p>{serverAlert === 'conflict' ? CONFLICT_MESSAGE : RETRY_MESSAGE}</p>
        </div>
      )}

      <p className="form-switch">
        Already have an account? <Link to="/login">Sign in</Link>
      </p>
    </form>
  )
}
