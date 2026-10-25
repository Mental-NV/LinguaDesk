import { useId } from 'react'
import { useRewriteFeature } from '../shell/workspaceStores'
import { WorkspaceView } from '../shell/WorkspaceView'

export function RewritePage({ onSignOut }: { readonly onSignOut: () => void }) {
  const store = useRewriteFeature()
  const { state, dispatch } = store
  const id = useId(), capabilities = state.capabilities
  return <WorkspaceView key={state.generation} feature="rewriting" store={store} onSignOut={onSignOut} settings={<>
    <div className="field source-settings">
      <label className="visually-hidden" htmlFor={`${id}-language`}>Writing language</label>
      <select id={`${id}-language`} value={state.sourceSelection} disabled={!capabilities} onChange={event => dispatch({ type: 'sourceSelectionChanged', value: event.target.value })}>
        <option value="auto">{state.resolvedSourceLanguage ? `${capabilities?.languages[state.resolvedSourceLanguage] ?? state.resolvedSourceLanguage} (detected)` : 'Detect language'}</option>
        {(capabilities?.sourceValues ?? []).filter(language => language !== 'auto').map(language => <option key={language} value={language}>{capabilities?.languages[language]}</option>)}
      </select>
    </div>
  </>} />
}
