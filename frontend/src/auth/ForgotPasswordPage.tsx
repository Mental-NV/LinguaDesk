import { useEffect, useId, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { requestLocalAccountPasswordReset } from '../api/accounts'

const SUMMARY_MESSAGE = 'Check the highlighted fields.' as const
const EMAIL_REQUIRED_MESSAGE = 'Enter your email address.' as const
const EMAIL_SHAPE_MESSAGE = 'Enter a valid email address.' as const
const SUCCESS_MESSAGE = 'If an account exists for that email, we sent a reset link.' as const
const RETRY_MESSAGE = 'We couldn\u2019t complete this request. Try again.' as const
const DELIVERY_MESSAGE = 'We couldn\u2019t send the email. Try again.' as const

const MAX_EMAIL_SCALARS = 254 as const

const EMAIL_SHAPE_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/u

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

function focusPageHeading(): void {
  const heading = document.querySelector('#main-content h1')
  if (heading instanceof HTMLElement) heading.focus()
}

export function ForgotPasswordPage() {
  const formId = useId()
  const emailId = `${formId}-email`
  const summaryId = `${formId}-summary`
  const outcomeId = `${formId}-outcome`

  const [email, setEmail] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [fieldError, setFieldError] = useState<string | null>(null)
  const [alert, setAlert] = useState<'retry' | 'delivery' | null>(null)
  const [sent, setSent] = useState(false)

  const emailRef = useRef<HTMLInputElement>(null)
  const alertRef = useRef<HTMLDivElement>(null)
  const mountedRef = useRef(true)
  const abortRef = useRef<AbortController | null>(null)
  const submissionRef = useRef(0)
  const pendingFocusRef = useRef(false)

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      abortRef.current?.abort()
    }
  }, [])

  useEffect(() => {
    if (sent) focusPageHeading()
  }, [sent])

  useEffect(() => {
    if (alert !== null) alertRef.current?.focus()
  }, [alert])

  useEffect(() => {
    if (!submitting && pendingFocusRef.current) {
      pendingFocusRef.current = false
      emailRef.current?.focus()
    }
  })

  const submitRequest = (address: string): void => {
    if (submitting) return
    const submissionId = submissionRef.current + 1
    submissionRef.current = submissionId
    abortRef.current?.abort()
    const controller = new AbortController()
    abortRef.current = controller
    setFieldError(null)
    setAlert(null)
    setSubmitting(true)

    void (async () => {
      let result: Awaited<ReturnType<typeof requestLocalAccountPasswordReset>>
      try {
        result = await requestLocalAccountPasswordReset({ email: address }, { signal: controller.signal })
      } catch {
        result = { kind: 'retry', status: null }
      }
      if (!mountedRef.current || submissionRef.current !== submissionId) return
      if (result.kind === 'sent') {
        setSubmitting(false)
        setSent(true)
        return
      }
      setSubmitting(false)
      if (result.kind === 'field') {
        setFieldError(EMAIL_SHAPE_MESSAGE)
        pendingFocusRef.current = true
      } else if (result.kind === 'delivery') {
        setAlert('delivery')
      } else {
        setAlert('retry')
      }
    })()
  }

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>): void => {
    event.preventDefault()
    if (submitting) return
    if (email.length === 0) {
      setFieldError(EMAIL_REQUIRED_MESSAGE)
      setAlert(null)
      emailRef.current?.focus()
      return
    }
    if (!isEmailShapeValid(email)) {
      setFieldError(EMAIL_SHAPE_MESSAGE)
      setAlert(null)
      emailRef.current?.focus()
      return
    }
    submitRequest(email)
  }

  const handleRetry = (): void => {
    if (submitting || !isEmailShapeValid(email)) return
    submitRequest(email)
  }

  if (sent) {
    return (
      <div ref={alertRef} id={outcomeId} role="status" tabIndex={-1}>
        <p>{SUCCESS_MESSAGE}</p>
        <p className="form-switch">
          <Link className="primary-link" to="/login">
            Back to sign in
          </Link>
        </p>
      </div>
    )
  }

  return (
    <>
      <form aria-label="Forgot password" noValidate onSubmit={handleSubmit}>
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
              aria-invalid={fieldError !== null}
              aria-describedby={fieldError === null ? undefined : `${emailId}-error`}
              onChange={(event) => setEmail(event.target.value)}
            />
            {fieldError === null ? null : (
              <p className="field-error" id={`${emailId}-error`}>
                {fieldError}
              </p>
            )}
          </div>
        </div>

        <div className="form-actions">
          <button type="submit" className="primary-button" disabled={submitting}>
            {submitting ? 'Sending…' : 'Send reset link'}
          </button>
        </div>

        {fieldError === null ? null : (
          <div className="error-summary" role="alert">
            <p id={summaryId}>{SUMMARY_MESSAGE}</p>
            <ul aria-labelledby={summaryId}>
              <li>
                <a
                  href={`#${emailId}`}
                  onClick={(event) => {
                    event.preventDefault()
                    emailRef.current?.focus()
                  }}
                >
                  {fieldError}
                </a>
              </li>
            </ul>
          </div>
        )}
      </form>

      {alert === null ? null : (
        <div ref={alertRef} className="server-alert" role="alert" tabIndex={-1}>
          <p>{alert === 'delivery' ? DELIVERY_MESSAGE : RETRY_MESSAGE}</p>
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
      )}

      <p className="form-switch">
        <Link className="primary-link" to="/login">
          Back to sign in
        </Link>
      </p>
    </>
  )
}
