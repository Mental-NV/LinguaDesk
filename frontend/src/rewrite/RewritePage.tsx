import { useCallback, useEffect, useId, useState } from 'react'
import { analyzeInput } from '../api/inputPolicy'
import {
  StartNewWorkspaceButton,
  WorkspaceLifetimeFooter,
  WorkspaceSessionTeardown,
} from '../shell/WorkspaceReset'
import { useRewriteFeature } from '../shell/workspaceStores'
import {
  MESSAGE_OFFLINE,
  describeReadiness,
  formatUsageLine,
  selectInlineValidation,
  selectStatusLine,
} from './rewritingWorkspace'

const LOAD_FAILURE_MESSAGE = 'Rewriting settings could not be loaded.' as const
const ANNOUNCE_READY_MESSAGE = 'Rewrite ready.' as const

interface RewritePageProps {
  readonly onSignOut: () => void
}

export function RewritePage({ onSignOut }: RewritePageProps) {
  const store = useRewriteFeature()
  const {
    state,
    dispatch,
    loadCapabilities,
    submit,
    checkStatus,
    refreshUsage,
    copyResult,
    saveScroll,
    readSavedScroll,
  } = store
  const formId = useId()
  const sourceId = `${formId}-source`
  const sourceLanguageId = `${formId}-source-language`
  const modeId = `${formId}-writing-mode`
  const resultId = `${formId}-result`
  const validationId = `${formId}-validation`
  const errorId = `${formId}-error`

  const [offline, setOffline] = useState(() => !window.navigator.onLine)

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

  const capabilities = state.capabilities
  const capabilitiesFailed = state.capabilitiesFailed
  useEffect(() => {
    // The lifted store survives navigation: reload only when no capabilities
    // settled yet, so returning to this page never aborts a hidden flight.
    if (capabilities === null && !capabilitiesFailed) loadCapabilities()
  }, [capabilities, capabilitiesFailed, loadCapabilities])

  useEffect(() => {
    const saved = readSavedScroll()
    if (saved > 0) {
      try {
        window.scrollTo(0, saved)
      } catch {
        /* jsdom and other non-visual runtimes ignore programmatic scroll */
      }
    }
    return () => {
      saveScroll(window.scrollY)
    }
  }, [saveScroll, readSavedScroll])

  const maximumSourceCharacters =
    state.capabilities?.maximumSourceCharacters ?? 2000
  const readiness = describeReadiness(state, maximumSourceCharacters)
  const inlineValidation = selectInlineValidation(state)
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

  const modeNameFor = useCallback(
    (id: string): string => state.capabilities?.modeNames[id] ?? id,
    [state.capabilities],
  )

  // Explicit submission, transport and stale fencing live in the lifted
  // store so a pending operation survives navigation to the sibling page.
  const handleRewrite = submit

  const handleCopy = copyResult

  const sourceOptions = state.capabilities
    ? [
        { value: state.capabilities.sourceDefault, label: 'Detect automatically' },
        ...state.capabilities.sourceValues
          .filter((id) => id !== state.capabilities?.sourceDefault)
          .map((id) => ({ value: id, label: languageNameFor(id) })),
      ]
    : []

  const rewriteDisabled = !readiness.canSubmit
  const showResult = state.hasResult

  return (
    <div className="workspace">
      <div className="workspace-controls">
        <div className="field">
          <label htmlFor={sourceLanguageId}>Writing language</label>
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
          <label htmlFor={modeId}>Writing mode</label>
          <select
            id={modeId}
            value={state.mode}
            onChange={(event) => dispatch({ type: 'modeChanged', value: event.target.value })}
          >
            {(state.capabilities?.modeValues ?? ['correctionOnly']).map((id) => (
              <option key={id} value={id}>
                {modeNameFor(id)}
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
          data-workspace-source="rewrite"
          autoComplete="off"
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
          disabled={rewriteDisabled}
          onClick={handleRewrite}
        >
          {state.phase === 'submitting' ? 'Rewriting…' : 'Rewrite'}
        </button>
        <StartNewWorkspaceButton
          feature="rewrite"
          fallbackReset={() => store.resetWorkspace()}
          fallbackEmpty={state.source === '' && !state.hasResult && state.resultText === ''}
        />
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
          {state.error.canCheckStatus ? (
            <p>
              <button type="button" className="secondary-button" onClick={checkStatus}>
                Check status
              </button>
            </p>
          ) : state.error.canRetry ? (
            <p>
              <button type="button" className="secondary-button" onClick={handleRewrite}>
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
            autoComplete="off"
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
        {state.usage === null && !state.usageUnavailable ? null : (
          <p>
            <button type="button" className="secondary-button" onClick={refreshUsage}>
              Refresh usage
            </button>
          </p>
        )}
      </section>

      <WorkspaceLifetimeFooter />
      <WorkspaceSessionTeardown />
    </div>
  )
}
