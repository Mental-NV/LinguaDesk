import { alternativesCompatible } from './workspaceModel'
import { submitAlternatives } from '../api/alternatives'
import { spanKey, type SentenceSpan } from '../rewrite/sentences'
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type Dispatch } from 'react'
import * as translation from '../api/translation'
import * as rewriting from '../api/rewriting'
import { analyzeInput } from '../api/inputPolicy'
import {
  createInitialWorkspace, describeReadiness, formatGlobalAllowanceMessage,
  formatResetInstant, formatUserAllowanceMessage, mapSubmitProblem, orderUsage,
  workspaceReducer, MESSAGE_MONETARY_SUSPENDED, currentResult,
  type Feature, type WorkspaceAction, type WorkspaceCapabilities, type WorkspaceState, type WorkspaceUsage,
} from './workspaceModel'
import type { TranslationCapabilities } from '../translate/translationWorkspace'
import type { RewritingCapabilities } from '../rewrite/rewritingWorkspace'

// A tab-local account channel; it contains metadata only. Reset also fences
// reads from the preceding session. No optimistic allowance increments.
export class UsageChannel {
  current: WorkspaceUsage | null = null
  generation = 0
  private listeners = new Set<(usage: WorkspaceUsage | null, ambiguous: boolean) => void>()
  subscribe(listener: (usage: WorkspaceUsage | null, ambiguous: boolean) => void) {
    this.listeners.add(listener)
    return () => { this.listeners.delete(listener) }
  }
  publish(usage: WorkspaceUsage): boolean {
    const order = orderUsage(this.current, usage)
    if (order === 'older') return false
    if (order === 'ambiguous') {
      this.listeners.forEach(listener => listener(null, true))
      return true
    }
    this.current = usage
    this.listeners.forEach(listener => listener(usage, false))
    return false
  }
  reset() { this.current = null; this.generation += 1 }
}

export interface FeatureStore<C extends WorkspaceCapabilities = WorkspaceCapabilities> {
  readonly state: WorkspaceState<C>
  readonly dispatch: Dispatch<WorkspaceAction<C>>
  readonly loadCapabilities: () => void
  readonly requestAlternatives?: (span: SentenceSpan) => void
  readonly submit: () => void
  readonly checkStatus: () => void
  readonly refreshUsage: () => void
  readonly setActive: (active: boolean) => void
  readonly saveScroll: (scrollY: number) => void
  readonly readSavedScroll: () => number
  readonly resetWorkspace: () => void
}
export type TranslateFeatureStore = FeatureStore<TranslationCapabilities>
export type RewriteFeatureStore = FeatureStore<RewritingCapabilities>
interface Candidate { generation: number; input: number; result: number; due: number | null }
interface Flight { controller: AbortController; sent: boolean }

function useFeatureInstance(feature: Feature, sharedChannel?: UsageChannel): FeatureStore {
  const [state, setState] = useState(() => createInitialWorkspace())
  const stateRef = useRef(state)
  const localChannel = useRef<UsageChannel | null>(null)
  if (localChannel.current === null) localChannel.current = new UsageChannel()
  const channel = sharedChannel ?? localChannel.current
  const alive = useRef(true)
  const active = useRef(false)
  const online = useRef(navigator.onLine)
  const candidate = useRef<Candidate | null>(null)
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const flight = useRef<Flight | null>(null)
  const reads = useRef(new Set<AbortController>())
  const capabilityFlight = useRef<AbortController | null>(null)
  const usageFlight = useRef<Promise<boolean> | null>(null)
  const statusFlight = useRef<AbortController | null>(null)
  const scroll = useRef(0)
  const actions = useRef({ attempt: () => {}, arm: () => {} })
  const apply = useCallback((action: WorkspaceAction) => {
    const next = workspaceReducer(stateRef.current, action)
    stateRef.current = next
    if (alive.current) setState(next)
  }, [])
  const cancelCandidate = useCallback(() => {
    if (timer.current !== null) clearTimeout(timer.current)
    timer.current = null
    candidate.current = null
    if (flight.current && !flight.current.sent) {
      flight.current.controller.abort()
      flight.current = null
    }
  }, [])
  const invalidateSession = useCallback((unverified = false) => {
    window.dispatchEvent(new CustomEvent('linguadesk:session-invalid', { detail: unverified ? 'unverified' : 'expired' }))
  }, [])
  const refreshUsageRead = useCallback((): Promise<boolean> => {
    if (usageFlight.current) return usageFlight.current
    const controller = new AbortController()
    reads.current.add(controller)
    const generation = stateRef.current.generation
    const account = channel.generation
    const promise = (async () => {
      try {
        const result = await translation.fetchCurrentUsage({ signal: controller.signal })
        if (!alive.current || controller.signal.aborted || generation !== stateRef.current.generation || account !== channel.generation) return false
        if (result.kind === 'ok') {
          const ambiguous = channel.publish(result.usage)
          return !ambiguous
        }
        if (result.kind === 'unauthorized') invalidateSession()
        apply({ type: 'usageRefreshFailed' })
        return false
      } finally {
        reads.current.delete(controller)
        // A preceding generation may have a newer read already in flight.
        if (generation === stateRef.current.generation) usageFlight.current = null
      }
    })()
    usageFlight.current = promise
    return promise
  }, [apply, channel, invalidateSession])

  const capacityAllows = useCallback(() => {
    const current = stateRef.current
    const usage = current.usage
    if (!usage || current.usageUnavailable || Date.parse(usage.resetAtUtc) <= Date.now()) return false
    const reset = formatResetInstant(usage.resetAtUtc, Date.now())
    const required = analyzeInput(current.source, current.capabilities?.maximumSourceCharacters ?? 5000).scalarCount ?? 0
    let text: string | null = null
    if (usage.availability === 'user-exhausted') text = formatUserAllowanceMessage(reset)
    else if (usage.availability === 'service-exhausted') text = formatGlobalAllowanceMessage(reset)
    else if (usage.availability === 'monetary-suspended') text = MESSAGE_MONETARY_SUSPENDED
    else if (usage.availability !== 'available') text = 'LinguaDesk processing is temporarily unavailable. Your text is safe.'
    else if (required > usage.availableCharacters) text = `This request needs ${required} characters, but you have ${usage.availableCharacters} remaining today.`
    if (text) apply({ type: 'validationFailed', error: { text, canRetry: false, canCheckStatus: false } })
    return text === null
  }, [apply])

  const arm = useCallback(() => {
    const queued = candidate.current
    const current = stateRef.current
    if (!queued || !active.current || !online.current || current.composing || !current.capabilities) return
    if (queued.generation !== current.generation || queued.input !== current.inputRevision || queued.result !== current.resultEditRevision) return
    const valid = describeReadiness({ ...current, phase: 'idle', pendingOperationId: null }, current.capabilities.maximumSourceCharacters, feature).canSubmit
    if (!valid) { cancelCandidate(); return }
    if (!current.usage || current.usageUnavailable || Date.parse(current.usage.resetAtUtc) <= Date.now()) {
      void refreshUsageRead().then(ok => {
        if (candidate.current !== queued) return
        if (ok) actions.current.arm()
        else {
          cancelCandidate()
          apply({ type: 'validationFailed', error: { text: 'We couldn’t check processing availability. Your text is safe. Try again.', canRetry: false, canCheckStatus: false } })
        }
      })
      return
    }
    if (!capacityAllows()) { cancelCandidate(); return }
    if (queued.due === null) queued.due = Date.now() + 1000
    if (queued.due <= Date.now()) { actions.current.attempt(); return }
    if (timer.current !== null) clearTimeout(timer.current)
    timer.current = setTimeout(() => { timer.current = null; actions.current.attempt() }, queued.due - Date.now())
  }, [apply, cancelCandidate, capacityAllows, feature, refreshUsageRead])

  const attempt = useCallback(() => {
    const queued = candidate.current
    const current = stateRef.current
    if (!queued || !active.current || !online.current || current.composing || flight.current || current.pendingOperationId || current.phase !== 'idle') return
    if (queued.due === null || queued.due > Date.now()) { actions.current.arm(); return }
    if (queued.generation !== current.generation || queued.input !== current.inputRevision || queued.result !== current.resultEditRevision) { cancelCandidate(); return }
    if (!current.capabilities || !describeReadiness(current, current.capabilities.maximumSourceCharacters, feature).canSubmit) { cancelCandidate(); return }
    if (!current.usage || current.usageUnavailable || Date.parse(current.usage.resetAtUtc) <= Date.now()) { actions.current.arm(); return }
    if (!capacityAllows()) { cancelCandidate(); return }
    candidate.current = null
    const controller = new AbortController()
    const thisFlight: Flight = { controller, sent: false }
    flight.current = thisFlight
    const account = channel.generation
    const isAlive = () => alive.current && !controller.signal.aborted && current.generation === stateRef.current.generation && account === channel.generation
    void (async () => {
      let deadline: ReturnType<typeof setTimeout> | null = null
      try {
        // Preflight has no provider side effect. Recheck captured revisions
        // and route visibility after it, before creating the operation.
        const bootstrap = await translation.fetchSubmitAntiforgeryToken({ signal: controller.signal })
        if (!isAlive()) return
        const latest = stateRef.current
        if (!active.current || !online.current || latest.composing || latest.inputRevision !== queued.input || latest.resultEditRevision !== queued.result) return
        if (bootstrap.kind !== 'ok') {
          apply({ type: 'validationFailed', error: mapSubmitProblem({ httpStatus: null }, feature) })
          return
        }
        if (!capacityAllows()) return
        const operationId = translation.createOperationId()
        thisFlight.sent = true
        apply({ type: 'submitRequested' })
        const revision = stateRef.current.requestRevision
        // A lost transport response is unknown, including a client deadline;
        // the original key remains blocked until status establishes finality.
        deadline = setTimeout(() => {
          controller.abort()
          if (alive.current && stateRef.current.generation === current.generation) {
            apply({ type: 'submitUnknownOutcome', revision, operationId })
            if (flight.current === thisFlight) flight.current = null
          }
        }, (current.capabilities?.overallDeadlineSeconds ?? 30) * 1000)
        const payload = { operationId, source: latest.source, sourceSelection: latest.sourceSelection, target: latest.target, mode: latest.mode, antiforgeryToken: bootstrap.requestToken }
        const result = feature === 'translation'
          ? await translation.submitTranslationOperation(payload, { signal: controller.signal })
          : await rewriting.submitRewritingOperation(payload, { signal: controller.signal })
        if (!isAlive()) return
        if (result.kind === 'succeeded') {
          apply({ type: 'submitSucceeded', revision, resolvedSourceLanguage: result.resolvedSourceLanguage, association: crypto.randomUUID(), translatedText: 'translatedText' in result ? result.translatedText : undefined, rewrittenText: 'rewrittenText' in result ? result.rewrittenText : undefined, characterCount: result.characterCount, admissionDay: result.admissionDay, usage: result.usage, observedAtMs: Date.now() })
          if (channel.publish(result.usage)) void refreshUsageRead()
        } else if (result.kind === 'network' || result.kind === 'pending') {
          apply({ type: 'submitUnknownOutcome', revision, operationId })
        } else {
          if (result.httpStatus === 401 || result.httpStatus === 403) { invalidateSession(result.httpStatus === 403); return }
          if (result.httpStatus >= 500 && !['processingFailure', 'deadlineExceeded', 'monetarySuspension', 'monetaryIneligible'].includes(result.category ?? '')) {
            apply({ type: 'submitUnknownOutcome', revision, operationId })
            return
          }
          apply({ type: 'submitFailed', revision, error: mapSubmitProblem({ ...result, languageName: current.capabilities?.languages[latest.sourceSelection], resetAtUtc: result.resetAtUtc ? formatResetInstant(result.resetAtUtc, Date.now()) : undefined }, feature) })
          if (result.httpStatus === 429 || result.httpStatus === 503) { cancelCandidate(); void refreshUsageRead() }
        }
      } finally {
        if (deadline !== null) clearTimeout(deadline)
        if (flight.current === thisFlight) flight.current = null
        if (isAlive()) actions.current.arm()
      }
    })()
  }, [apply, cancelCandidate, capacityAllows, channel, feature, invalidateSession, refreshUsageRead])

  useEffect(() => { actions.current = { attempt, arm } }, [attempt, arm])
  const dispatch = useCallback((action: WorkspaceAction) => {
    const before = stateRef.current
    apply(action)
    const after = stateRef.current
    if (action.type === 'resultEdited' || action.type === 'compositionStarted' || action.type === 'resultCompositionStarted') cancelCandidate()
    const changed = after.inputRevision !== before.inputRevision
    if (changed) {
      cancelCandidate()
      if (active.current && online.current && !after.composing) {
        candidate.current = { generation: after.generation, input: after.inputRevision, result: after.resultEditRevision, due: null }
        actions.current.arm()
      }
    }
  }, [apply, cancelCandidate])

  const loadCapabilities = useCallback(() => {
    if (capabilityFlight.current) return
    const controller = new AbortController()
    capabilityFlight.current = controller
    const generation = stateRef.current.generation
    apply({ type: 'capabilitiesRetried' })
    void (async () => {
      const result = feature === 'translation' ? await translation.fetchTranslationCapabilities({ signal: controller.signal }) : await rewriting.fetchRewritingCapabilities({ signal: controller.signal })
      if (!alive.current || controller.signal.aborted || generation !== stateRef.current.generation) return
      if (result.kind === 'ok') {
        apply({ type: 'capabilitiesLoaded', capabilities: result.capabilities })
        if (await refreshUsageRead()) actions.current.arm()
      } else apply({ type: 'capabilitiesFailed' })
      if (capabilityFlight.current === controller) capabilityFlight.current = null
    })()
  }, [apply, feature, refreshUsageRead])

  const checkStatus = useCallback(() => {
    const current = stateRef.current
    const operationId = current.pendingOperationId
    if (!operationId || statusFlight.current) return
    const controller = new AbortController()
    statusFlight.current = controller
    reads.current.add(controller)
    const account = channel.generation
    void (async () => {
      try {
        const result = await translation.fetchOperationStatus(operationId, { signal: controller.signal })
        if (!alive.current || controller.signal.aborted || current.generation !== stateRef.current.generation || account !== channel.generation) return
        if (result.kind === 'unauthorized' || result.kind === 'forbidden') { invalidateSession(result.kind === 'forbidden'); return }
        if (result.kind === 'unavailable') return
        const resolution = result.kind === 'unknownRecord' ? { outcome: 'noRecord' as const }
          : result.kind === 'windowExpired' ? { outcome: 'windowExpired' as const }
          : { outcome: result.status === 'succeeded' ? 'succeededLostOutput' as const : result.status === 'pending' ? 'stillPending' as const : 'terminalFailure' as const, characterCount: result.characterCount, admissionDay: result.admissionDay, usage: result.usage, observedAtMs: Date.now() }
        apply({ type: 'statusResolved', revision: current.requestRevision, operationId, resolution })
        if (result.kind === 'ok' && channel.publish(result.usage)) void refreshUsageRead()
        // Expiry/no record never proves a known terminal outcome. Expiry
        // permits an explicit new candidate, but does not drain a queued one.
        if (result.kind === 'windowExpired' || result.kind === 'unknownRecord') cancelCandidate()
        else actions.current.arm()
      } finally {
        reads.current.delete(controller)
        if (statusFlight.current === controller) statusFlight.current = null
      }
    })()
  }, [apply, cancelCandidate, channel, invalidateSession, refreshUsageRead])

  const requestAlternatives = useCallback((span: SentenceSpan) => {
    const current = stateRef.current
    apply({ type: 'sentenceSelected', span })
    if (feature !== 'rewriting' || !alternativesCompatible(current) || !currentResult(current) || !current.association || !current.comparisonSource || current.editingResult
      || current.alternativeSets[spanKey(span)] || current.phase !== 'idle' || current.pendingOperationId || flight.current || candidate.current
      || !active.current || !online.current || current.composing || Array.from(current.resultText).length > 8000 || span.length > 2000) return
    if (!current.usage || current.usageUnavailable || Date.parse(current.usage.resetAtUtc) <= Date.now()) { void refreshUsageRead(); return }
    if (current.usage.availability !== 'available' || span.length > current.usage.availableCharacters) {
      apply({ type: 'validationFailed', error: { text: 'Sentence alternatives are unavailable with the remaining allowance. Your text is safe.', canRetry: false, canCheckStatus: false } }); return
    }
    const capture = { association: current.association, span, context: current.resultText }
    const controller = new AbortController()
    const thisFlight: Flight = { controller, sent: false }
    flight.current = thisFlight
    const account = channel.generation
    const isAlive = () => alive.current && !controller.signal.aborted && current.generation === stateRef.current.generation && account === channel.generation
    void (async () => {
      let deadline: ReturnType<typeof setTimeout> | null = null
      try {
        const bootstrap = await translation.fetchSubmitAntiforgeryToken({ signal: controller.signal })
        if (!isAlive() || !active.current || !online.current || stateRef.current.association !== capture.association || stateRef.current.composing || stateRef.current.editingResult) return
        if (bootstrap.kind !== 'ok') { apply({ type: 'validationFailed', error: mapSubmitProblem({ httpStatus: null }, feature) }); return }
        const operationId = translation.createOperationId()
        thisFlight.sent = true
        apply({ type: 'alternativesRequested', capture })
        const revision = stateRef.current.requestRevision
        deadline = setTimeout(() => {
          controller.abort()
          if (alive.current && current.generation === stateRef.current.generation) {
            apply({ type: 'submitUnknownOutcome', revision, operationId })
            if (flight.current === thisFlight) flight.current = null
          }
        }, 15000)
        const result = await submitAlternatives({ operationId, context: capture.context, start: span.start, length: span.length,
          association: capture.association, language: current.resolvedSourceLanguage!, mode: current.mode }, bootstrap.requestToken, { signal: controller.signal })
        if (!isAlive()) return
        if (result.kind === 'succeeded') {
          apply({ type: 'alternativesSucceeded', revision, options: result.body.options, association: result.body.association,
            characterCount: result.body.characterCount, admissionDay: result.body.admissionDay, usage: result.usage, observedAtMs: Date.now() })
          if (channel.publish(result.usage)) void refreshUsageRead()
        } else if (result.kind === 'unknown') apply({ type: 'submitUnknownOutcome', revision, operationId })
        else {
          if (result.httpStatus === 401 || result.httpStatus === 403) { invalidateSession(result.httpStatus === 403); return }
          apply({ type: 'alternativesFailed', revision, message: result.category === 'monetarySuspension'
            ? MESSAGE_MONETARY_SUSPENDED : 'We couldn’t load alternatives. Your text is safe.', canRetry: result.httpStatus !== 429 && result.category !== 'monetarySuspension' })
          if (result.httpStatus === 429 || result.httpStatus === 503) void refreshUsageRead()
        }
      } finally {
        if (deadline !== null) clearTimeout(deadline)
        if (flight.current === thisFlight) flight.current = null
        if (isAlive()) actions.current.arm()
      }
    })()
  }, [apply, channel, feature, invalidateSession, refreshUsageRead])

  const submit = useCallback(() => {
    const current = stateRef.current
    if (!current.error?.canRetry || current.pendingOperationId) return
    cancelCandidate()
    candidate.current = { generation: current.generation, input: current.inputRevision, result: current.resultEditRevision, due: Date.now() }
    actions.current.arm()
  }, [cancelCandidate])
  const setActive = useCallback((value: boolean) => {
    active.current = value
    if (!value) {
      cancelCandidate()
      if (stateRef.current.composing) apply({ type: 'resultCompositionEnded' })
    }
  }, [apply, cancelCandidate])
  const resetWorkspace = useCallback(() => {
    cancelCandidate()
    flight.current?.controller.abort()
    flight.current = null
    capabilityFlight.current?.abort()
    capabilityFlight.current = null
    reads.current.forEach(controller => controller.abort())
    reads.current.clear()
    usageFlight.current = null
    statusFlight.current = null
    scroll.current = 0
    apply({ type: 'workspaceCleared' })
  }, [apply, cancelCandidate])
  const saveScroll = useCallback((value: number) => { scroll.current = value }, [])
  const readSavedScroll = useCallback(() => scroll.current, [])
  const refreshUsage = useCallback(() => { void refreshUsageRead() }, [refreshUsageRead])

  useEffect(() => {
    alive.current = true
    const ownedReads = reads.current
    const unsubscribe = channel.subscribe((usage, ambiguous) => {
      if (ambiguous) apply({ type: 'usageRefreshFailed' })
      else if (usage) apply({ type: 'usageUpdated', usage, observedAtMs: Date.now() })
    })
    if (channel.current) apply({ type: 'usageUpdated', usage: channel.current, observedAtMs: Date.now() })
    const offline = () => { online.current = false; cancelCandidate() }
    const onlineHandler = () => { online.current = true; void refreshUsageRead() }
    window.addEventListener('offline', offline)
    window.addEventListener('online', onlineHandler)
    return () => {
      alive.current = false
      unsubscribe()
      cancelCandidate()
      flight.current?.controller.abort()
      capabilityFlight.current?.abort()
      capabilityFlight.current = null
      usageFlight.current = null
      ownedReads.forEach(controller => controller.abort())
      window.removeEventListener('offline', offline)
      window.removeEventListener('online', onlineHandler)
    }
  }, [apply, cancelCandidate, channel, refreshUsageRead])

  return useMemo(() => ({ state, dispatch, loadCapabilities, requestAlternatives, submit, checkStatus, refreshUsage, setActive, saveScroll, readSavedScroll, resetWorkspace }), [state, dispatch, loadCapabilities, requestAlternatives, submit, checkStatus, refreshUsage, setActive, saveScroll, readSavedScroll, resetWorkspace])
}
export function useTranslateFeatureInstance(channel?: UsageChannel) { return useFeatureInstance('translation', channel) as TranslateFeatureStore }
export function useRewriteFeatureInstance(channel?: UsageChannel) { return useFeatureInstance('rewriting', channel) as RewriteFeatureStore }
export interface WorkspaceStores { readonly translate: TranslateFeatureStore; readonly rewrite: RewriteFeatureStore; readonly resetAll: () => void; readonly navigationExpanded?: boolean; readonly toggleNavigation?: () => void }
export const WorkspaceStoreContext = createContext<WorkspaceStores | null>(null)
export function useWorkspaceStores() { return useContext(WorkspaceStoreContext) }
export function useTranslateFeature(): TranslateFeatureStore {
  const context = useWorkspaceStores()
  if (!context) throw new Error('Translation requires the workspace provider.')
  return context.translate
}
export function useRewriteFeature(): RewriteFeatureStore {
  const context = useWorkspaceStores()
  if (!context) throw new Error('Rewriting requires the workspace provider.')
  return context.rewrite
}
