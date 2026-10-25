import { analyzeInput, validateRewriting, validateTranslation } from '../api/inputPolicy'

export type Feature = 'translation' | 'rewriting'
export interface WorkspaceCapabilities {
  readonly maximumSourceCharacters: number
  readonly overallDeadlineSeconds?: number
  readonly sourceDefault: string
  readonly sourceValues: readonly string[]
  readonly languages: Readonly<Record<string, string>>
  readonly targetValues?: readonly string[]
  readonly modeDefault?: string
  readonly modeValues?: readonly string[]
  readonly modeNames?: Readonly<Record<string, string>>
}
export interface WorkspaceUsage {
  readonly consumedCharacters: number
  readonly allowanceCharacters: number
  readonly availableCharacters: number
  readonly resetAtUtc: string
  readonly availability: string
  readonly revision: number
}
export interface WorkspaceError {
  readonly text: string
  readonly canRetry: boolean
  readonly canCheckStatus: boolean
}
export interface WorkspaceState<C extends WorkspaceCapabilities = WorkspaceCapabilities> {
  readonly source: string
  readonly sourceSelection: string
  readonly target: string
  readonly mode: string
  readonly resultText: string
  readonly hasResult: boolean
  readonly resultOutdated: boolean
  readonly resultEdited: boolean
  readonly phase: 'idle' | 'submitting'
  readonly composing: boolean
  readonly error: WorkspaceError | null
  readonly notice: string | null
  readonly usage: WorkspaceUsage | null
  readonly usageObservedAtMs: number | null
  readonly usageUnavailable: boolean
  readonly generation: number
  readonly inputRevision: number
  readonly resultEditRevision: number
  readonly submittedInputRevision: number
  readonly submittedResultEditRevision: number
  readonly requestRevision: number
  readonly appliedRevision: number
  readonly pendingOperationId: string | null
  readonly capabilities: C | null
  readonly capabilitiesFailed: boolean
}
export type StatusResolution =
  | { readonly outcome: 'succeededLostOutput'; readonly characterCount: number; readonly admissionDay?: string; readonly usage: WorkspaceUsage; readonly observedAtMs: number }
  | { readonly outcome: 'terminalFailure' | 'stillPending'; readonly usage: WorkspaceUsage; readonly observedAtMs: number }
  | { readonly outcome: 'noRecord' | 'windowExpired' }
  | { readonly outcome: 'auth'; readonly error: WorkspaceError }
export type WorkspaceAction<C extends WorkspaceCapabilities = WorkspaceCapabilities> =
  | { readonly type: 'sourceChanged'; readonly source: string }
  | { readonly type: 'sourceSelectionChanged' | 'targetChanged' | 'modeChanged'; readonly value: string }
  | { readonly type: 'compositionStarted' | 'compositionEnded' | 'resultCompositionStarted' | 'resultCompositionEnded' | 'submitRequested' | 'capabilitiesFailed' | 'capabilitiesRetried' | 'workspaceCleared' | 'usageRefreshFailed' | 'noticeDismissed' }
  | { readonly type: 'capabilitiesLoaded'; readonly capabilities: C }
  | { readonly type: 'submitSucceeded'; readonly revision: number; readonly translatedText?: string; readonly rewrittenText?: string; readonly characterCount: number; readonly admissionDay?: string; readonly usage: WorkspaceUsage; readonly observedAtMs: number }
  | { readonly type: 'validationFailed'; readonly error: WorkspaceError | null }
  | { readonly type: 'submitFailed'; readonly revision: number; readonly error: WorkspaceError }
  | { readonly type: 'submitUnknownOutcome'; readonly revision: number; readonly operationId: string }
  | { readonly type: 'statusResolved'; readonly revision: number; readonly operationId: string; readonly resolution: StatusResolution }
  | { readonly type: 'submitAborted'; readonly revision: number }
  | { readonly type: 'resultEdited'; readonly value: string }
  | { readonly type: 'usageUpdated'; readonly usage: WorkspaceUsage; readonly observedAtMs: number }

export const MESSAGE_UNCERTAIN_SOURCE = 'Choose the source language to continue.'
export const MESSAGE_PROCESSING_FAILURE = 'We couldn’t process this text. Your text and previous result are safe.'
export const MESSAGE_DEADLINE = 'Processing took too long. Your text and previous result are safe.'
export const MESSAGE_UNSUPPORTED_CONTENT = 'This text contains too much unsupported or mixed-language content. Use one main language: English, Russian, Romanian, or Chinese.'
export const MESSAGE_MONETARY_SUSPENDED = 'LinguaDesk processing is temporarily unavailable because its service budget has been reached. Your text is safe. Try again after service resumes.'
export const MESSAGE_OFFLINE = 'You’re offline. Keep editing. When you’re back online, edit your text or settings to process it.'
export const MESSAGE_UNKNOWN_OUTCOME = 'We couldn’t confirm whether this request completed. Your text is safe. Check its status before trying again.'
export const MESSAGE_STATUS_NO_RECORD = 'We found no record of this request, so its outcome is unknown. Your text is safe. Check its status before trying again.'
export const MESSAGE_STATUS_WINDOW_EXPIRED = 'The status window for this request has expired, so its outcome is unknown. Your text is safe. Edit your text or settings to start a new request.'
export const MESSAGE_SIGN_IN = 'Sign in to continue. Your text was not processed.'
export const MESSAGE_VERIFY_EMAIL = 'Verify your email to use Translation and Rewriting.'

export function createInitialWorkspace<C extends WorkspaceCapabilities>(): WorkspaceState<C> {
  return { source: '', sourceSelection: 'auto', target: '', mode: 'correctionOnly', resultText: '', hasResult: false, resultOutdated: false, resultEdited: false, phase: 'idle', composing: false, error: null, notice: null, usage: null, usageObservedAtMs: null, usageUnavailable: false, generation: 0, inputRevision: 0, resultEditRevision: 0, submittedInputRevision: -1, submittedResultEditRevision: -1, requestRevision: 0, appliedRevision: 0, pendingOperationId: null, capabilities: null, capabilitiesFailed: false }
}
export function selectorPolicyFor(capabilities: WorkspaceCapabilities) {
  return { sourceDefault: capabilities.sourceDefault, sourceValues: capabilities.sourceValues, targetValues: capabilities.targetValues ?? [], modeDefault: capabilities.modeDefault ?? 'correctionOnly', modeValues: capabilities.modeValues ?? ['correctionOnly'] }
}
export function describeReadiness(state: WorkspaceState, limit: number, feature: Feature) {
  const cap = state.capabilities
  if (cap === null) return { inputValid: false, selectorsValid: false, canSubmit: false, excess: 0, isSameLanguage: false, languageName: null }
  const validation = feature === 'translation'
    ? validateTranslation(state.source, limit, state.sourceSelection, state.target || undefined, selectorPolicyFor(cap))
    : validateRewriting(state.source, limit, state.sourceSelection, state.mode, selectorPolicyFor(cap))
  const sameLanguage = feature === 'translation' && state.sourceSelection !== cap.sourceDefault && state.sourceSelection === state.target
  return { inputValid: validation.input.isValid, selectorsValid: validation.isValid || (!validation.input.isValid && (feature === 'translation' ? cap.targetValues?.includes(state.target) && !sameLanguage : cap.modeValues?.includes(state.mode))) === true, canSubmit: state.phase === 'idle' && !state.composing && state.pendingOperationId === null && validation.isValid, excess: validation.input.excess, isSameLanguage: sameLanguage, languageName: sameLanguage ? cap.languages[state.sourceSelection] : null }
}
export function formatOversizeMessage(feature: Feature) {
  return feature === 'translation' ? 'Translation is limited to 5,000 characters. Shorten the text to continue.' : 'Rewriting is limited to 2,000 characters. Shorten the text to continue.'
}
export function formatSameLanguageMessage(language: string) { return `Choose a target language different from ${language}.` }
export function selectInlineValidation(state: WorkspaceState, feature: Feature): string | null {
  if (!state.capabilities) return null
  if (analyzeInput(state.source, state.capabilities.maximumSourceCharacters).isOversized) return formatOversizeMessage(feature)
  const ready = describeReadiness(state, state.capabilities.maximumSourceCharacters, feature)
  return ready.isSameLanguage && ready.languageName ? formatSameLanguageMessage(ready.languageName) : null
}
export function formatUserAllowanceMessage(reset: string) { return `Your daily allowance is used up. Try again after ${reset}. Your text is safe.` }
export function formatGlobalAllowanceMessage(reset: string) { return `LinguaDesk’s shared daily allowance is used up. Try again after ${reset}. Your text is safe.` }
export function formatStaleChargeNotice(count: number, admissionDay?: string, currentDay?: string) {
  return `An earlier update completed but was not applied. ${count} characters counted toward your usage.${admissionDay && admissionDay !== currentDay ? ` Admission period: ${admissionDay} (UTC).` : ''}`
}
export function formatLostOutputNotice(count: number, admissionDay?: string) {
  return `The request completed and ${count} characters were counted toward your usage, but the result text is no longer available.${admissionDay ? ` Admission period: ${admissionDay} (UTC).` : ''} Edit your text or settings to generate a new result.`
}
export function formatResetInstant(resetAtUtc: string, nowMs: number): string {
  const resetMs = Date.parse(resetAtUtc)
  if (!Number.isFinite(resetMs)) return 'the next reset'
  const reset = new Date(resetMs)
  const absolute = `${String(reset.getUTCHours()).padStart(2, '0')}:${String(reset.getUTCMinutes()).padStart(2, '0')} UTC`
  const minutes = Math.max(0, Math.ceil((resetMs - nowMs) / 60000))
  return minutes ? `${absolute} (in ${minutes} minute${minutes === 1 ? '' : 's'})` : absolute
}
export interface SubmitProblemInput { readonly httpStatus: number | null; readonly category?: unknown; readonly reason?: unknown; readonly resetAtUtc?: unknown; readonly characterCount?: unknown; readonly limit?: unknown; readonly languageName?: string }
export function mapSubmitProblem(input: SubmitProblemInput, feature: Feature): WorkspaceError {
  let text = MESSAGE_PROCESSING_FAILURE, canRetry = false
  if (input.httpStatus === 401) text = MESSAGE_SIGN_IN
  else if (input.httpStatus === 403) text = MESSAGE_VERIFY_EMAIL
  else if (input.httpStatus === 429) text = input.category === 'globalAllowance' ? formatGlobalAllowanceMessage(String(input.resetAtUtc ?? 'the next reset')) : formatUserAllowanceMessage(String(input.resetAtUtc ?? 'the next reset'))
  else if (input.httpStatus === 422) text = (input.reason === 'uncertain' || input.reason === 'sourceMismatch') ? MESSAGE_UNCERTAIN_SOURCE : input.reason === 'oversizedSource' ? formatOversizeMessage(feature) : input.reason === 'sameLanguage' ? formatSameLanguageMessage(input.languageName ?? 'the source language') : MESSAGE_UNSUPPORTED_CONTENT
  else if (input.httpStatus === 503 && input.category === 'monetarySuspension') text = MESSAGE_MONETARY_SUSPENDED
  else { text = input.httpStatus === 504 ? MESSAGE_DEADLINE : MESSAGE_PROCESSING_FAILURE; canRetry = true }
  return { text, canRetry, canCheckStatus: false }
}
// Compare the UTC period before its durable revision. Equal revisions may not
// change values: an inconsistent snapshot requires a fresh read, never guessing.
export function orderUsage(current: WorkspaceUsage | null, next: WorkspaceUsage): 'newer' | 'older' | 'same' | 'ambiguous' {
  if (!Number.isSafeInteger(next.revision) || !Number.isFinite(Date.parse(next.resetAtUtc))) return 'ambiguous'
  if (!current) return 'newer'
  const period = Date.parse(next.resetAtUtc) - Date.parse(current.resetAtUtc)
  if (period) return period > 0 ? 'newer' : 'older'
  if (next.revision !== current.revision) return next.revision > current.revision ? 'newer' : 'older'
  return next.consumedCharacters === current.consumedCharacters && next.availableCharacters === current.availableCharacters && next.allowanceCharacters === current.allowanceCharacters && next.availability === current.availability ? 'same' : 'ambiguous'
}
function withUsage<C extends WorkspaceCapabilities>(state: WorkspaceState<C>, usage: WorkspaceUsage, at: number): WorkspaceState<C> {
  const order = orderUsage(state.usage, usage)
  if (order === 'older') return state
  if (order === 'ambiguous') return { ...state, usageUnavailable: true }
  return { ...state, usage, usageObservedAtMs: at, usageUnavailable: false }
}
function currentText(state: WorkspaceState) { return state.inputRevision === state.submittedInputRevision && state.resultEditRevision === state.submittedResultEditRevision }
export function workspaceReducer<C extends WorkspaceCapabilities>(state: WorkspaceState<C>, action: WorkspaceAction<C>): WorkspaceState<C> {
  switch (action.type) {
    case 'sourceChanged':
    case 'sourceSelectionChanged':
    case 'targetChanged':
    case 'modeChanged': {
      const key = action.type === 'sourceChanged' ? 'source' : action.type === 'sourceSelectionChanged' ? 'sourceSelection' : action.type === 'targetChanged' ? 'target' : 'mode'
      const value = action.type === 'sourceChanged' ? action.source : action.value
      if (state[key] === value) return state
      return { ...state, [key]: value, inputRevision: state.inputRevision + 1, resultOutdated: state.hasResult, error: state.pendingOperationId ? state.error : null, notice: null }
    }
    case 'compositionStarted': return { ...state, composing: true, resultEditRevision: state.resultEditRevision + 1 }
    case 'compositionEnded': return { ...state, composing: false, inputRevision: state.inputRevision + 1 }
    case 'resultCompositionStarted': return { ...state, composing: true, resultEditRevision: state.resultEditRevision + 1 }
    case 'resultCompositionEnded': return { ...state, composing: false }
    case 'capabilitiesLoaded': return { ...state, capabilities: action.capabilities, capabilitiesFailed: false }
    case 'capabilitiesFailed': return { ...state, capabilitiesFailed: true }
    case 'capabilitiesRetried': return { ...state, capabilitiesFailed: false }
    case 'submitRequested':
      if (state.phase !== 'idle' || state.composing || !state.capabilities || state.pendingOperationId) return state
      return { ...state, phase: 'submitting', requestRevision: state.requestRevision + 1, submittedInputRevision: state.inputRevision, submittedResultEditRevision: state.resultEditRevision, resultEdited: false, error: null, notice: null }
    case 'submitSucceeded': {
      if (action.revision !== state.requestRevision) return state
      const next = withUsage(state, action.usage, action.observedAtMs)
      if (!currentText(state)) return { ...next, phase: 'idle', notice: formatStaleChargeNotice(action.characterCount, action.admissionDay, new Date(Date.parse(action.usage.resetAtUtc) - 86400000).toISOString().slice(0, 10)) }
      return { ...next, phase: 'idle', resultText: action.translatedText ?? action.rewrittenText ?? '', hasResult: true, resultOutdated: false, resultEdited: false, appliedRevision: action.revision, error: null, notice: null }
    }
    case 'validationFailed': return { ...state, error: state.pendingOperationId ? state.error : action.error }
    case 'submitFailed':
      if (action.revision !== state.requestRevision) return state
      return { ...state, phase: 'idle', error: currentText(state) ? action.error : state.error }
    case 'submitUnknownOutcome':
      if (action.revision !== state.requestRevision) return state
      return { ...state, phase: 'idle', error: { text: MESSAGE_UNKNOWN_OUTCOME, canRetry: false, canCheckStatus: true }, pendingOperationId: action.operationId }
    case 'statusResolved': {
      if (action.revision !== state.requestRevision || action.operationId !== state.pendingOperationId) return state
      const result = action.resolution
      if (result.outcome === 'stillPending') return withUsage(state, result.usage, result.observedAtMs)
      if (result.outcome === 'noRecord') return { ...state, error: { text: MESSAGE_STATUS_NO_RECORD, canRetry: false, canCheckStatus: true } }
      if (result.outcome === 'windowExpired') return { ...state, pendingOperationId: null, error: { text: MESSAGE_STATUS_WINDOW_EXPIRED, canRetry: true, canCheckStatus: false } }
      if (result.outcome === 'auth') return { ...state, pendingOperationId: null, error: result.error }
      if (result.outcome === 'succeededLostOutput') return { ...withUsage(state, result.usage, result.observedAtMs), pendingOperationId: null, error: null, notice: formatLostOutputNotice(result.characterCount, result.admissionDay) }
      if (result.outcome === 'terminalFailure') return { ...withUsage(state, result.usage, result.observedAtMs), pendingOperationId: null, error: currentText(state) ? { text: MESSAGE_PROCESSING_FAILURE, canRetry: true, canCheckStatus: false } : null }
      return state
    }
    case 'submitAborted': return action.revision === state.requestRevision ? { ...state, phase: 'idle' } : state
    case 'workspaceCleared': return { ...createInitialWorkspace<C>(), capabilities: state.capabilities, generation: state.generation + 1, requestRevision: state.requestRevision + 1 }
    case 'resultEdited': return { ...state, resultText: action.value, resultEdited: true, resultEditRevision: state.resultEditRevision + 1 }
    case 'usageUpdated': return withUsage(state, action.usage, action.observedAtMs)
    case 'usageRefreshFailed': return { ...state, usageUnavailable: true }
    case 'noticeDismissed': return { ...state, notice: null }
  }
}
