import { useMemo, useRef, type ReactNode } from 'react'
import type { WorkspaceAction, WorkspaceState } from '../shell/workspaceModel'
import { currentResult } from '../shell/workspaceModel'
import { compareText, changedScalarRanges } from './comparison'
import { scalarSlice, sentenceSpans, spanKey, type SentenceSpan } from './sentences'

export function WritePresentation({ state, dispatch, requestAlternatives }: { readonly state: WorkspaceState; readonly dispatch: (action: Exclude<WorkspaceAction, { type: 'capabilitiesLoaded' }>) => void; readonly requestAlternatives?: (span: SentenceSpan) => void }) {
  const down = useRef<{ x: number; y: number } | null>(null)
  const comparison = useMemo(() => state.showChanges && state.comparisonSource !== null ? compareText(state.comparisonSource, state.resultText) : [], [state.showChanges, state.comparisonSource, state.resultText])
  const ranges = changedScalarRanges(comparison)
  const contextLength = Array.from(state.resultText).length
  const assistance = currentResult(state) && state.association !== null && state.comparisonSource !== null
  const spans = useMemo(() => assistance && contextLength <= 8000 ? sentenceSpans(state.resultText) : [], [assistance, contextLength, state.resultText])
  const selectedSet = state.selectedSentence ? state.alternativeSets[spanKey(state.selectedSentence)] : undefined
  const loading = state.phase === 'submitting' && state.operationKind === 'sentenceAlternatives' && state.selectedSentence && state.alternativesCapture && spanKey(state.selectedSentence) === spanKey(state.alternativesCapture.span)
  const highlight = (start: number, length: number): ReactNode => {
    const end = start + length, pieces: ReactNode[] = []
    let at = start
    for (const range of ranges) {
      const from = Math.max(at, range.start), to = Math.min(end, range.end)
      if (from >= to) continue
      if (from > at) pieces.push(scalarSlice(state.resultText, at, from - at))
      pieces.push(<mark className="change-insertion" key={`${from}:${to}`}>{scalarSlice(state.resultText, from, to - from)}</mark>); at = to
    }
    if (at < end) pieces.push(scalarSlice(state.resultText, at, end - at))
    return pieces
  }
  let offset = 0
  const main: ReactNode[] = []
  for (const span of spans) {
    if (span.start > offset) main.push(highlight(offset, span.start - offset))
    const enabled = assistance && contextLength <= 8000 && span.length <= 2000
    main.push(enabled ? <span key={spanKey(span)} className="result-sentence" role="button" tabIndex={0}
      aria-label={`Sentence alternatives: ${scalarSlice(state.resultText, span.start, span.length)}`}
      aria-pressed={state.selectedSentence !== null && spanKey(state.selectedSentence) === spanKey(span)}
      onPointerDown={event => { down.current = { x: event.clientX, y: event.clientY } }}
      onClick={event => {
        const moved = down.current && Math.hypot(event.clientX - down.current.x, event.clientY - down.current.y) > 4
        down.current = null
        if (!moved && (window.getSelection()?.isCollapsed ?? true)) requestAlternatives?.(span)
      }}
      onKeyDown={event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); requestAlternatives?.(span) } }}
    >{highlight(span.start, span.length)}</span> : <span key={spanKey(span)}>{highlight(span.start, span.length)}</span>)
    offset = span.start + span.length
  }
  if (offset < contextLength) main.push(highlight(offset, contextLength - offset))
  return <div className="write-presentation">
    <div className="result-plain-text" aria-label="Improved result" data-testid="write-result" aria-busy={state.phase === 'submitting' && state.operationKind === 'fullText' && state.inputRevision === state.submittedInputRevision && state.resultEditRevision === state.submittedResultEditRevision} tabIndex={0} onKeyDown={event => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'a') {
        event.preventDefault()
        const range = document.createRange(); range.selectNodeContents(event.currentTarget)
        const selection = window.getSelection(); selection?.removeAllRanges(); selection?.addRange(range)
      }
    }}>{main}</div>
    {state.showChanges && comparison.some(part => part.kind === 'delete') && <div className="deletion-review" aria-label="Deleted source text">
      <span>Deleted from source:</span> {comparison.filter(part => part.kind === 'delete').map((part, index) => <del key={index}>{part.text}</del>)}
    </div>}
    {assistance && contextLength > 8000 && <p className="assistance-message">Sentence alternatives are unavailable because this result exceeds 8,000 characters. You can still edit and copy it.</p>}
    {assistance && state.selectedSentence && (loading || selectedSet || state.alternativesError) && <section className="alternatives" aria-label="Sentence alternatives" aria-busy={!!loading}>
      <h3>Alternatives:</h3>
      {loading && <p role="status">Generating alternatives…</p>}
      {state.alternativesError && <p role="alert">{state.alternativesError}</p>}
      {state.alternativesError && state.alternativesCanRetry && <button type="button" className="text-action" onClick={() => { if (state.selectedSentence) requestAlternatives?.(state.selectedSentence) }}>Try again</button>}
      {selectedSet && <p className="visually-hidden" role="status">Alternatives ready.</p>}
      {selectedSet?.choices.filter(value => value !== selectedSet.current).map(value => <button type="button" key={value} className="alternative-option" disabled={!currentResult(state) || state.composing} onClick={() => dispatch({ type: 'alternativeApplied', value, association: crypto.randomUUID() })}>{value}</button>)}
    </section>}
  </div>
}
