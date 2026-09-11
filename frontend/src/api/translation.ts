import type { components, operations } from './generated/linguadesk-api.d'
import { getAccountAntiforgeryToken, ANTIFORGERY_HEADER } from './accounts'
import type { TranslationCapabilities, TranslationUsage } from '../translate/translationWorkspace'

type CapabilitiesBody = operations['getCapabilities']['responses']['200']['content']['application/json']
type SubmitBody = operations['submitLanguageOperation']['requestBody']['content']['application/json']
type StatusBody = operations['getLanguageOperationStatus']['responses']['200']['content']['application/json']
type SuccessBody = components['schemas']['TranslationSuccessResponse']
type PendingBody = components['schemas']['OperationPendingResponse']
type ProblemBody = components['schemas']['OperationProblemDetails']
type UsageBody = components['schemas']['UsageSnapshot']

export const OPERATIONS_PATH = '/api/operations' as const
export const USAGE_PATH = '/api/usage' as const
export const CAPABILITIES_PATH = '/api/capabilities' as const

export type TranslationTarget = 'en' | 'ru' | 'ro' | 'zh'

function languageName(language: components['schemas']['LanguageCapability']): [string, string] {
  return [language.id, language.id === 'zh' ? 'Chinese (Simplified output)' : language.name]
}

export function toCapabilities(body: CapabilitiesBody): TranslationCapabilities {
  const languages: Record<string, string> = {}
  for (const language of body.languages) {
    const [id, name] = languageName(language)
    languages[id] = name
  }
  return {
    maximumSourceCharacters: body.translation.maximumSourceCharacters,
    sourceDefault: body.sourceSelection.default,
    sourceValues: [...body.sourceSelection.values],
    languages,
    targetValues: body.languages.map((language) => language.id),
  }
}

export function toUsage(body: UsageBody): TranslationUsage {
  return {
    consumedCharacters: body.consumedCharacters,
    allowanceCharacters: body.allowanceCharacters,
    availableCharacters: body.availableCharacters,
    resetAtUtc: body.resetAtUtc,
    availability: body.availability,
    revision: body.revision,
  }
}

export interface TransportOptions {
  readonly signal?: AbortSignal
}

export type CapabilitiesResult =
  | { readonly kind: 'ok'; readonly capabilities: TranslationCapabilities }
  | { readonly kind: 'retry' }

export async function fetchTranslationCapabilities(
  options: TransportOptions = {},
): Promise<CapabilitiesResult> {
  let response: Response
  try {
    response = await fetch(CAPABILITIES_PATH, { method: 'GET', signal: options.signal })
  } catch {
    return { kind: 'retry' }
  }
  if (response.status !== 200) return { kind: 'retry' }
  try {
    return { kind: 'ok', capabilities: toCapabilities((await response.json()) as CapabilitiesBody) }
  } catch {
    return { kind: 'retry' }
  }
}

export type UsageResult =
  | { readonly kind: 'ok'; readonly usage: TranslationUsage }
  | { readonly kind: 'unauthorized' }
  | { readonly kind: 'retry' }

export async function fetchCurrentUsage(options: TransportOptions = {}): Promise<UsageResult> {
  let response: Response
  try {
    response = await fetch(USAGE_PATH, { method: 'GET', signal: options.signal })
  } catch {
    return { kind: 'retry' }
  }
  if (response.status === 200) {
    try {
      return { kind: 'ok', usage: toUsage((await response.json()) as UsageBody) }
    } catch {
      return { kind: 'retry' }
    }
  }
  if (response.status === 401) return { kind: 'unauthorized' }
  return { kind: 'retry' }
}

async function readProblem(response: Response): Promise<ProblemBody | null> {
  try {
    return (await response.json()) as ProblemBody
  } catch {
    return null
  }
}

export type SubmitTranslationResult =
  | {
      readonly kind: 'succeeded'
      readonly translatedText: string
      readonly characterCount: number
      readonly usage: TranslationUsage
    }
  | { readonly kind: 'pending' }
  | {
      readonly kind: 'problem'
      readonly httpStatus: number
      readonly category: string | null
      readonly reason: string | null
      readonly resetAtUtc: string | null
      readonly characterCount: number | null
      readonly limit: number | null
    }
  | { readonly kind: 'network' }

function toProblem(httpStatus: number, body: ProblemBody | null): SubmitTranslationResult {
  const category =
    typeof body?.category === 'string' && body.category.length > 0 ? body.category : null
  const reason = typeof body?.reason === 'string' && body.reason.length > 0 ? body.reason : null
  const resetAtUtc =
    typeof body?.resetAtUtc === 'string' && body.resetAtUtc.length > 0 ? body.resetAtUtc : null
  const characterCount =
    typeof body?.characterCount === 'number' && Number.isFinite(body.characterCount)
      ? Math.floor(body.characterCount)
      : null
  const limit =
    typeof body?.limit === 'number' && Number.isFinite(body.limit) ? Math.floor(body.limit) : null
  return { kind: 'problem', httpStatus, category, reason, resetAtUtc, characterCount, limit }
}

export interface SubmitTranslationInput {
  readonly operationId: string
  readonly source: string
  readonly sourceSelection: string
  readonly target: string
  readonly antiforgeryToken: string
}

export async function submitTranslationOperation(
  input: SubmitTranslationInput,
  options: TransportOptions = {},
): Promise<SubmitTranslationResult> {
  const payload: SubmitBody = {
    operationId: input.operationId,
    family: 'translation',
    source: input.source,
    sourceSelection: input.sourceSelection,
    target: input.target,
  }
  let response: Response
  try {
    response = await fetch(OPERATIONS_PATH, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        [ANTIFORGERY_HEADER]: input.antiforgeryToken,
      },
      body: JSON.stringify(payload),
      signal: options.signal,
    })
  } catch {
    return { kind: 'network' }
  }
  if (response.status === 201) {
    try {
      const body = (await response.json()) as SuccessBody
      return {
        kind: 'succeeded',
        translatedText: body.translatedText,
        characterCount: body.characterCount,
        usage: toUsage(body.usage),
      }
    } catch {
      return { kind: 'network' }
    }
  }
  if (response.status === 202) {
    try {
      await (response.json() as Promise<PendingBody>)
    } catch {
      /* pending shape is informational only */
    }
    return { kind: 'pending' }
  }
  return toProblem(response.status, await readProblem(response))
}

export type OperationStatusCheck =
  | {
      readonly kind: 'ok'
      readonly status: 'pending' | 'succeeded' | 'failed' | 'interrupted'
      readonly characterCount: number
      readonly admissionDay: string
      readonly usage: TranslationUsage
    }
  | { readonly kind: 'unknownRecord' }
  | { readonly kind: 'windowExpired' }
  | { readonly kind: 'unauthorized' }
  | { readonly kind: 'forbidden' }
  | { readonly kind: 'unavailable' }

/**
 * Read-only status read for the original operation identity (M031 recovery).
 * Never dispatches provider work, charges, or creates claims: a GET against
 * the existing `getLanguageOperationStatus` wire shape only. 404 means no
 * account-owned record exists (nothing is known); 410 means the replay
 * window ended; any other failure preserves the unknown outcome.
 */
export async function fetchOperationStatus(
  operationId: string,
  options: TransportOptions = {},
): Promise<OperationStatusCheck> {
  let response: Response
  try {
    response = await fetch(`${OPERATIONS_PATH}/${encodeURIComponent(operationId)}`, {
      method: 'GET',
      signal: options.signal,
    })
  } catch {
    return { kind: 'unavailable' }
  }
  if (response.status === 404) return { kind: 'unknownRecord' }
  if (response.status === 410) return { kind: 'windowExpired' }
  if (response.status === 401) return { kind: 'unauthorized' }
  if (response.status === 403) return { kind: 'forbidden' }
  if (response.status !== 200) return { kind: 'unavailable' }
  try {
    const body = (await response.json()) as StatusBody
    if (
      body.status !== 'pending' &&
      body.status !== 'succeeded' &&
      body.status !== 'failed' &&
      body.status !== 'interrupted'
    ) {
      return { kind: 'unavailable' }
    }
    return {
      kind: 'ok',
      status: body.status,
      characterCount: body.characterCount,
      admissionDay: body.admissionDay,
      usage: toUsage(body.usage),
    }
  } catch {
    return { kind: 'unavailable' }
  }
}

export type AntiforgeryResult =
  | { readonly kind: 'ok'; readonly requestToken: string }
  | { readonly kind: 'retry' }

export async function fetchSubmitAntiforgeryToken(
  options: TransportOptions = {},
): Promise<AntiforgeryResult> {
  const bootstrap = await getAccountAntiforgeryToken(options)
  if (bootstrap.kind !== 'ok') return { kind: 'retry' }
  return { kind: 'ok', requestToken: bootstrap.requestToken }
}

/** RFC 9562 UUIDv7: 48-bit millisecond timestamp with version and variant bits. */
export function createOperationId(nowMs: number = Date.now()): string {
  const random = new Uint8Array(10)
  crypto.getRandomValues(random)
  const milliseconds = Math.floor(nowMs)
  const timeHigh = Math.floor(milliseconds / 65_536) >>> 0
  const timeLow = milliseconds & 0xffff
  const randomAndVersion = ((random[0] << 8) | random[1]) & 0x0fff | 0x7000
  const clockAndVariant = ((random[2] & 0x3f) | 0x80).toString(16).padStart(2, '0')
  const hex = (value: number, width: number): string =>
    value.toString(16).padStart(width, '0').slice(-width)
  const bytes = (index: number): string => random[index].toString(16).padStart(2, '0')
  return (
    `${hex(timeHigh, 8)}-${hex(timeLow, 4)}-${hex(randomAndVersion, 4)}-` +
    `${clockAndVariant}${bytes(3)}-${bytes(4)}${bytes(5)}${bytes(6)}${bytes(7)}${bytes(8)}${bytes(9)}`
  )
}
