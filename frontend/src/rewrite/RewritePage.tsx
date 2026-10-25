import { useId } from 'react'
import { useRewriteFeature } from '../shell/workspaceStores'
import { WorkspaceView } from '../shell/WorkspaceView'

export function RewritePage({ onSignOut }: { readonly onSignOut: () => void }) {
  const store = useRewriteFeature()
  const { state, dispatch } = store
  const id = useId()
  const capabilities = state.capabilities
  return <WorkspaceView feature="rewriting" store={store} onSignOut={onSignOut} settings={
    <div className="source-settings rewrite-settings">
      <div className="field">
        <label htmlFor={`${id}-language`}>Writing language</label>
        <select id={`${id}-language`} value={state.sourceSelection} disabled={!capabilities}
          onChange={event => dispatch({ type: 'sourceSelectionChanged', value: event.target.value })}>
          <option value="auto">Detect automatically</option>
          {(capabilities?.sourceValues ?? []).filter(language => language !== capabilities?.sourceDefault).map(language => <option key={language} value={language}>{capabilities?.languages[language]}</option>)}
        </select>
      </div>
      <div className="field">
        <label htmlFor={`${id}-mode`}>Writing mode</label>
        <select id={`${id}-mode`} value={state.mode} disabled={!capabilities}
          onChange={event => dispatch({ type: 'modeChanged', value: event.target.value })}>
          {(capabilities?.modeValues ?? ['correctionOnly']).map(mode => <option key={mode} value={mode}>{capabilities?.modeNames[mode] ?? 'Correction only'}</option>)}
        </select>
      </div>
    </div>
  } />
}
