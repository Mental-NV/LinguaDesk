import { useId } from 'react'
import { useTranslateFeature } from '../shell/workspaceStores'
import { WorkspaceView } from '../shell/WorkspaceView'

export function TranslatePage({ onSignOut }: { readonly onSignOut: () => void }) {
  const store = useTranslateFeature()
  const { state, dispatch } = store
  const id = useId()
  const capabilities = state.capabilities
  return <WorkspaceView feature="translation" store={store} onSignOut={onSignOut} settings={<>
    <div className="field source-settings translation-language">
      <label htmlFor={`${id}-source-language`}>Source language</label>
      <select id={`${id}-source-language`} value={state.sourceSelection} disabled={!capabilities}
        onChange={event => dispatch({ type: 'sourceSelectionChanged', value: event.target.value })}>
        <option value="auto">Detect automatically</option>
        {(capabilities?.targetValues ?? []).map(language => <option key={language} value={language}>{capabilities?.languages[language]}</option>)}
      </select>
    </div>
    <div className="field result-settings translation-language">
      <label htmlFor={`${id}-target-language`}>Target language</label>
      <select id={`${id}-target-language`} value={state.target} disabled={!capabilities}
        onChange={event => dispatch({ type: 'targetChanged', value: event.target.value })}>
        <option value="">Choose target language</option>
        {(capabilities?.targetValues ?? []).map(language => <option key={language} value={language} disabled={language === state.sourceSelection}>{capabilities?.languages[language]}</option>)}
      </select>
    </div>
  </>} />
}
