import type { components, operations } from './generated/linguadesk-api.d'
import { getAccountAntiforgeryToken, ANTIFORGERY_HEADER } from './accounts'
import type { RewritingCapabilities, RewritingUsage } from '../rewrite/rewritingWorkspace'

type CapabilitiesBody = operations['getCapabilities']['responses']['200']['content']['application/json']
type SubmitBody = operations['submitLanguageOperation']['requestBody']['content']['application/json']
type SuccessBody = components['schemas']['RewritingSuccessResponse']
type PendingBody = components['schemas']['OperationPendingResponse']
type ProblemBody = components['schemas']['OperationProblemDetails']
type UsageBody = components['schemas']['UsageSnapshot']

export const OPERATIONS_PATH = '/api/operations' as const
export const USAGE_PATH = '/api/usage' as const
export const CAPABILITIES_PATH = '/api/capabilities' as const

function languageName(language: components['schemas']['LanguageCapability']): [string, string] {
  return [language.id, language.id === 'zh' ? 'Chinese (Simplified output)' : language.name]
}

export function toCapabilities(body: CapabilitiesBody): RewritingCapabilities {
  const languages: Record<string, string> = {}
  for (const language of body.languages) {
    const [id, name] = languageName(language)
    languages[id] = name
  }
  const modeNames: Record<string, string> = {}
  for (const mode of body.rewriting.modes) {
    modeNames[mode.id] = mode.name
  }
  return {
    maximumSourceCharacters: body.rewriting.maximumSourceCharacters,
    sourceDefault: body.sourceSelection.default,
    sourceValues: [...body.sourceSelection.values],
    languages,
    modeDefault: body.rewriting.defaultMode,
    modeValues: body.rewriting.modes.map((mode) => mode.id),
    modeNames,
  }
}

export function toUsage(body: UsageBody): RewritingUsage {
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
  | { readonly kind: 'ok'; readonly capabilities: RewritingCapabilities }
  | { readonly kind: 'retry' }

export async function fetchRewritingCapabilities(
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
  | { readonly kind: 'ok'; readonly usage: RewritingUsage }
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

export type SubmitRewritingResult =
  | {
      readonly kind: 'succeeded'
      readonly rewrittenText: string
      readonly characterCount: number
      readonly usage: RewritingUsage
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

function toProblem(httpStatus: number, body: ProblemBody | null): SubmitRewritingResult {
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

export interface SubmitRewritingInput {
  readonly operationId: string
  readonly source: string
  readonly sourceSelection: string
  readonly mode: string
  readonly antiforgeryToken: string
}

export async function submitRewritingOperation(
  input: SubmitRewritingInput,
  options: TransportOptions = {},
): Promise<SubmitRewritingResult> {
  const payload: SubmitBody = {
    operationId: input.operationId,
    family: 'rewriting',
    source: input.source,
    sourceSelection: input.sourceSelection,
    mode: input.mode,
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
        rewrittenText: body.rewrittenText,
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
