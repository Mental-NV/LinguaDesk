import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useReducer,
  useRef,
  type Dispatch,
} from 'react'
import {
  createOperationId as createTranslationOperationId,
  fetchCurrentUsage as fetchTranslationUsage,
  fetchOperationStatus as fetchTranslationOperationStatus,
  fetchSubmitAntiforgeryToken as fetchTranslationAntiforgeryToken,
  fetchTranslationCapabilities,
  submitTranslationOperation,
} from '../api/translation'
import {
  createOperationId as createRewritingOperationId,
  fetchCurrentUsage as fetchRewritingUsage,
  fetchOperationStatus as fetchRewritingOperationStatus,
  fetchSubmitAntiforgeryToken as fetchRewritingAntiforgeryToken,
  fetchRewritingCapabilities,
  submitRewritingOperation,
} from '../api/rewriting'
import {
  MESSAGE_COPIED as TRANSLATION_MESSAGE_COPIED,
  createInitialWorkspace as createInitialTranslationWorkspace,
  describeReadiness as describeTranslationReadiness,
  formatResetInstant as formatTranslationResetInstant,
  mapSubmitProblem as mapTranslationSubmitProblem,
  translationWorkspaceReducer,
  type TranslationWorkspaceAction,
  type TranslationWorkspaceState,
} from '../translate/translationWorkspace'
import {
  MESSAGE_COPIED as REWRITING_MESSAGE_COPIED,
  createInitialWorkspace as createInitialRewritingWorkspace,
  describeReadiness as describeRewritingReadiness,
  formatResetInstant as formatRewritingResetInstant,
  mapSubmitProblem as mapRewritingSubmitProblem,
  rewritingWorkspaceReducer,
  type RewritingWorkspaceAction,
  type RewritingWorkspaceState,
} from '../rewrite/rewritingWorkspace'

/**
 * M030 lifted workspace store.
 *
 * Each feature keeps one reducer state above the router so mode-link and
 * Back/Forward navigation preserves in-memory text/settings/result without
 * submitting. Submit flights are owned here too: a pending operation keeps
 * its own AbortController across navigation and settles into its owning
 * entry by captured revision, so a hidden page's result never touches the
 * visible page. Reducers own the guards; this store owns transport.
 * Reducer semantics are unchanged from the per-page M028/M029 behavior.
 */

export interface TranslateFeatureStore {
  readonly state: TranslationWorkspaceState
  readonly dispatch: Dispatch<TranslationWorkspaceAction>
  readonly loadCapabilities: () => void
  readonly submit: () => void
  readonly checkStatus: () => void
  readonly refreshUsage: () => void
  readonly copyResult: () => void
  readonly saveScroll: (scrollY: number) => void
  readonly readSavedScroll: () => number
  readonly abortFlights: () => void
  readonly resetWorkspace: () => void
}

export interface RewriteFeatureStore {
  readonly state: RewritingWorkspaceState
  readonly dispatch: Dispatch<RewritingWorkspaceAction>
  readonly loadCapabilities: () => void
  readonly submit: () => void
  readonly checkStatus: () => void
  readonly refreshUsage: () => void
  readonly copyResult: () => void
  readonly saveScroll: (scrollY: number) => void
  readonly readSavedScroll: () => number
  readonly abortFlights: () => void
  readonly resetWorkspace: () => void
}

export function useTranslateFeatureInstance(): TranslateFeatureStore {
  const [state, dispatch] = useReducer(
    translationWorkspaceReducer,
    undefined,
    createInitialTranslationWorkspace,
  )
  const aliveRef = useRef(true)
  const flightRef = useRef<AbortController | null>(null)
  // Read-only recovery flights (Check status, Refresh usage) own a separate
  // controller so they never abort a paid submission, and a new submission
  // supersedes them. Sign-out aborts both.
  const readRef = useRef<AbortController | null>(null)
  const copyTimerRef = useRef<number | null>(null)
  const scrollRef = useRef(0)
  const revisionRef = useRef(0)
  useEffect(() => {
    revisionRef.current = state.requestRevision
  }, [state.requestRevision])

  useEffect(() => {
    aliveRef.current = true
    return () => {
      aliveRef.current = false
      flightRef.current?.abort()
      readRef.current?.abort()
      if (copyTimerRef.current !== null) window.clearTimeout(copyTimerRef.current)
    }
  }, [])

  useEffect(() => {
    if (state.notice === TRANSLATION_MESSAGE_COPIED) {
      if (copyTimerRef.current !== null) window.clearTimeout(copyTimerRef.current)
      copyTimerRef.current = window.setTimeout(() => dispatch({ type: 'noticeDismissed' }), 2000)
    }
  }, [state.notice])

  const loadCapabilities = useCallback(() => {
    dispatch({ type: 'capabilitiesRetried' })
    flightRef.current?.abort()
    const controller = new AbortController()
    flightRef.current = controller
    void (async () => {
      const capabilities = await fetchTranslationCapabilities({ signal: controller.signal })
      if (!aliveRef.current || controller.signal.aborted) return
      if (capabilities.kind === 'ok') {
        dispatch({ type: 'capabilitiesLoaded', capabilities: capabilities.capabilities })
        const usage = await fetchTranslationUsage({ signal: controller.signal })
        if (!aliveRef.current || controller.signal.aborted) return
        if (usage.kind === 'ok') {
          dispatch({ type: 'usageUpdated', usage: usage.usage, observedAtMs: Date.now() })
        }
      } else {
        dispatch({ type: 'capabilitiesFailed' })
      }
    })()
  }, [])

  const submit = useCallback(() => {
    if (state.composing || state.capabilities === null || state.pendingOperationId !== null) return
    const current = describeTranslationReadiness(state, state.capabilities.maximumSourceCharacters)
    if (!current.canSubmit) return
    const revision = state.requestRevision + 1
    const captured = {
      source: state.source,
      sourceSelection: state.sourceSelection,
      target: state.target,
    }
    const languageName = state.capabilities.languages[captured.sourceSelection] ?? captured.sourceSelection
    const operationId = createTranslationOperationId()
    dispatch({ type: 'submitRequested' })
    flightRef.current?.abort()
    readRef.current?.abort()
    const controller = new AbortController()
    flightRef.current = controller
    void (async () => {
      const bootstrap = await fetchTranslationAntiforgeryToken({ signal: controller.signal })
      if (!aliveRef.current || controller.signal.aborted) return
      if (bootstrap.kind !== 'ok') {
        // Transport interruption before dispatch (UX-AC-075): the outcome is
        // unknown even though nothing was sent, so Check status resolves it
        // read-only instead of asserting a zero charge.
        dispatch({ type: 'submitUnknownOutcome', revision, operationId })
        return
      }
      const outcome = await submitTranslationOperation(
        {
          operationId,
          source: captured.source,
          sourceSelection: captured.sourceSelection,
          target: captured.target,
          antiforgeryToken: bootstrap.requestToken,
        },
        { signal: controller.signal },
      )
      if (!aliveRef.current || controller.signal.aborted) return
      if (outcome.kind === 'succeeded') {
        dispatch({
          type: 'submitSucceeded',
          revision,
          translatedText: outcome.translatedText,
          characterCount: outcome.characterCount,
          usage: outcome.usage,
          observedAtMs: Date.now(),
        })
        return
      }
      if (outcome.kind === 'pending') {
        const usage = await fetchTranslationUsage({ signal: controller.signal })
        if (aliveRef.current && !controller.signal.aborted && usage.kind === 'ok') {
          dispatch({ type: 'usageUpdated', usage: usage.usage, observedAtMs: Date.now() })
        }
        if (!aliveRef.current || controller.signal.aborted) return
        dispatch({
          type: 'submitFailed',
          revision,
          error: mapTranslationSubmitProblem({ httpStatus: null }),
        })
        return
      }
      if (outcome.kind === 'network') {
        // The POST may still have arrived: retain the identity for Check
        // status and assert neither success nor zero charge.
        dispatch({ type: 'submitUnknownOutcome', revision, operationId })
        return
      }
      const reset =
        outcome.resetAtUtc === null
          ? undefined
          : formatTranslationResetInstant(outcome.resetAtUtc, Date.now())
      dispatch({
        type: 'submitFailed',
        revision,
        error: mapTranslationSubmitProblem({
          httpStatus: outcome.httpStatus,
          category: outcome.category,
          reason: outcome.reason,
          resetAtUtc: reset,
          characterCount: outcome.characterCount,
          limit: outcome.limit,
          languageName,
        }),
      })
    })()
  }, [state])

  const checkStatus = useCallback(() => {
    const pendingId = state.pendingOperationId
    if (pendingId === null || state.error?.canCheckStatus !== true) return
    const revision = state.requestRevision
    readRef.current?.abort()
    const controller = new AbortController()
    readRef.current = controller
    void (async () => {
      const check = await fetchTranslationOperationStatus(pendingId, {
        signal: controller.signal,
      })
      if (!aliveRef.current || controller.signal.aborted) return
      const observedAtMs = Date.now()
      switch (check.kind) {
        case 'ok':
          if (check.status === 'succeeded') {
            dispatch({
              type: 'statusResolved',
              revision,
              operationId: pendingId,
              resolution: {
                outcome: 'succeededLostOutput',
                characterCount: check.characterCount,
                usage: check.usage,
                observedAtMs,
              },
            })
          } else if (check.status === 'failed' || check.status === 'interrupted') {
            dispatch({
              type: 'statusResolved',
              revision,
              operationId: pendingId,
              resolution: {
                outcome: 'terminalFailure',
                usage: check.usage,
                observedAtMs,
              },
            })
          } else {
            dispatch({
              type: 'statusResolved',
              revision,
              operationId: pendingId,
              resolution: { outcome: 'stillPending', usage: check.usage, observedAtMs },
            })
          }
          return
        case 'unknownRecord':
          dispatch({
            type: 'statusResolved',
            revision,
            operationId: pendingId,
            resolution: { outcome: 'noRecord' },
          })
          return
        case 'windowExpired':
          dispatch({
            type: 'statusResolved',
            revision,
            operationId: pendingId,
            resolution: { outcome: 'windowExpired' },
          })
          return
        case 'unauthorized':
          dispatch({
            type: 'statusResolved',
            revision,
            operationId: pendingId,
            resolution: {
              outcome: 'auth',
              error: mapTranslationSubmitProblem({ httpStatus: 401 }),
            },
          })
          return
        case 'forbidden':
          dispatch({
            type: 'statusResolved',
            revision,
            operationId: pendingId,
            resolution: {
              outcome: 'auth',
              error: mapTranslationSubmitProblem({ httpStatus: 403 }),
            },
          })
          return
        case 'unavailable':
          // The unknown outcome is preserved; the user can check again.
          return
      }
    })()
  }, [state])

  const refreshUsage = useCallback(() => {
    readRef.current?.abort()
    const controller = new AbortController()
    readRef.current = controller
    void (async () => {
      const usage = await fetchTranslationUsage({ signal: controller.signal })
      if (!aliveRef.current || controller.signal.aborted) return
      if (usage.kind === 'ok') {
        dispatch({ type: 'usageUpdated', usage: usage.usage, observedAtMs: Date.now() })
      } else {
        dispatch({ type: 'usageRefreshFailed' })
      }
    })()
  }, [])

  const copyResult = useCallback(() => {
    const value = state.resultText
    void (async () => {
      try {
        await window.navigator.clipboard.writeText(value)
        if (aliveRef.current) dispatch({ type: 'copied', ok: true })
      } catch {
        if (aliveRef.current) dispatch({ type: 'copied', ok: false })
      }
    })()
  }, [state.resultText])

  const saveScroll = useCallback((scrollY: number) => {
    scrollRef.current = scrollY
  }, [])

  const readSavedScroll = useCallback(() => scrollRef.current, [])

  const abortFlights = useCallback(() => {
    const revision = revisionRef.current
    flightRef.current?.abort()
    readRef.current?.abort()
    dispatch({ type: 'submitAborted', revision })
  }, [])

  const resetWorkspace = useCallback(() => {
    // M032 safe reset: abort paid and read-only flights, drop saved scroll,
    // then clear text/settings/result/errors/pending identity through the
    // single clearing action. The reducer revision bump fences late responses.
    flightRef.current?.abort()
    readRef.current?.abort()
    scrollRef.current = 0
    dispatch({ type: 'workspaceCleared' })
  }, [])

  return useMemo<TranslateFeatureStore>(
    () => ({
      state,
      dispatch,
      loadCapabilities,
      submit,
      checkStatus,
      refreshUsage,
      copyResult,
      saveScroll,
      readSavedScroll,
      abortFlights,
      resetWorkspace,
    }),
    [
      state,
      loadCapabilities,
      submit,
      checkStatus,
      refreshUsage,
      copyResult,
      saveScroll,
      readSavedScroll,
      abortFlights,
      resetWorkspace,
    ],
  )
}

export function useRewriteFeatureInstance(): RewriteFeatureStore {
  const [state, dispatch] = useReducer(
    rewritingWorkspaceReducer,
    undefined,
    createInitialRewritingWorkspace,
  )
  const aliveRef = useRef(true)
  const flightRef = useRef<AbortController | null>(null)
  // Read-only recovery flights (Check status, Refresh usage) own a separate
  // controller so they never abort a paid submission, and a new submission
  // supersedes them. Sign-out aborts both.
  const readRef = useRef<AbortController | null>(null)
  const copyTimerRef = useRef<number | null>(null)
  const scrollRef = useRef(0)
  const revisionRef = useRef(0)
  useEffect(() => {
    revisionRef.current = state.requestRevision
  }, [state.requestRevision])

  useEffect(() => {
    aliveRef.current = true
    return () => {
      aliveRef.current = false
      flightRef.current?.abort()
      readRef.current?.abort()
      if (copyTimerRef.current !== null) window.clearTimeout(copyTimerRef.current)
    }
  }, [])

  useEffect(() => {
    if (state.notice === REWRITING_MESSAGE_COPIED) {
      if (copyTimerRef.current !== null) window.clearTimeout(copyTimerRef.current)
      copyTimerRef.current = window.setTimeout(() => dispatch({ type: 'noticeDismissed' }), 2000)
    }
  }, [state.notice])

  const loadCapabilities = useCallback(() => {
    dispatch({ type: 'capabilitiesRetried' })
    flightRef.current?.abort()
    const controller = new AbortController()
    flightRef.current = controller
    void (async () => {
      const capabilities = await fetchRewritingCapabilities({ signal: controller.signal })
      if (!aliveRef.current || controller.signal.aborted) return
      if (capabilities.kind === 'ok') {
        dispatch({ type: 'capabilitiesLoaded', capabilities: capabilities.capabilities })
        const usage = await fetchRewritingUsage({ signal: controller.signal })
        if (!aliveRef.current || controller.signal.aborted) return
        if (usage.kind === 'ok') {
          dispatch({ type: 'usageUpdated', usage: usage.usage, observedAtMs: Date.now() })
        }
      } else {
        dispatch({ type: 'capabilitiesFailed' })
      }
    })()
  }, [])

  const submit = useCallback(() => {
    if (state.composing || state.capabilities === null || state.pendingOperationId !== null) return
    const current = describeRewritingReadiness(state, state.capabilities.maximumSourceCharacters)
    if (!current.canSubmit) return
    const revision = state.requestRevision + 1
    const captured = {
      source: state.source,
      sourceSelection: state.sourceSelection,
      mode: state.mode,
    }
    const operationId = createRewritingOperationId()
    dispatch({ type: 'submitRequested' })
    flightRef.current?.abort()
    readRef.current?.abort()
    const controller = new AbortController()
    flightRef.current = controller
    void (async () => {
      const bootstrap = await fetchRewritingAntiforgeryToken({ signal: controller.signal })
      if (!aliveRef.current || controller.signal.aborted) return
      if (bootstrap.kind !== 'ok') {
        // Transport interruption before dispatch (UX-AC-075): the outcome is
        // unknown even though nothing was sent, so Check status resolves it
        // read-only instead of asserting a zero charge.
        dispatch({ type: 'submitUnknownOutcome', revision, operationId })
        return
      }
      const outcome = await submitRewritingOperation(
        {
          operationId,
          source: captured.source,
          sourceSelection: captured.sourceSelection,
          mode: captured.mode,
          antiforgeryToken: bootstrap.requestToken,
        },
        { signal: controller.signal },
      )
      if (!aliveRef.current || controller.signal.aborted) return
      if (outcome.kind === 'succeeded') {
        dispatch({
          type: 'submitSucceeded',
          revision,
          rewrittenText: outcome.rewrittenText,
          characterCount: outcome.characterCount,
          usage: outcome.usage,
          observedAtMs: Date.now(),
        })
        return
      }
      if (outcome.kind === 'pending') {
        const usage = await fetchRewritingUsage({ signal: controller.signal })
        if (aliveRef.current && !controller.signal.aborted && usage.kind === 'ok') {
          dispatch({ type: 'usageUpdated', usage: usage.usage, observedAtMs: Date.now() })
        }
        if (!aliveRef.current || controller.signal.aborted) return
        dispatch({
          type: 'submitFailed',
          revision,
          error: mapRewritingSubmitProblem({ httpStatus: null }),
        })
        return
      }
      if (outcome.kind === 'network') {
        // The POST may still have arrived: retain the identity for Check
        // status and assert neither success nor zero charge.
        dispatch({ type: 'submitUnknownOutcome', revision, operationId })
        return
      }
      const reset =
        outcome.resetAtUtc === null
          ? undefined
          : formatRewritingResetInstant(outcome.resetAtUtc, Date.now())
      dispatch({
        type: 'submitFailed',
        revision,
        error: mapRewritingSubmitProblem({
          httpStatus: outcome.httpStatus,
          category: outcome.category,
          reason: outcome.reason,
          resetAtUtc: reset,
          characterCount: outcome.characterCount,
          limit: outcome.limit,
        }),
      })
    })()
  }, [state])

  const checkStatus = useCallback(() => {
    const pendingId = state.pendingOperationId
    if (pendingId === null || state.error?.canCheckStatus !== true) return
    const revision = state.requestRevision
    readRef.current?.abort()
    const controller = new AbortController()
    readRef.current = controller
    void (async () => {
      const check = await fetchRewritingOperationStatus(pendingId, {
        signal: controller.signal,
      })
      if (!aliveRef.current || controller.signal.aborted) return
      const observedAtMs = Date.now()
      switch (check.kind) {
        case 'ok':
          if (check.status === 'succeeded') {
            dispatch({
              type: 'statusResolved',
              revision,
              operationId: pendingId,
              resolution: {
                outcome: 'succeededLostOutput',
                characterCount: check.characterCount,
                usage: check.usage,
                observedAtMs,
              },
            })
          } else if (check.status === 'failed' || check.status === 'interrupted') {
            dispatch({
              type: 'statusResolved',
              revision,
              operationId: pendingId,
              resolution: {
                outcome: 'terminalFailure',
                usage: check.usage,
                observedAtMs,
              },
            })
          } else {
            dispatch({
              type: 'statusResolved',
              revision,
              operationId: pendingId,
              resolution: { outcome: 'stillPending', usage: check.usage, observedAtMs },
            })
          }
          return
        case 'unknownRecord':
          dispatch({
            type: 'statusResolved',
            revision,
            operationId: pendingId,
            resolution: { outcome: 'noRecord' },
          })
          return
        case 'windowExpired':
          dispatch({
            type: 'statusResolved',
            revision,
            operationId: pendingId,
            resolution: { outcome: 'windowExpired' },
          })
          return
        case 'unauthorized':
          dispatch({
            type: 'statusResolved',
            revision,
            operationId: pendingId,
            resolution: {
              outcome: 'auth',
              error: mapRewritingSubmitProblem({ httpStatus: 401 }),
            },
          })
          return
        case 'forbidden':
          dispatch({
            type: 'statusResolved',
            revision,
            operationId: pendingId,
            resolution: {
              outcome: 'auth',
              error: mapRewritingSubmitProblem({ httpStatus: 403 }),
            },
          })
          return
        case 'unavailable':
          // The unknown outcome is preserved; the user can check again.
          return
      }
    })()
  }, [state])

  const refreshUsage = useCallback(() => {
    readRef.current?.abort()
    const controller = new AbortController()
    readRef.current = controller
    void (async () => {
      const usage = await fetchRewritingUsage({ signal: controller.signal })
      if (!aliveRef.current || controller.signal.aborted) return
      if (usage.kind === 'ok') {
        dispatch({ type: 'usageUpdated', usage: usage.usage, observedAtMs: Date.now() })
      } else {
        dispatch({ type: 'usageRefreshFailed' })
      }
    })()
  }, [])

  const copyResult = useCallback(() => {
    const value = state.resultText
    void (async () => {
      try {
        await window.navigator.clipboard.writeText(value)
        if (aliveRef.current) dispatch({ type: 'copied', ok: true })
      } catch {
        if (aliveRef.current) dispatch({ type: 'copied', ok: false })
      }
    })()
  }, [state.resultText])

  const saveScroll = useCallback((scrollY: number) => {
    scrollRef.current = scrollY
  }, [])

  const readSavedScroll = useCallback(() => scrollRef.current, [])

  const abortFlights = useCallback(() => {
    const revision = revisionRef.current
    flightRef.current?.abort()
    readRef.current?.abort()
    dispatch({ type: 'submitAborted', revision })
  }, [])

  const resetWorkspace = useCallback(() => {
    // M032 safe reset: abort paid and read-only flights, drop saved scroll,
    // then clear text/settings/result/errors/pending identity through the
    // single clearing action. The reducer revision bump fences late responses.
    flightRef.current?.abort()
    readRef.current?.abort()
    scrollRef.current = 0
    dispatch({ type: 'workspaceCleared' })
  }, [])

  return useMemo<RewriteFeatureStore>(
    () => ({
      state,
      dispatch,
      loadCapabilities,
      submit,
      checkStatus,
      refreshUsage,
      copyResult,
      saveScroll,
      readSavedScroll,
      abortFlights,
      resetWorkspace,
    }),
    [
      state,
      loadCapabilities,
      submit,
      checkStatus,
      refreshUsage,
      copyResult,
      saveScroll,
      readSavedScroll,
      abortFlights,
      resetWorkspace,
    ],
  )
}

export interface WorkspaceStores {
  readonly translate: TranslateFeatureStore
  readonly rewrite: RewriteFeatureStore
  readonly resetAll: () => void
}

export const WorkspaceStoreContext = createContext<WorkspaceStores | null>(null)

export function useWorkspaceStores(): WorkspaceStores | null {
  return useContext(WorkspaceStoreContext)
}

/**
 * Pages render inside the provider under the real shell and standalone in
 * focused unit tests. The standalone fallback preserves the exact M028/M029
 * per-page behavior (including unmount-abort); the provider keeps flights
 * alive across navigation.
 */
export function useTranslateFeature(): TranslateFeatureStore {
  const context = useContext(WorkspaceStoreContext)
  const local = useTranslateFeatureInstance()
  return context?.translate ?? local
}

export function useRewriteFeature(): RewriteFeatureStore {
  const context = useContext(WorkspaceStoreContext)
  const local = useRewriteFeatureInstance()
  return context?.rewrite ?? local
}
