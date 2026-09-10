import type { components, operations } from './generated/linguadesk-api.d'

export type RegistrationPayload =
  operations['registerLocalAccount']['requestBody']['content']['application/json']

export type RegistrationAcceptedBody =
  components['schemas']['RegistrationAccepted']

export type RegistrationProblemBody =
  components['schemas']['RegistrationProblemDetails']

export const REGISTRATION_PATH = '/api/accounts/register' as const

export type RegistrationResult =
  | { readonly kind: 'accepted' }
  | { readonly kind: 'field'; readonly fields: readonly ('email' | 'password')[] }
  | { readonly kind: 'retry'; readonly status: number | null }

export interface AccountCallOptions {
  readonly signal?: AbortSignal
}

export type RegistrationCallOptions = AccountCallOptions

function parseFieldNames(body: unknown): ('email' | 'password')[] {
  if (typeof body !== 'object' || body === null) return []
  const errors = (body as { errors?: unknown }).errors
  if (typeof errors !== 'object' || errors === null) return []
  const names: ('email' | 'password')[] = []
  for (const name of ['email', 'password'] as const) {
    if (Object.hasOwn(errors as Record<string, unknown>, name)) names.push(name)
  }
  return names
}

export async function registerLocalAccount(
  payload: RegistrationPayload,
  options: RegistrationCallOptions = {},
): Promise<RegistrationResult> {
  let response: Response
  try {
    response = await fetch(REGISTRATION_PATH, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: payload.email, password: payload.password }),
      signal: options.signal,
    })
  } catch {
    return { kind: 'retry', status: null }
  }

  if (response.status === 202) return { kind: 'accepted' }
  if (response.status === 400) {
    let fields: ('email' | 'password')[] = []
    try {
      fields = parseFieldNames(await response.json())
    } catch {
      fields = []
    }
    return { kind: 'field', fields }
  }
  return { kind: 'retry', status: response.status }
}

export const RESEND_VERIFICATION_PATH = '/api/accounts/resend-verification' as const
export const CONFIRM_EMAIL_PATH = '/api/accounts/confirm-email' as const
export const SESSION_PATH = '/api/accounts/session' as const

export type ResendVerificationPayload =
  operations['resendLocalAccountVerification']['requestBody']['content']['application/json']

export type ConfirmEmailPayload =
  operations['confirmLocalAccountEmail']['requestBody']['content']['application/json']

export type ResendVerificationResult =
  | { readonly kind: 'sent'; readonly retryAfterSeconds: number }
  | { readonly kind: 'field' }
  | { readonly kind: 'retry'; readonly status: number | null }

export type ConfirmEmailResult =
  | { readonly kind: 'verified' }
  | { readonly kind: 'invalid' }
  | { readonly kind: 'retry'; readonly status: number | null }

export type AccountSessionResult =
  | { readonly kind: 'verified' }
  | { readonly kind: 'unverified' }
  | { readonly kind: 'signedOut' }
  | { readonly kind: 'retry'; readonly status: number | null }

interface ProblemCategoryBody {
  readonly category?: unknown
  readonly errors?: unknown
}

function parseProblemBody(body: unknown): ProblemCategoryBody {
  if (typeof body !== 'object' || body === null) return {}
  const record = body as Record<string, unknown>
  return { category: record['category'], errors: record['errors'] }
}

async function readProblemBody(response: Response): Promise<ProblemCategoryBody> {
  try {
    return parseProblemBody(await response.json())
  } catch {
    return {}
  }
}

function readRetryAfterSeconds(body: unknown): number {
  if (typeof body === 'object' && body !== null) {
    const value = (body as Record<string, unknown>)['retryAfterSeconds']
    if (typeof value === 'number' && Number.isFinite(value) && value > 0) {
      return Math.floor(value)
    }
  }
  return 60
}

export async function resendLocalAccountVerification(
  payload: ResendVerificationPayload,
  options: AccountCallOptions = {},
): Promise<ResendVerificationResult> {
  let response: Response
  try {
    response = await fetch(RESEND_VERIFICATION_PATH, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: payload.email }),
      signal: options.signal,
    })
  } catch {
    return { kind: 'retry', status: null }
  }

  if (response.status === 202) {
    let body: unknown = null
    try {
      body = await response.json()
    } catch {
      body = null
    }
    return { kind: 'sent', retryAfterSeconds: readRetryAfterSeconds(body) }
  }
  if (response.status === 400) {
    const problem = await readProblemBody(response)
    if (problem.errors !== undefined && typeof problem.errors === 'object' && problem.errors !== null) {
      return { kind: 'field' }
    }
    return { kind: 'retry', status: response.status }
  }
  return { kind: 'retry', status: response.status }
}

export async function confirmLocalAccountEmail(
  payload: ConfirmEmailPayload,
  options: AccountCallOptions = {},
): Promise<ConfirmEmailResult> {
  let response: Response
  try {
    response = await fetch(CONFIRM_EMAIL_PATH, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userId: payload.userId, code: payload.code }),
      signal: options.signal,
    })
  } catch {
    return { kind: 'retry', status: null }
  }

  if (response.status === 200) return { kind: 'verified' }
  if (response.status === 400) return { kind: 'invalid' }
  return { kind: 'retry', status: response.status }
}

export async function getLocalAccountSession(
  options: AccountCallOptions = {},
): Promise<AccountSessionResult> {
  let response: Response
  try {
    response = await fetch(SESSION_PATH, { method: 'GET', signal: options.signal })
  } catch {
    return { kind: 'retry', status: null }
  }

  if (response.status === 200) {
    let body: unknown = null
    try {
      body = await response.json()
    } catch {
      return { kind: 'retry', status: response.status }
    }
    if (typeof body === 'object' && body !== null) {
      const status = (body as Record<string, unknown>)['verificationStatus']
      if (status === 'verified') return { kind: 'verified' }
      if (status === 'verificationRequired') return { kind: 'unverified' }
    }
    return { kind: 'retry', status: response.status }
  }
  if (response.status === 401) return { kind: 'signedOut' }
  return { kind: 'retry', status: response.status }
}
