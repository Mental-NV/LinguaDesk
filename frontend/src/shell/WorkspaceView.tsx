import { useEffect, useId, useRef, useState, type ReactNode } from 'react'
import { WorkspaceLifetimeFooter } from './WorkspaceReset'
import { MESSAGE_OFFLINE, selectInlineValidation, type Feature, type WorkspaceCapabilities } from './workspaceModel'
import type { FeatureStore } from './workspaceStores'

function fitMobileEditor(element: HTMLTextAreaElement | null) {
  if (!element) return
  if (window.matchMedia?.('(max-width: 767px)').matches) {
    element.style.height = '240px'
    element.style.height = `${Math.min(360, Math.max(240, element.scrollHeight))}px`
  } else element.style.removeProperty('height')
}
export function WorkspaceView<C extends WorkspaceCapabilities>({ feature, store, settings, onSignOut }: {
  readonly feature: Feature; readonly store: FeatureStore<C>; readonly settings: ReactNode; readonly onSignOut: () => void
}) {
  const { state, dispatch, setActive, loadCapabilities, checkStatus, refreshUsage, submit, saveScroll, readSavedScroll } = store
  const id = useId()
  const source = useRef<HTMLTextAreaElement>(null)
  const result = useRef<HTMLTextAreaElement>(null)
  const resultName = feature === 'translation' ? 'Translation result' : 'Improved result'
  const [offline, setOffline] = useState(() => !navigator.onLine)
  const validation = selectInlineValidation(state, feature)
  const messages = [validation, state.error?.text, offline ? MESSAGE_OFFLINE : null].filter((message): message is string => !!message)
  const uniqueMessages = [...new Set(messages)]
  useEffect(() => {
    setActive(true)
    const saved = readSavedScroll()
    if (saved > 0) window.scrollTo(0, saved)
    return () => { saveScroll(window.scrollY); setActive(false) }
  }, [setActive, saveScroll, readSavedScroll])
  useEffect(() => {
    if (!state.capabilities && !state.capabilitiesFailed) loadCapabilities()
  }, [state.capabilities, state.capabilitiesFailed, loadCapabilities])
  useEffect(() => {
    const update = () => setOffline(!navigator.onLine)
    window.addEventListener('online', update)
    window.addEventListener('offline', update)
    return () => { window.removeEventListener('online', update); window.removeEventListener('offline', update) }
  }, [])
  useEffect(() => {
    const fit = () => { fitMobileEditor(source.current); fitMobileEditor(result.current) }
    fit()
    window.addEventListener('resize', fit)
    return () => window.removeEventListener('resize', fit)
  }, [state.source, state.resultText])
  return (
    <div className={`workspace workspace-${feature}`}>
      <div className="workspace-grid">
        {settings}
        <section className="editor-panel source-panel">
          <h2 id={`${id}-source-heading`}><label htmlFor={`${id}-source`}>Source text</label></h2>
          <textarea ref={source} id={`${id}-source`} value={state.source} autoComplete="off"
            data-workspace-source={feature === 'translation' ? 'translation' : 'rewrite'}
            aria-invalid={validation !== null} aria-describedby={validation ? `${id}-alerts` : undefined}
            onChange={event => dispatch({ type: 'sourceChanged', source: event.target.value })}
            onCompositionStart={() => dispatch({ type: 'compositionStarted' })}
            onCompositionEnd={() => dispatch({ type: 'compositionEnded' })} />
        </section>
        <div className="source-actions">
          <button type="button" className="secondary-button" onClick={() => {
            dispatch({ type: 'sourceChanged', source: '' })
            source.current?.focus()
          }}>Clear source text</button>
        </div>
        <section className="editor-panel result-panel">
          <h2 id={`${id}-result-heading`}><label htmlFor={`${id}-result`}>{resultName}</label></h2>
          <textarea ref={result} id={`${id}-result`} value={state.resultText} autoComplete="off"
            readOnly={!state.hasResult} placeholder={feature === 'translation' ? 'Your translation will appear here.' : 'Your improved text will appear here.'}
            aria-busy={state.phase === 'submitting' && state.inputRevision === state.submittedInputRevision && state.resultEditRevision === state.submittedResultEditRevision}
            onChange={event => dispatch({ type: 'resultEdited', value: event.target.value })}
            onCompositionStart={() => dispatch({ type: 'resultCompositionStarted' })}
            onCompositionEnd={() => dispatch({ type: 'resultCompositionEnded' })} />
        </section>
      </div>
      {uniqueMessages.length > 0 && <div className="server-alert" role="alert" id={`${id}-alerts`}>
        {uniqueMessages.map(message => <p key={message}>{message}</p>)}
        {state.error?.canCheckStatus && <button type="button" className="secondary-button" onClick={checkStatus}>Check status</button>}
        {state.error?.canRetry && <button type="button" className="secondary-button" onClick={submit}>Try again</button>}
        {state.error && !state.error.canRetry && !state.error.canCheckStatus && /allowance|budget|unavailable|remaining|availability/.test(state.error.text) && <button type="button" className="secondary-button" onClick={refreshUsage}>Check availability</button>}
      </div>}
      {state.capabilitiesFailed && <div className="server-alert" role="alert">
        <p>{feature === 'translation' ? 'Translation' : 'Rewriting'} settings could not be loaded.</p>
        <button type="button" className="secondary-button" onClick={loadCapabilities}>Try again</button>
      </div>}
      {state.notice && <div className="workspace-notice" role="status"><p>{state.notice}</p></div>}
      <p className="visually-hidden" role="status">{state.phase === 'submitting' ? 'Processing…' : ''}</p>
      <p key={state.appliedRevision} className="visually-hidden" role="status">{state.appliedRevision > 0 ? (feature === 'translation' ? 'Translation ready.' : 'Rewrite ready.') : ''}</p>
      <div className="workspace-account"><button type="button" className="inline-button" onClick={onSignOut}>Sign out</button></div>
      <WorkspaceLifetimeFooter />
    </div>
  )
}
