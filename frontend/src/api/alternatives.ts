import type { components, operations } from './generated/linguadesk-api.d'
import { ANTIFORGERY_HEADER } from './accounts'
import { toUsage, type TransportOptions } from './rewriting'

type RequestBody = operations['submitSentenceAlternatives']['requestBody']['content']['application/json']
type SuccessBody = components['schemas']['AlternativesSuccessResponse']
export type AlternativesResult = { readonly kind: 'succeeded'; readonly body: SuccessBody; readonly usage: ReturnType<typeof toUsage> }
  | { readonly kind: 'unknown' }
  | { readonly kind: 'problem'; readonly httpStatus: number; readonly category: string | null }
export async function submitAlternatives(payload: RequestBody, antiforgeryToken: string, options: TransportOptions = {}): Promise<AlternativesResult> {
  try {
    const response = await fetch('/api/rewriting/alternatives', { method: 'POST', headers: { 'Content-Type': 'application/json', [ANTIFORGERY_HEADER]: antiforgeryToken }, body: JSON.stringify(payload), signal: options.signal })
    if (response.status === 201) {
      const body = await response.json() as SuccessBody
      if (body.operationId !== payload.operationId || body.association !== payload.association || body.start !== payload.start || body.length !== payload.length
        || body.options.length !== 2 || body.options.some(option => typeof option !== 'string' || !option.trim()) || body.options[0] === body.options[1]
        || body.resolvedSourceLanguage !== payload.language || body.mode !== (payload.mode ?? 'correctionOnly')) return { kind: 'unknown' }
      return { kind: 'succeeded', body, usage: toUsage(body.usage) }
    }
    if (response.status === 200 || response.status === 202) return { kind: 'unknown' }
    const problem = await response.json() as components['schemas']['OperationProblemDetails']
    if (response.status >= 500 && !['processingFailure', 'deadlineExceeded', 'monetarySuspension', 'monetaryIneligible'].includes(problem.category)) return { kind: 'unknown' }
    return { kind: 'problem', httpStatus: response.status, category: problem.category }
  } catch { return { kind: 'unknown' } }
}
