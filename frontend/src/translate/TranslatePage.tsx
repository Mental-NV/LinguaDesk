import { useId } from 'react'
import { useTranslateFeature } from '../shell/workspaceStores'
import { WorkspaceView } from '../shell/WorkspaceView'

export function TranslatePage({ onSignOut }: { readonly onSignOut: () => void }) {
  const store = useTranslateFeature()
  const { state, dispatch } = store
  const id = useId(), capabilities = state.capabilities
  return <WorkspaceView key={state.generation} feature="translation" store={store} onSignOut={onSignOut} settings={<>
    <div className="field source-settings">
      <label className="visually-hidden" htmlFor={`${id}-language`}>Source language</label>
      <select id={`${id}-language`} value={state.sourceSelection} disabled={!capabilities} onChange={event => dispatch({ type: 'sourceSelectionChanged', value: event.target.value })}>
        <option value="auto">{state.resolvedSourceLanguage ? `${capabilities?.languages[state.resolvedSourceLanguage] ?? state.resolvedSourceLanguage} (detected)` : 'Detect language'}</option>
        {(capabilities?.sourceValues ?? []).filter(language => language !== 'auto').map(language => <option key={language} value={language}>{capabilities?.languages[language]}</option>)}
      </select>
    </div>
    <div className="field result-settings">
      <label className="visually-hidden" htmlFor={`${id}-target`}>Target language</label>
      <select id={`${id}-target`} value={state.target} disabled={!capabilities} onChange={event => dispatch({ type: 'targetChanged', value: event.target.value })}>
        <option value="">Choose target language</option>
        {(capabilities?.targetValues ?? []).map(language => <option key={language} value={language} disabled={language === state.sourceSelection}>{capabilities?.languages[language]}</option>)}
      </select>
    </div>
  </>} />
}
