import { useEffect, useLayoutEffect, useId, useRef, useState, type ReactNode } from 'react'
import { EditingTools } from './EditingTools'
import { ActionIcon } from './ActionIcon'
import { WritePresentation } from '../rewrite/WritePresentation'
import { canReuseResult } from './workspaceModel'
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
export function WorkspaceView<C extends WorkspaceCapabilities>({ feature, store, settings }: {
  readonly feature: Feature; readonly store: FeatureStore<C>; readonly settings: ReactNode; readonly onSignOut?: () => void
}) {
  const { state, dispatch, setActive, loadCapabilities, checkStatus, refreshUsage, submit, saveScroll, readSavedScroll, requestAlternatives } = store
  const id = useId()
  const focusToolbar = useRef(false)
  const toolbarButton = useRef<HTMLButtonElement>(null)
  const [clipboardMessage, setClipboardMessage] = useState('')
  const mounted = useRef(true)
  useEffect(() => { mounted.current = true; return () => { mounted.current = false } }, [])
  const copy = async (text: string) => {
    try { await navigator.clipboard.writeText(text); if (mounted.current) setClipboardMessage('Copied.') }
    catch { if (mounted.current) setClipboardMessage('Couldn’t copy. Select the text and copy it manually.') }
  }
  useEffect(() => {
    if (clipboardMessage !== 'Copied.') return
    const timer = setTimeout(() => setClipboardMessage(''), 2500)
    return () => clearTimeout(timer)
  }, [clipboardMessage])
  const collapseToolbar = () => { focusToolbar.current = true; dispatch({ type: 'toolbarToggled' }) }
  useLayoutEffect(() => {
    if (!focusToolbar.current) return
    focusToolbar.current = false
    if (state.toolbarExpanded) document.querySelector<HTMLButtonElement>('.editing-tools button')?.focus()
    else toolbarButton.current?.focus()
  }, [state.toolbarExpanded])
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
  useEffect(() => { if (state.editingResult) result.current?.focus() }, [state.editingResult])
  return (
    <div className={`workspace workspace-${feature}`}>
      <div className={`workspace-body${state.toolbarExpanded ? ' tools-expanded' : ''}`}>
        <div className="language-bar">
          <div className="language-controls">{settings}</div>
          <button type="button" className="icon-button swap-button" title={feature === 'translation' ? 'Swap languages and texts' : 'Use result as source'} aria-label={feature === 'translation' ? 'Swap languages and texts' : 'Use result as source'} disabled={!canReuseResult(state)} onClick={() => dispatch({ type: 'swapOrReuse', feature })}><ActionIcon name="swap" /></button>
          {!state.toolbarExpanded && <button ref={toolbarButton} type="button" className="icon-button toolbar-disclosure" aria-label="Toolbar" aria-expanded={false} onClick={() => { focusToolbar.current = true; dispatch({ type: 'toolbarToggled' }) }}><ActionIcon name="tools" /><span>Toolbar</span></button>}
        </div>
        {state.toolbarExpanded && <EditingTools state={state} dispatch={dispatch} disabled={feature === 'translation'} onCollapse={collapseToolbar} />}
        <div className="workspace-grid">
          <section className="editor-panel source-panel">
            <h2 className="visually-hidden" id={`${id}-source-heading`}><label htmlFor={`${id}-source`}>Source text</label></h2>
            <textarea ref={source} id={`${id}-source`} value={state.source} autoComplete="off"
              placeholder={feature === 'translation' ? 'Type to translate.' : 'Type or paste text to see ideas for improvement.'}
              data-workspace-source={feature === 'translation' ? 'translation' : 'rewrite'}
              aria-invalid={validation !== null} aria-describedby={validation ? `${id}-alerts` : undefined}
              onChange={event => dispatch({ type: 'sourceChanged', source: event.target.value })}
              onCompositionStart={() => dispatch({ type: 'compositionStarted' })}
              onCompositionEnd={() => dispatch({ type: 'compositionEnded' })} />
            {!state.source && feature === 'rewriting' && <p className="source-helper">Click a sentence in the result to see alternatives.</p>}
            {state.source && <button type="button" className="icon-button clear-source" title="Clear source text" aria-label="Clear source text" onClick={() => { dispatch({ type: 'sourceChanged', source: '' }); source.current?.focus() }}><ActionIcon name="close" /></button>}
            <div className="editor-actions">{state.source && feature === 'translation' && <button type="button" className="icon-button" title="Copy source text" aria-label="Copy source text" onPointerDown={event => event.preventDefault()} onClick={() => { void copy(state.source) }}><ActionIcon name="copy" /></button>}</div>
          </section>
          <section className="editor-panel result-panel">
            <h2 className="visually-hidden" id={`${id}-result-heading`}><label htmlFor={`${id}-result`}>{resultName}</label></h2>
            {feature === 'rewriting' && !state.editingResult
              ? <WritePresentation state={state} dispatch={dispatch} requestAlternatives={requestAlternatives} />
              : <textarea ref={result} id={`${id}-result`} value={state.resultText} autoComplete="off"
                readOnly={!state.hasResult}
                aria-busy={state.phase === 'submitting' && state.inputRevision === state.submittedInputRevision && state.resultEditRevision === state.submittedResultEditRevision}
                onChange={event => dispatch({ type: 'resultEdited', value: event.target.value })}
                onCompositionStart={() => dispatch({ type: 'resultCompositionStarted' })}
                onCompositionEnd={() => dispatch({ type: 'resultCompositionEnded' })} />}
            <div className="editor-actions">
              {feature === 'rewriting' && state.hasResult && <button type="button" className="text-action" onClick={() => dispatch({ type: 'resultEditingToggled' })}>{state.editingResult ? 'Done editing' : 'Edit result'}</button>}
              {state.resultText && <button type="button" className="icon-button" title="Copy result" aria-label="Copy result" onPointerDown={event => event.preventDefault()} onClick={() => { void copy(state.resultText) }}><ActionIcon name="copy" /></button>}
            </div>
          </section>
        </div>
      </div>
      {clipboardMessage && <p className="clipboard-message" role="status">{clipboardMessage}</p>}
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
      <p className="visually-hidden" role="status">{state.phase === 'submitting' && state.operationKind === 'fullText' ? 'Processing…' : ''}</p>
      <p key={state.appliedRevision} className="visually-hidden" role="status">{state.appliedRevision > 0 ? (feature === 'translation' ? 'Translation ready.' : 'Rewrite ready.') : ''}</p>
      <WorkspaceLifetimeFooter />
    </div>
  )
}
