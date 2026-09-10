import { useEffect, useId, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { analyzeInput } from '../api/inputPolicy'
import { resetLocalAccountPassword } from '../api/accounts'

const SUMMARY_MESSAGE = 'Check the highlighted fields.' as const
const PASSWORD_REQUIRED_MESSAGE = 'Enter your password.' as const
const PASSWORD_MISMATCH_MESSAGE = 'Passwords do not match.' as const
const PASSWORD_POLICY_MESSAGE = 'Password must meet all requirements.' as const
const SUCCESS_MESSAGE = 'Password updated. Sign in with your new password.' as const
const INVALID_LINK_MESSAGE = 'This reset link is invalid or has expired.' as const
const RETRY_MESSAGE = 'We couldn\u2019t complete this request. Try again.' as const

const MIN_PASSWORD_SCALARS = 15 as const
const MAX_PASSWORD_SCALARS = 128 as const

type LinkMaterial =
  | { readonly kind: 'none' }
  | { readonly kind: 'delivered'; readonly userId: string; readonly code: string }
  | { readonly kind: 'malformed' }

function classifyLinkMaterial(params: URLSearchParams): LinkMaterial {
  const keys = [...params.keys()]
  if (keys.length === 0) return { kind: 'none' }
  if (keys.length === 2 && keys.includes('userId') && keys.includes('code')) {
    const userId = params.get('userId') ?? ''
    const code = params.get('code') ?? ''
    if (userId.length > 0 && code.length > 0) return { kind: 'delivered', userId, code }
  }
  return { kind: 'malformed' }
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

type FieldName = 'password' | 'confirm'

interface FieldIssue {
  readonly field: FieldName
  readonly message: string
}

function validateLocal(password: string, confirm: string): FieldIssue[] {
  const found: FieldIssue[] = []
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

function focusPageHeading(): void {
  const heading = document.querySelector('#main-content h1')
  if (heading instanceof HTMLElement) heading.focus()
}

export function ResetPasswordPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [material] = useState<LinkMaterial>(() => classifyLinkMaterial(searchParams))

  const formId = useId()
  const passwordId = `${formId}-password`
  const confirmId = `${formId}-confirm`
  const summaryId = `${formId}-summary`
  const outcomeId = `${formId}-outcome`
  const checklistId = `${formId}-checklist`

  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [issues, setIssues] = useState<readonly FieldIssue[]>([])
  const [invalid, setInvalid] = useState(false)
  const [retry, setRetry] = useState(false)
  const [succeeded, setSucceeded] = useState(false)

  const passwordRef = useRef<HTMLInputElement>(null)
  const confirmRef = useRef<HTMLInputElement>(null)
  const outcomeRef = useRef<HTMLDivElement>(null)
  const lastSecretRef = useRef<'password' | 'confirm' | null>(null)
  const mountedRef = useRef(true)
  const strippedRef = useRef(false)
  const abortRef = useRef<AbortController | null>(null)
  const submissionRef = useRef(0)
  const pendingFocusRef = useRef(false)
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

  useEffect(() => {
    if (strippedRef.current) return
    strippedRef.current = true
    if ([...searchParams.keys()].length > 0) setSearchParams({}, { replace: true })
  }, [searchParams, setSearchParams])

  useEffect(() => {
    const handlePageShow = (): void => {
      setPassword('')
      setConfirm('')
    }
    window.addEventListener('pageshow', handlePageShow)
    return () => {
      window.removeEventListener('pageshow', handlePageShow)
    }
  }, [])

  useEffect(() => {
    if (succeeded) focusPageHeading()
  }, [succeeded])

  useEffect(() => {
    if (invalid || retry) outcomeRef.current?.focus()
  }, [invalid, retry])

  useEffect(() => {
    if (!submitting && pendingFocusRef.current) {
      pendingFocusRef.current = false
      passwordRef.current?.focus()
    }
  })

  const fieldId = (field: FieldName): string => (field === 'password' ? passwordId : confirmId)

  const focusField = (field: FieldName): void => {
    const target = field === 'password' ? passwordRef.current : confirmRef.current
    target?.focus()
  }

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

  const describedBy = (field: FieldName): string | undefined => {
    const parts: string[] = []
    if (field === 'password') parts.push(checklistId)
    if (issueMessage(field) !== undefined) parts.push(`${fieldId(field)}-error`)
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

  const failInvalid = (): void => {
    setPassword('')
    setConfirm('')
    setSubmitting(false)
    setIssues([])
    setRetry(false)
    setInvalid(true)
  }

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>): void => {
    event.preventDefault()
    if (submitting || material.kind !== 'delivered') return

    const localIssues = validateLocal(password, confirm)
    if (localIssues.length > 0) {
      setIssues(localIssues)
      setRetry(false)
      focusField(localIssues[0].field)
      return
    }

    const submissionId = submissionRef.current + 1
    submissionRef.current = submissionId
    abortRef.current?.abort()
    const controller = new AbortController()
    abortRef.current = controller
    const materialSnapshot = material
    setIssues([])
    setRetry(false)
    setSubmitting(true)

    void (async () => {
      let result: Awaited<ReturnType<typeof resetLocalAccountPassword>>
      try {
        result = await resetLocalAccountPassword(
          { userId: materialSnapshot.userId, code: materialSnapshot.code, newPassword: password },
          { signal: controller.signal },
        )
      } catch {
        result = { kind: 'retry', status: null }
      }
      if (!mountedRef.current || submissionRef.current !== submissionId) return
      if (result.kind === 'reset') {
        setPassword('')
        setConfirm('')
        setSubmitting(false)
        setSucceeded(true)
        return
      }
      if (result.kind === 'field') {
        setPassword('')
        setConfirm('')
        setSubmitting(false)
        setIssues([{ field: 'password', message: PASSWORD_POLICY_MESSAGE }])
        pendingFocusRef.current = true
        return
      }
      if (result.kind === 'invalid') {
        failInvalid()
        return
      }
      setPassword('')
      setConfirm('')
      setSubmitting(false)
      setIssues([])
      setRetry(true)
    })()
  }

  const handleRetry = (): void => {
    if (submitting || material.kind !== 'delivered') return
    setRetry(false)
    setInvalid(false)
    setIssues([])
    passwordRef.current?.focus()
  }

  if (succeeded) {
    return (
      <div ref={outcomeRef} id={outcomeId} role="status" tabIndex={-1}>
        <p>{SUCCESS_MESSAGE}</p>
        <p className="form-switch">
          <Link className="primary-link" to="/login">
            Go to sign in
          </Link>
        </p>
      </div>
    )
  }

  if (material.kind !== 'delivered' || invalid) {
    return (
      <div ref={outcomeRef} id={outcomeId} className="server-alert" role="alert" tabIndex={-1}>
        <p>{INVALID_LINK_MESSAGE}</p>
        <p className="form-switch">
          <Link className="primary-link" to="/forgot-password">
            Request a new reset link
          </Link>
        </p>
      </div>
    )
  }

  const passwordMet = isPasswordPolicyMet(password)
  const passwordIssue = issueMessage('password')
  const confirmIssue = issueMessage('confirm')
  const passwordType = showPassword ? 'text' : 'password'

  return (
    <>
      <form aria-label="Reset password" noValidate onSubmit={handleSubmit}>
        <div className="form-fields">
          <div className="field">
            <label htmlFor={passwordId}>New password</label>
            <input
              ref={passwordRef}
              id={passwordId}
              name="newPassword"
              type={passwordType}
              autoComplete="new-password"
              value={password}
              disabled={submitting}
              aria-invalid={passwordIssue !== undefined}
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
            {submitting ? 'Resetting…' : 'Reset password'}
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
      </form>

      {retry ? (
        <div ref={outcomeRef} className="server-alert" role="alert" tabIndex={-1}>
          <p>{RETRY_MESSAGE}</p>
          <p>
            <button
              type="button"
              className="secondary-button"
              disabled={submitting}
              onClick={handleRetry}
            >
              Try again
            </button>
          </p>
        </div>
      ) : null}
    </>
  )
}
