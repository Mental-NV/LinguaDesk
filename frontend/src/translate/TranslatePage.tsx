import { useCallback, useEffect, useId, useReducer, useRef, useState } from 'react'
import { analyzeInput } from '../api/inputPolicy'
import {
  createOperationId,
  fetchCurrentUsage,
  fetchSubmitAntiforgeryToken,
  fetchTranslationCapabilities,
  submitTranslationOperation,
} from '../api/translation'
import {
  MESSAGE_COPIED,
  MESSAGE_OFFLINE,
  createInitialWorkspace,
  describeReadiness,
  formatResetInstant,
  formatUsageLine,
  mapSubmitProblem,
  selectInlineValidation,
  selectStatusLine,
  translationWorkspaceReducer,
} from './translationWorkspace'

const LOAD_FAILURE_MESSAGE = 'Translation settings could not be loaded.' as const
const ANNOUNCE_READY_MESSAGE = 'Translation ready.' as const

interface TranslatePageProps {
  readonly onSignOut: () => void
}

export function TranslatePage({ onSignOut }: TranslatePageProps) {
  const [state, dispatch] = useReducer(translationWorkspaceReducer, undefined, createInitialWorkspace)
  const formId = useId()
  const sourceId = `${formId}-source`
  const sourceLanguageId = `${formId}-source-language`
  const targetLanguageId = `${formId}-target-language`
  const resultId = `${formId}-result`
  const validationId = `${formId}-validation`
  const errorId = `${formId}-error`

  const mountedRef = useRef(true)
  const flightRef = useRef<AbortController | null>(null)
  const copyTimerRef = useRef<number | null>(null)
  const [offline, setOffline] = useState(() => !window.navigator.onLine)

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      flightRef.current?.abort()
      if (copyTimerRef.current !== null) window.clearTimeout(copyTimerRef.current)
    }
  }, [])

  useEffect(() => {
    const handleOnline = (): void => setOffline(false)
    const handleOffline = (): void => setOffline(true)
    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)
    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [setOffline])

  const loadCapabilities = useCallback(() => {
    dispatch({ type: 'capabilitiesRetried' })
    flightRef.current?.abort()
    const controller = new AbortController()
    flightRef.current = controller
    void (async () => {
      const capabilities = await fetchTranslationCapabilities({ signal: controller.signal })
      if (!mountedRef.current || controller.signal.aborted) return
      if (capabilities.kind === 'ok') {
        dispatch({ type: 'capabilitiesLoaded', capabilities: capabilities.capabilities })
        const usage = await fetchCurrentUsage({ signal: controller.signal })
        if (!mountedRef.current || controller.signal.aborted) return
        if (usage.kind === 'ok') {
          dispatch({ type: 'usageUpdated', usage: usage.usage, observedAtMs: Date.now() })
        }
      } else {
        dispatch({ type: 'capabilitiesFailed' })
      }
    })()
  }, [])

  useEffect(() => {
    loadCapabilities()
  }, [loadCapabilities])

  useEffect(() => {
    if (state.notice === MESSAGE_COPIED) {
      if (copyTimerRef.current !== null) window.clearTimeout(copyTimerRef.current)
      copyTimerRef.current = window.setTimeout(() => dispatch({ type: 'noticeDismissed' }), 2000)
    }
  }, [state.notice])

  const maximumSourceCharacters =
    state.capabilities?.maximumSourceCharacters ?? 5000
  const readiness = describeReadiness(state, maximumSourceCharacters)
  const inlineValidation = selectInlineValidation(state, readiness)
  const statusLine = selectStatusLine(state)
  const inputAnalysis = analyzeInput(state.source, maximumSourceCharacters)
  const scalarLabel =
    inputAnalysis.scalarCount === null
      ? 'Character count unavailable'
      : `${inputAnalysis.scalarCount.toLocaleString('en-US')} / ${maximumSourceCharacters.toLocaleString('en-US')} characters`

  const languageNameFor = useCallback(
    (id: string): string => state.capabilities?.languages[id] ?? id,
    [state.capabilities],
  )

  const handleTranslate = useCallback(() => {
    if (state.composing || state.capabilities === null) return
    const current = describeReadiness(state, state.capabilities.maximumSourceCharacters)
    if (!current.canSubmit) return
    const revision = state.requestRevision + 1
    const captured = {
      source: state.source,
      sourceSelection: state.sourceSelection,
      target: state.target,
    }
    dispatch({ type: 'submitRequested' })
    flightRef.current?.abort()
    const controller = new AbortController()
    flightRef.current = controller
    void (async () => {
      const bootstrap = await fetchSubmitAntiforgeryToken({ signal: controller.signal })
      if (!mountedRef.current || controller.signal.aborted) return
      if (bootstrap.kind !== 'ok') {
        dispatch({
          type: 'submitFailed',
          revision,
          error: mapSubmitProblem({ httpStatus: null }),
        })
        return
      }
      const outcome = await submitTranslationOperation(
        {
          operationId: createOperationId(),
          source: captured.source,
          sourceSelection: captured.sourceSelection,
          target: captured.target,
          antiforgeryToken: bootstrap.requestToken,
        },
        { signal: controller.signal },
      )
      if (!mountedRef.current || controller.signal.aborted) return
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
        const usage = await fetchCurrentUsage({ signal: controller.signal })
        if (mountedRef.current && !controller.signal.aborted && usage.kind === 'ok') {
          dispatch({ type: 'usageUpdated', usage: usage.usage, observedAtMs: Date.now() })
        }
        if (!mountedRef.current || controller.signal.aborted) return
        dispatch({
          type: 'submitFailed',
          revision,
          error: mapSubmitProblem({ httpStatus: null }),
        })
        return
      }
      if (outcome.kind === 'network') {
        dispatch({
          type: 'submitFailed',
          revision,
          error: mapSubmitProblem({ httpStatus: null }),
        })
        return
      }
      const reset =
        outcome.resetAtUtc === null
          ? undefined
          : formatResetInstant(outcome.resetAtUtc, Date.now())
      dispatch({
        type: 'submitFailed',
        revision,
        error: mapSubmitProblem({
          httpStatus: outcome.httpStatus,
          category: outcome.category,
          reason: outcome.reason,
          resetAtUtc: reset,
          characterCount: outcome.characterCount,
          limit: outcome.limit,
          languageName: languageNameFor(captured.sourceSelection),
        }),
      })
    })()
  }, [state, languageNameFor])

  const handleCopy = useCallback(() => {
    void (async () => {
      try {
        await window.navigator.clipboard.writeText(state.resultText)
        if (mountedRef.current) dispatch({ type: 'copied', ok: true })
      } catch {
        if (mountedRef.current) dispatch({ type: 'copied', ok: false })
      }
    })()
  }, [state.resultText])

  const sourceOptions = state.capabilities
    ? [
        { value: state.capabilities.sourceDefault, label: 'Detect automatically' },
        ...state.capabilities.targetValues.map((id) => ({ value: id, label: languageNameFor(id) })),
      ]
    : []

  const translateDisabled = !readiness.canSubmit
  const showResult = state.hasResult

  return (
    <div className="workspace">
      <div className="workspace-controls">
        <div className="field">
          <label htmlFor={sourceLanguageId}>Source language</label>
          <select
            id={sourceLanguageId}
            value={state.sourceSelection}
            onChange={(event) =>
              dispatch({ type: 'sourceSelectionChanged', value: event.target.value })
            }
          >
            {sourceOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor={targetLanguageId}>Target language</label>
          <select
            id={targetLanguageId}
            value={state.target}
            onChange={(event) => dispatch({ type: 'targetChanged', value: event.target.value })}
          >
            <option value="">Choose target language</option>
            {(state.capabilities?.targetValues ?? []).map((id) => (
              <option
                key={id}
                value={id}
                disabled={
                  state.sourceSelection !== state.capabilities?.sourceDefault &&
                  id === state.sourceSelection
                }
              >
                {languageNameFor(id)}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="field">
        <label htmlFor={sourceId}>Source text</label>
        <textarea
          id={sourceId}
          value={state.source}
          rows={6}
          aria-describedby={inlineValidation === null ? undefined : validationId}
          aria-invalid={inlineValidation !== null}
          onChange={(event) => dispatch({ type: 'sourceChanged', source: event.target.value })}
          onCompositionStart={() => dispatch({ type: 'compositionStarted' })}
          onCompositionEnd={() => dispatch({ type: 'compositionEnded' })}
        />
        <p className="character-count">{scalarLabel}</p>
        {inlineValidation === null ? null : (
          <p className="field-error" id={validationId} role="alert">
            {inlineValidation}
          </p>
        )}
      </div>

      <div className="form-actions">
        <button
          type="button"
          className="primary-button"
          disabled={translateDisabled}
          onClick={handleTranslate}
        >
          {state.phase === 'submitting' ? 'Translating…' : 'Translate'}
        </button>
        <button type="button" className="secondary-button" onClick={onSignOut}>
          Sign out
        </button>
      </div>

      {statusLine === null ? null : (
        <div className="workspace-status" role="status">
          <p>{statusLine}</p>
        </div>
      )}
      {state.appliedRevision === 0 ? null : (
        <p key={state.appliedRevision} className="visually-hidden" role="status">
          {ANNOUNCE_READY_MESSAGE}
        </p>
      )}

      {state.error === null ? null : (
        <div className="server-alert" id={errorId} role="alert">
          <p>{state.error.text}</p>
          {state.error.canRetry ? (
            <p>
              <button type="button" className="secondary-button" onClick={handleTranslate}>
                Try again
              </button>
            </p>
          ) : null}
        </div>
      )}

      {state.notice === null ? null : (
        <div className="workspace-notice" role="status">
          <p>{state.notice}</p>
        </div>
      )}

      {state.capabilitiesFailed ? (
        <div className="server-alert" role="alert">
          <p>{LOAD_FAILURE_MESSAGE}</p>
          <p>
            <button type="button" className="secondary-button" onClick={loadCapabilities}>
              Try again
            </button>
          </p>
        </div>
      ) : null}

      {state.copyAlert === null ? null : (
        <div className="server-alert" role="alert">
          <p>{state.copyAlert}</p>
        </div>
      )}

      {offline ? (
        <div className="server-alert" role="alert">
          <p>{MESSAGE_OFFLINE}</p>
        </div>
      ) : null}

      {showResult ? (
        <div className="field">
          <label htmlFor={resultId}>Result</label>
          <textarea
            id={resultId}
            value={state.resultText}
            rows={6}
            onChange={(event) => dispatch({ type: 'resultEdited', value: event.target.value })}
          />
          <div className="form-actions">
            <button type="button" className="secondary-button" onClick={handleCopy}>
              Copy result
            </button>
          </div>
        </div>
      ) : null}

      <section aria-label="Usage">
        {state.usage === null || state.usageObservedAtMs === null ? (
          <p>Usage is unavailable until the first successful submission.</p>
        ) : (
          <p>{formatUsageLine(state.usage, state.usageObservedAtMs)}</p>
        )}
      </section>
    </div>
  )
}

