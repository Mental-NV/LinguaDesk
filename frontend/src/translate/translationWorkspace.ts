import {
  analyzeInput,
  validateTranslation,
  type SelectorPolicy,
} from '../api/inputPolicy'

export const TRANSLATION_MAXIMUM_SOURCE_CHARACTERS = 5000 as const

export const MESSAGE_PROCESSING = 'Processing…' as const
export const MESSAGE_UPDATING = 'Updating… Previous result shown.' as const
export const MESSAGE_UP_TO_DATE = 'Up to date' as const
export const MESSAGE_READY = 'Ready to process. Select Translate.' as const
export const MESSAGE_OUTDATED = 'Input or settings changed. Previous result shown. Select Translate to update.' as const
export const MESSAGE_UNCERTAIN_SOURCE = 'Choose the source language to continue.' as const
export const MESSAGE_RESULT_EDITED = 'Edited result — the current update won’t replace your changes.' as const
export const MESSAGE_STALE_SUCCESS = 'This update is no longer current and was not applied.' as const
export const MESSAGE_COPIED = 'Result copied to clipboard.' as const
export const MESSAGE_COPY_FAILED = 'Couldn’t copy the result. Select the text and copy it manually.' as const
export const MESSAGE_SIGN_IN = 'Sign in to continue. Your text was not processed.' as const
export const MESSAGE_VERIFY_EMAIL = 'Verify your email to use Translation and Rewriting.' as const
export const MESSAGE_PROCESSING_FAILURE = 'We couldn’t process this text. Your text and previous result are safe.' as const
export const MESSAGE_DEADLINE = 'Processing took too long. Your text and previous result are safe.' as const
export const MESSAGE_UNSUPPORTED_CONTENT =
  'This text contains too much unsupported or mixed-language content. Use one main language: English, Russian, Romanian, or Chinese.' as const
export const MESSAGE_MONETARY_SUSPENDED =
  'LinguaDesk processing is temporarily unavailable because its service budget has been reached. Your text is safe. Try again after service resumes.' as const
export const MESSAGE_OFFLINE =
  'You’re offline. Keep editing. When you’re back online, select Translate or Rewrite to process your text.' as const

export function formatOversizeMessage(excess: number): string {
  return `Translation is limited to 5,000 characters. Remove ${excess} characters to continue.`
}

export function formatSameLanguageMessage(languageName: string): string {
  return `Choose a target language different from ${languageName}.`
}

export function formatUserAllowanceMessage(reset: string): string {
  return `Your daily allowance is used up. Try again after ${reset}. Your text is safe.`
}

export function formatGlobalAllowanceMessage(reset: string): string {
  return `LinguaDesk’s shared daily allowance is used up. Try again after ${reset}. Your text is safe.`
}

export function formatStaleChargeNotice(characterCount: number): string {
  return `An earlier update completed but was not applied. ${characterCount} characters counted toward your usage.`
}

export interface TranslationCapabilities {
  readonly maximumSourceCharacters: number
  readonly sourceDefault: string
  readonly sourceValues: readonly string[]
  readonly languages: Readonly<Record<string, string>>
  readonly targetValues: readonly string[]
}

export interface TranslationUsage {
  readonly consumedCharacters: number
  readonly allowanceCharacters: number
  readonly availableCharacters: number
  readonly resetAtUtc: string
  readonly availability: string
  readonly revision: number
}

export type TranslationPhase = 'idle' | 'submitting'

export interface TranslationError {
  readonly text: string
  readonly canRetry: boolean
}

export interface TranslationWorkspaceState {
  readonly source: string
  readonly sourceSelection: string
  readonly target: string
  readonly resultText: string
  readonly hasResult: boolean
  readonly resultOutdated: boolean
  readonly resultEdited: boolean
  readonly phase: TranslationPhase
  readonly composing: boolean
  readonly error: TranslationError | null
  readonly notice: string | null
  readonly copyAlert: string | null
  readonly usage: TranslationUsage | null
  readonly usageObservedAtMs: number | null
  readonly requestRevision: number
  readonly appliedRevision: number
  readonly staleChargePending: number | null
  readonly capabilities: TranslationCapabilities | null
  readonly capabilitiesFailed: boolean
}

export type TranslationWorkspaceAction =
  | { readonly type: 'sourceChanged'; readonly source: string }
  | { readonly type: 'sourceSelectionChanged'; readonly value: string }
  | { readonly type: 'targetChanged'; readonly value: string }
  | { readonly type: 'compositionStarted' }
  | { readonly type: 'compositionEnded' }
  | { readonly type: 'capabilitiesLoaded'; readonly capabilities: TranslationCapabilities }
  | { readonly type: 'capabilitiesFailed' }
  | { readonly type: 'capabilitiesRetried' }
  | { readonly type: 'submitRequested' }
  | {
      readonly type: 'submitSucceeded'
      readonly revision: number
      readonly translatedText: string
      readonly characterCount: number
      readonly usage: TranslationUsage
      readonly observedAtMs: number
    }
  | { readonly type: 'submitFailed'; readonly revision: number; readonly error: TranslationError }
  | { readonly type: 'submitAborted'; readonly revision: number }
  | { readonly type: 'resultEdited'; readonly value: string }
  | { readonly type: 'usageUpdated'; readonly usage: TranslationUsage; readonly observedAtMs: number }
  | { readonly type: 'copied'; readonly ok: boolean }
  | { readonly type: 'noticeDismissed' }

export function selectorPolicyFor(capabilities: TranslationCapabilities): SelectorPolicy {
  return {
    sourceDefault: capabilities.sourceDefault,
    sourceValues: capabilities.sourceValues,
    targetValues: capabilities.targetValues,
    modeDefault: 'correctionOnly',
    modeValues: ['correctionOnly'],
  }
}

export function createInitialWorkspace(): TranslationWorkspaceState {
  return {
    source: '',
    sourceSelection: 'auto',
    target: '',
    resultText: '',
    hasResult: false,
    resultOutdated: false,
    resultEdited: false,
    phase: 'idle',
    composing: false,
    error: null,
    notice: null,
    copyAlert: null,
    usage: null,
    usageObservedAtMs: null,
    requestRevision: 0,
    appliedRevision: 0,
    staleChargePending: null,
    capabilities: null,
    capabilitiesFailed: false,
  }
}

export interface TranslationReadiness {
  readonly inputValid: boolean
  readonly selectorsValid: boolean
  readonly canSubmit: boolean
  readonly excess: number
  readonly isSameLanguage: boolean
  readonly languageName: string | null
}

export function describeReadiness(
  state: TranslationWorkspaceState,
  maximumSourceCharacters: number,
): TranslationReadiness {
  const capabilities = state.capabilities
  if (capabilities === null) {
    return {
      inputValid: false,
      selectorsValid: false,
      canSubmit: false,
      excess: 0,
      isSameLanguage: false,
      languageName: null,
    }
  }
  const validation = validateTranslation(
    state.source,
    maximumSourceCharacters,
    state.sourceSelection,
    state.target === '' ? undefined : state.target,
    selectorPolicyFor(capabilities),
  )
  const languageName =
    state.sourceSelection === capabilities.sourceDefault
      ? null
      : (capabilities.languages[state.sourceSelection] ?? state.sourceSelection)
  const isSameLanguage =
    validation.isSourceSelectionValid &&
    validation.isTargetValid &&
    !validation.isDistinctSelection
  return {
    inputValid: validation.input.isValid,
    selectorsValid:
      validation.isSourceSelectionValid && validation.isTargetValid && validation.isDistinctSelection,
    canSubmit:
      state.phase === 'idle' &&
      !state.composing &&
      validation.isValid,
    excess: validation.input.excess,
    isSameLanguage,
    languageName,
  }
}

export function selectInlineValidation(
  state: TranslationWorkspaceState,
  readiness: TranslationReadiness,
): string | null {
  if (state.capabilities === null) return null
  const input = analyzeInput(state.source, state.capabilities.maximumSourceCharacters)
  if (input.isOversized) return formatOversizeMessage(input.excess)
  if (readiness.isSameLanguage && readiness.languageName !== null) {
    return formatSameLanguageMessage(readiness.languageName)
  }
  return null
}

export function selectStatusLine(state: TranslationWorkspaceState): string | null {
  if (state.phase === 'submitting') {
    return state.hasResult ? MESSAGE_UPDATING : MESSAGE_PROCESSING
  }
  if (state.error !== null) return null
  if (state.resultOutdated && state.hasResult) return MESSAGE_OUTDATED
  if (state.hasResult) return MESSAGE_UP_TO_DATE
  if (state.capabilities !== null) return MESSAGE_READY
  return null
}

export interface SubmitProblemInput {
  readonly httpStatus: number | null
  readonly category?: unknown
  readonly reason?: unknown
  readonly resetAtUtc?: unknown
  readonly characterCount?: unknown
  readonly limit?: unknown
  readonly languageName?: string
}

function asText(value: unknown): string | null {
  return typeof value === 'string' && value.length > 0 ? value : null
}

function asCount(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) && value >= 0
    ? Math.floor(value)
    : null
}

export function mapSubmitProblem(input: SubmitProblemInput): TranslationError {
  const category = asText(input.category)
  const reason = asText(input.reason)
  if (input.httpStatus === 401) return { text: MESSAGE_SIGN_IN, canRetry: false }
  if (input.httpStatus === 403) return { text: MESSAGE_VERIFY_EMAIL, canRetry: false }
  if (input.httpStatus === 429) {
    const reset = asText(input.resetAtUtc) ?? 'the next reset'
    return {
      text:
        category === 'globalAllowance'
          ? formatGlobalAllowanceMessage(reset)
          : formatUserAllowanceMessage(reset),
      canRetry: false,
    }
  }
  if (input.httpStatus === 422) {
    if (reason === 'uncertain') return { text: MESSAGE_UNCERTAIN_SOURCE, canRetry: false }
    if (reason === 'oversizedSource') {
      const count = asCount(input.characterCount)
      const limit = asCount(input.limit)
      const excess = count !== null && limit !== null ? Math.max(0, count - limit) : 0
      return { text: formatOversizeMessage(excess), canRetry: false }
    }
    if (reason === 'sameLanguage') {
      return {
        text: formatSameLanguageMessage(input.languageName ?? 'the source language'),
        canRetry: false,
      }
    }
    return { text: MESSAGE_UNSUPPORTED_CONTENT, canRetry: false }
  }
  if (input.httpStatus === 503 && category === 'monetarySuspension') {
    return { text: MESSAGE_MONETARY_SUSPENDED, canRetry: false }
  }
  if (input.httpStatus === 504) return { text: MESSAGE_DEADLINE, canRetry: true }
  return { text: MESSAGE_PROCESSING_FAILURE, canRetry: true }
}

export function translationWorkspaceReducer(
  state: TranslationWorkspaceState,
  action: TranslationWorkspaceAction,
): TranslationWorkspaceState {
  switch (action.type) {
    case 'sourceChanged':
      return {
        ...state,
        source: action.source,
        resultOutdated: state.hasResult ? true : state.resultOutdated,
        error: null,
      }
    case 'sourceSelectionChanged':
      return {
        ...state,
        sourceSelection: action.value,
        resultOutdated: state.hasResult ? true : state.resultOutdated,
        error: null,
      }
    case 'targetChanged':
      return {
        ...state,
        target: action.value,
        resultOutdated: state.hasResult ? true : state.resultOutdated,
        error: null,
      }
    case 'compositionStarted':
      return { ...state, composing: true }
    case 'compositionEnded':
      return { ...state, composing: false }
    case 'capabilitiesLoaded':
      return { ...state, capabilities: action.capabilities, capabilitiesFailed: false }
    case 'capabilitiesFailed':
      return { ...state, capabilitiesFailed: true }
    case 'capabilitiesRetried':
      return { ...state, capabilitiesFailed: false }
    case 'submitRequested':
      if (state.phase !== 'idle' || state.composing || state.capabilities === null) return state
      return {
        ...state,
        phase: 'submitting',
        requestRevision: state.requestRevision + 1,
        // A later explicit submission may replace preexisting manual edits; only
        // edits made after this activation are protected from its response.
        resultEdited: false,
        error: null,
        notice: null,
        copyAlert: null,
      }
    case 'submitSucceeded': {
      if (action.revision !== state.requestRevision) {
        return {
          ...state,
          staleChargePending: action.characterCount,
          notice: MESSAGE_STALE_SUCCESS,
        }
      }
      if (state.resultEdited) {
        return {
          ...state,
          phase: 'idle',
          appliedRevision: action.revision,
          usage: action.usage,
          usageObservedAtMs: action.observedAtMs,
          notice:
            state.staleChargePending !== null
              ? formatStaleChargeNotice(state.staleChargePending)
              : MESSAGE_RESULT_EDITED,
          staleChargePending: null,
        }
      }
      return {
        ...state,
        phase: 'idle',
        resultText: action.translatedText,
        hasResult: true,
        resultOutdated: false,
        resultEdited: false,
        appliedRevision: action.revision,
        usage: action.usage,
        usageObservedAtMs: action.observedAtMs,
        error: null,
        notice:
          state.staleChargePending !== null
            ? formatStaleChargeNotice(state.staleChargePending)
            : null,
        staleChargePending: null,
      }
    }
    case 'submitFailed':
      if (action.revision !== state.requestRevision) return state
      return { ...state, phase: 'idle', error: action.error }
    case 'submitAborted':
      // Sign-out invalidates pending flights (M013 teardown): release the
      // busy phase without touching text. Navigation never dispatches this.
      if (action.revision !== state.requestRevision) return state
      return { ...state, phase: 'idle' }
    case 'resultEdited':
      return { ...state, resultText: action.value, resultEdited: true, copyAlert: null }
    case 'usageUpdated':
      return { ...state, usage: action.usage, usageObservedAtMs: action.observedAtMs }
    case 'copied':
      return {
        ...state,
        notice: action.ok ? MESSAGE_COPIED : null,
        copyAlert: action.ok ? null : MESSAGE_COPY_FAILED,
      }
    case 'noticeDismissed':
      return { ...state, notice: null, copyAlert: null }
  }
}

export function formatCount(value: number): string {
  return value.toLocaleString('en-US')
}

export function formatResetInstant(resetAtUtc: string, nowMs: number): string {
  const resetMs = Date.parse(resetAtUtc)
  if (Number.isNaN(resetMs)) return 'the next reset'
  const reset = new Date(resetMs)
  const absolute = `${String(reset.getUTCHours()).padStart(2, '0')}:${String(reset.getUTCMinutes()).padStart(2, '0')} UTC`
  const diffMinutes = Math.max(0, Math.round((resetMs - nowMs) / 60_000))
  if (diffMinutes < 1) return absolute
  if (diffMinutes < 60) return `${absolute} (in ${diffMinutes} minute${diffMinutes === 1 ? '' : 's'})`
  const hours = Math.floor(diffMinutes / 60)
  const minutes = diffMinutes % 60
  const relative =
    minutes === 0
      ? `in ${hours} hour${hours === 1 ? '' : 's'}`
      : `in ${hours} hour${hours === 1 ? '' : 's'} ${minutes} minute${minutes === 1 ? '' : 's'}`
  return `${absolute} (${relative})`
}

export function formatUsageLine(usage: TranslationUsage, nowMs: number): string {
  const reset = formatResetInstant(usage.resetAtUtc, nowMs)
  return `${formatCount(usage.consumedCharacters)} of ${formatCount(usage.allowanceCharacters)} characters used · ${formatCount(usage.availableCharacters)} remaining · Resets ${reset}`
}
