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

export interface RegistrationCallOptions {
  readonly signal?: AbortSignal
}

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
