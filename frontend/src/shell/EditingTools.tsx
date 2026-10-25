import { useEffect, useId, useRef, useState, type CSSProperties } from 'react'
import type { WorkspaceAction, WorkspaceState } from './workspaceModel'
import { ActionIcon } from './ActionIcon'

const choices = [ ['correctionOnly', 'None set'], ['simple', 'Simple'], ['business', 'Business'], ['academic', 'Academic'], ['casual', 'Casual'], ['enthusiastic', 'Enthusiastic'], ['friendly', 'Friendly'], ['confident', 'Confident'], ['diplomatic', 'Diplomatic'] ] as const
export function EditingTools({ state, dispatch, disabled, onCollapse }: { readonly state: WorkspaceState; readonly dispatch: (action: Exclude<WorkspaceAction, { type: 'capabilitiesLoaded' }>) => void; readonly disabled: boolean; readonly onCollapse: () => void }) {
  const [open, setOpen] = useState(false), [active, setActive] = useState(0), [position, setPosition] = useState<CSSProperties>({})
  const trigger = useRef<HTMLButtonElement>(null), list = useRef<HTMLDivElement>(null)
  const id = useId(), search = useRef(''), searchAt = useRef(0)
  const close = () => { setOpen(false); trigger.current?.focus() }
  useEffect(() => {
    if (!open) return
    list.current?.focus()
    const outside = (event: PointerEvent) => { if (!list.current?.contains(event.target as Node) && !trigger.current?.contains(event.target as Node)) setOpen(false) }
    const resize = () => { setOpen(false); trigger.current?.focus() }
    document.addEventListener('pointerdown', outside); window.addEventListener('resize', resize)
    return () => { document.removeEventListener('pointerdown', outside); window.removeEventListener('resize', resize) }
  }, [open])
  useEffect(() => { if (open) document.getElementById(`${id}-${active}`)?.scrollIntoView?.({ block: 'nearest' }) }, [active, id, open])
  const commit = (index: number) => { dispatch({ type: 'modeChanged', value: choices[index][0] }); close() }
  const option = (index: number) => <div key={choices[index][0]} id={`${id}-${index}`} role="option" aria-selected={state.mode === choices[index][0]} data-active={active === index} onPointerMove={() => setActive(index)} onClick={() => commit(index)}><span>{choices[index][1]}</span><span aria-hidden="true">{state.mode === choices[index][0] ? '✓' : ''}</span></div>
  return <aside className="editing-tools" aria-label="Editing tools" onKeyDown={event => { if (event.key === 'Escape' && !open) { event.stopPropagation(); onCollapse() } }}>
    <button type="button" className="icon-button" aria-label="Collapse toolbar" onClick={onCollapse}><ActionIcon name="panel" /></button>
    <h2>Editing tools</h2>
    {disabled && <p id={`${id}-disabled`} className="tool-explanation">Writing tools are available on Write.</p>}
    <label className="tool-row"><span>Correction Only</span><button type="button" role="switch" aria-label="Correction Only" aria-checked={state.correctionOnly} disabled={disabled} aria-describedby={disabled ? `${id}-disabled` : undefined} onClick={() => dispatch({ type: 'correctionOnlyToggled' })}><span /></button></label>
    <button ref={trigger} type="button" className="styles-trigger" aria-label="Styles/Tones" aria-haspopup="listbox" aria-expanded={open} aria-controls={open ? `${id}-list` : undefined} disabled={disabled || state.correctionOnly} onClick={() => {
      if (open) { close(); return }
      const rect = trigger.current!.getBoundingClientRect(), below = window.innerHeight - rect.bottom - 12, above = rect.top - 12
      const height = Math.min(360, Math.max(below, above))
      setPosition({ position: 'fixed', left: Math.max(8, Math.min(rect.left, window.innerWidth - rect.width - 8)), top: below >= Math.min(360, above) ? rect.bottom + 4 : Math.max(8, rect.top - height - 4), width: rect.width, maxHeight: height })
      setActive(Math.max(0, choices.findIndex(([value]) => value === state.mode))); setOpen(true)
    }}><span>Styles/Tones</span><span>{choices.find(([value]) => value === state.mode)?.[1]} <span aria-hidden="true">⌄</span></span></button>
    {open && <div ref={list} id={`${id}-list`} className="styles-list" role="listbox" aria-label="Styles/Tones" tabIndex={0} aria-activedescendant={`${id}-${active}`} style={position} onKeyDown={event => {
      let next = active
      if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close(); return }
      if (event.key === 'Tab') { setOpen(false); return }
      if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); commit(active); return }
      if (event.key === 'ArrowDown') next = (active + 1) % choices.length
      else if (event.key === 'ArrowUp') next = (active + choices.length - 1) % choices.length
      else if (event.key === 'Home') next = 0
      else if (event.key === 'End') next = choices.length - 1
      else if (event.key.length === 1) {
        search.current = event.timeStamp - searchAt.current > 700 ? event.key : search.current + event.key
        searchAt.current = event.timeStamp
        const found = choices.findIndex(([, name]) => name.toLowerCase().startsWith(search.current.toLowerCase()))
        if (found >= 0) next = found
      } else return
      event.preventDefault(); setActive(next)
    }}>
      {option(0)}<div role="group" aria-label="Writing style"><div className="list-heading" aria-hidden="true">WRITING STYLE</div>{[1, 2, 3, 4].map(option)}</div>
      <div role="group" aria-label="Tone"><div className="list-heading" aria-hidden="true">TONE</div>{[5, 6, 7, 8].map(option)}</div>
    </div>}
    <label className="tool-row"><span>Show changes</span><button type="button" role="switch" aria-label="Show changes" aria-checked={state.showChanges} disabled={disabled} onClick={() => dispatch({ type: 'showChangesToggled' })}><span /></button></label>
  </aside>
}
