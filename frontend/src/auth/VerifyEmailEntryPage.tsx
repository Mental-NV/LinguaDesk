import { useEffect, useId, useRef, useState } from 'react'
import { Link, Navigate, useSearchParams } from 'react-router-dom'
import {
  confirmLocalAccountEmail,
  getLocalAccountSession,
  resendLocalAccountVerification,
} from '../api/accounts'
import { resolveSafeReturn } from './safeReturn'

const STATUS_MESSAGE = 'Check your email to verify your account.' as const
const CONFIRMING_MESSAGE = 'Confirming your email…' as const
const RESEND_SUCCESS_MESSAGE = 'Verification email sent.' as const
const INVALID_LINK_MESSAGE = 'This verification link is invalid or has expired.' as const
const RETRY_MESSAGE = 'We couldn\u2019t complete this request. Try again.' as const
const VERIFIED_MESSAGE = 'Your email is verified.' as const
const STILL_UNVERIFIED_MESSAGE =
  'Your email is still unverified. Open the link in your inbox, or send a new verification email below.' as const
const SIGNED_OUT_STATUS_MESSAGE =
  'You are signed out. Verify your email through its link, then sign in to continue.' as const
const EMAIL_REQUIRED_MESSAGE = 'Enter your email address.' as const
const EMAIL_SHAPE_MESSAGE = 'Enter a valid email address.' as const

const EMAIL_SHAPE_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/u

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

function isEmailShapeValid(email: string): boolean {
  return EMAIL_SHAPE_PATTERN.test(email)
}

type ConfirmPhase = 'idle' | 'confirming' | 'invalid' | 'retry'
type ResendPhase = 'idle' | 'sending' | 'sent' | 'retry'
type StatusPhase = 'idle' | 'checking' | 'unverified' | 'signedOut' | 'retry'

interface VerifyEmailEntryPageProps {
  readonly email: string | null
  readonly allowUnverifiedEntry?: boolean
  readonly protectedContinuationPath?: string
}

export function VerifyEmailEntryPage({
  email,
  allowUnverifiedEntry = false,
  protectedContinuationPath = '/translate',
}: VerifyEmailEntryPageProps) {
  const [searchParams, setSearchParams] = useSearchParams()
  const [material] = useState<LinkMaterial>(() => classifyLinkMaterial(searchParams))
  const [confirmPhase, setConfirmPhase] = useState<ConfirmPhase>(
    material.kind === 'delivered' ? 'confirming' : 'idle',
  )
  const [verified, setVerified] = useState(false)
  const [continuation, setContinuation] = useState<'signin' | 'protected'>('signin')
  const [resendEmail, setResendEmail] = useState(email ?? '')
  const [resendFieldError, setResendFieldError] = useState<string | null>(null)
  const [resendPhase, setResendPhase] = useState<ResendPhase>('idle')
  const [cooldownSeconds, setCooldownSeconds] = useState(0)
  const [statusPhase, setStatusPhase] = useState<StatusPhase>('idle')

  const formId = useId()
  const resendEmailId = `${formId}-resend-email`
  const statusRegionId = `${formId}-status`

  const mountedRef = useRef(true)
  const strippedRef = useRef(false)
  const lastAnnouncedRef = useRef('')
  const confirmStartedRef = useRef(false)
  const confirmFlightRef = useRef(0)
  const resendFlightRef = useRef(false)
  const statusFlightRef = useRef(false)
  const resendEmailRef = useRef<HTMLInputElement>(null)
  const statusRegionRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
    }
  }, [])

  useEffect(() => {
    if (strippedRef.current) return
    strippedRef.current = true
    if ([...searchParams.keys()].length > 0) setSearchParams({}, { replace: true })
  }, [searchParams, setSearchParams])

  const focusStatus = (): void => {
    statusRegionRef.current?.focus()
  }

  const shouldAnnounce =
    verified ||
    confirmPhase === 'invalid' ||
    confirmPhase === 'retry' ||
    resendPhase === 'sent' ||
    resendPhase === 'retry' ||
    (statusPhase !== 'idle' && statusPhase !== 'checking')

  useEffect(() => {
    const signature = `${String(verified)}|${confirmPhase}|${resendPhase}|${statusPhase}`
    if (signature === lastAnnouncedRef.current) return
    lastAnnouncedRef.current = signature
    if (shouldAnnounce) focusStatus()
  })

  const postConfirm = (userId: string, code: string): void => {
    if (confirmFlightRef.current > 0) return
    const flight = confirmFlightRef.current + 1
    confirmFlightRef.current = flight
    setConfirmPhase('confirming')

    void (async () => {
      let result: Awaited<ReturnType<typeof confirmLocalAccountEmail>>
      try {
        result = await confirmLocalAccountEmail({ userId, code })
      } catch {
        result = { kind: 'retry', status: null }
      }
      if (!mountedRef.current || confirmFlightRef.current !== flight) return
      confirmFlightRef.current = 0
      if (result.kind === 'verified') {
        setVerified(true)
        setContinuation('signin')
        setConfirmPhase('idle')
      } else if (result.kind === 'invalid') {
        setConfirmPhase('invalid')
      } else {
        setConfirmPhase('retry')
      }
    })()
  }

  useEffect(() => {
    if (material.kind !== 'delivered' || confirmStartedRef.current) return
    confirmStartedRef.current = true
    postConfirm(material.userId, material.code)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const coolingDown = cooldownSeconds > 0

  useEffect(() => {
    if (!coolingDown) return
    const timer = setInterval(() => {
      setCooldownSeconds((current) => (current > 0 ? current - 1 : 0))
    }, 1000)
    return () => clearInterval(timer)
  }, [coolingDown])

  if (email === null && material.kind === 'none' && !allowUnverifiedEntry) {
    return <Navigate to="/register" replace />
  }

  const continuationPath = resolveSafeReturn(protectedContinuationPath)
  const continuationLabel =
    continuationPath === '/rewrite' ? 'Continue to Rewriting' : 'Continue to Translation'

  const cooldownActive = cooldownSeconds > 0
  const showInvalid = confirmPhase === 'invalid' || material.kind === 'malformed'
  const busy = confirmPhase === 'confirming' || resendPhase === 'sending' || statusPhase === 'checking'

  const handleResend = (event: React.FormEvent<HTMLFormElement>): void => {
    event.preventDefault()
    if (resendPhase === 'sending' || cooldownActive) return
    if (resendEmail.length === 0) {
      setResendFieldError(EMAIL_REQUIRED_MESSAGE)
      resendEmailRef.current?.focus()
      return
    }
    if (!isEmailShapeValid(resendEmail)) {
      setResendFieldError(EMAIL_SHAPE_MESSAGE)
      resendEmailRef.current?.focus()
      return
    }
    if (resendFlightRef.current) return
    resendFlightRef.current = true
    setResendFieldError(null)
    setResendPhase('sending')

    void (async () => {
      let result: Awaited<ReturnType<typeof resendLocalAccountVerification>>
      try {
        result = await resendLocalAccountVerification({ email: resendEmail })
      } catch {
        result = { kind: 'retry', status: null }
      }
      if (!mountedRef.current) return
      resendFlightRef.current = false
      if (result.kind === 'sent') {
        setResendPhase('sent')
        setCooldownSeconds(result.retryAfterSeconds)
      } else if (result.kind === 'field') {
        setResendPhase('idle')
        setResendFieldError(EMAIL_SHAPE_MESSAGE)
        resendEmailRef.current?.focus()
        return
      } else {
        setResendPhase('retry')
      }
    })()
  }

  const handleStatusCheck = (): void => {
    if (statusFlightRef.current) return
    statusFlightRef.current = true
    setStatusPhase('checking')

    void (async () => {
      let result: Awaited<ReturnType<typeof getLocalAccountSession>>
      try {
        result = await getLocalAccountSession()
      } catch {
        result = { kind: 'retry', status: null }
      }
      if (!mountedRef.current) return
      statusFlightRef.current = false
      if (result.kind === 'verified') {
        setVerified(true)
        setContinuation('protected')
        setStatusPhase('idle')
      } else if (result.kind === 'unverified') {
        setStatusPhase('unverified')
      } else if (result.kind === 'signedOut') {
        setStatusPhase('signedOut')
      } else {
        setStatusPhase('retry')
      }
    })()
  }

  const handleConfirmRetry = (): void => {
    if (material.kind !== 'delivered') return
    postConfirm(material.userId, material.code)
  }

  return (
    <>
      {verified ? (
        <div ref={statusRegionRef} id={statusRegionId} role="status" tabIndex={-1}>
          <p>{VERIFIED_MESSAGE}</p>
          <p className="form-switch">
            {continuation === 'protected' ? (
              <Link className="primary-link" to={continuationPath}>
                {continuationLabel}
              </Link>
            ) : (
              <Link className="primary-link" to="/login">
                Go to sign in
              </Link>
            )}
          </p>
        </div>
      ) : (
        <>
          {material.kind === 'none' && email !== null ? (
            <div>
              <p>{STATUS_MESSAGE}</p>
              <p>
                We sent a verification link to <strong>{email}</strong>.
              </p>
            </div>
          ) : null}
          {material.kind === 'delivered' && confirmPhase === 'confirming' ? (
            <div ref={statusRegionRef} id={statusRegionId} role="status" tabIndex={-1}>
              <p>{CONFIRMING_MESSAGE}</p>
            </div>
          ) : null}
          {showInvalid ? (
            <div
              ref={statusRegionRef}
              id={statusRegionId}
              className="server-alert"
              role="alert"
              tabIndex={-1}
            >
              <p>{INVALID_LINK_MESSAGE}</p>
            </div>
          ) : null}
          {confirmPhase === 'retry' ? (
            <div
              ref={statusRegionRef}
              id={statusRegionId}
              className="server-alert"
              role="alert"
              tabIndex={-1}
            >
              <p>{RETRY_MESSAGE}</p>
              <p>
                <button
                  type="button"
                  className="secondary-button"
                  disabled={busy}
                  onClick={handleConfirmRetry}
                >
                  Try again
                </button>
              </p>
            </div>
          ) : null}
          {resendPhase === 'sent' ? (
            <div ref={statusRegionRef} id={statusRegionId} role="status" tabIndex={-1}>
              <p>{RESEND_SUCCESS_MESSAGE}</p>
              {cooldownActive ? (
                <p>You can request another email in {cooldownSeconds} seconds.</p>
              ) : null}
            </div>
          ) : null}
          {resendPhase === 'retry' ? (
            <div className="server-alert" role="alert">
              <p>{RETRY_MESSAGE}</p>
            </div>
          ) : null}
          {statusPhase === 'unverified' ? (
            <div className="server-alert" role="alert">
              <p>{STILL_UNVERIFIED_MESSAGE}</p>
            </div>
          ) : null}
          {statusPhase === 'signedOut' ? (
            <div className="server-alert" role="alert">
              <p>{SIGNED_OUT_STATUS_MESSAGE}</p>
              <p className="form-switch">
                <Link className="primary-link" to="/login">
                  Go to sign in
                </Link>
              </p>
            </div>
          ) : null}
          {statusPhase === 'retry' ? (
            <div className="server-alert" role="alert">
              <p>{RETRY_MESSAGE}</p>
            </div>
          ) : null}

          <form aria-label="Resend verification email" noValidate onSubmit={handleResend}>
            <div className="field">
              <label htmlFor={resendEmailId}>Email</label>
              <input
                ref={resendEmailRef}
                id={resendEmailId}
                name="email"
                type="email"
                autoComplete="email"
                value={resendEmail}
                disabled={resendPhase === 'sending' || cooldownActive}
                aria-invalid={resendFieldError !== null}
                aria-describedby={resendFieldError === null ? undefined : `${resendEmailId}-error`}
                onChange={(event) => setResendEmail(event.target.value)}
              />
              {resendFieldError === null ? null : (
                <p className="field-error" id={`${resendEmailId}-error`}>
                  {resendFieldError}
                </p>
              )}
            </div>
            <div className="form-actions">
              <button
                type="submit"
                className="primary-button"
                disabled={resendPhase === 'sending' || cooldownActive}
              >
                {resendPhase === 'sending' ? 'Sending…' : 'Send a new verification email'}
              </button>
            </div>
            {resendFieldError === null ? null : (
              <div className="error-summary" role="alert">
                <p>{resendFieldError}</p>
                <ul>
                  <li>
                    <a
                      href={`#${resendEmailId}`}
                      onClick={(event) => {
                        event.preventDefault()
                        resendEmailRef.current?.focus()
                      }}
                    >
                      {resendFieldError}
                    </a>
                  </li>
                </ul>
              </div>
            )}
          </form>

          <p className="form-switch">
            <button
              type="button"
              className="secondary-button"
              disabled={statusPhase === 'checking'}
              onClick={handleStatusCheck}
            >
              {statusPhase === 'checking' ? 'Checking…' : 'I’ve verified my email'}
            </button>
          </p>
        </>
      )}
    </>
  )
}
