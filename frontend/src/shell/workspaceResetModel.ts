import type { RewritingWorkspaceState } from '../rewrite/rewritingWorkspace'
import type { TranslationWorkspaceState } from '../translate/translationWorkspace'

export const RESET_DIALOG_HEADING = 'Start a new workspace?' as const
export const RESET_DIALOG_BODY =
  'Source text, results, and workspace settings in this tab will be cleared. This cannot be undone.' as const
export const RESET_DIALOG_CANCEL = 'Cancel' as const
export const RESET_DIALOG_CONFIRM = 'Start new workspace' as const
export const WORKSPACE_FOOTER_TEXT =
  'Text and settings are cleared when this workspace ends, including refresh, sign-out, or session expiry.' as const

export type WorkspaceFeature = 'translation' | 'rewrite'

export function isTranslationWorkspaceEmpty(state: TranslationWorkspaceState): boolean {
  return state.source === '' && !state.hasResult && state.resultText === ''
}

export function isRewritingWorkspaceEmpty(state: RewritingWorkspaceState): boolean {
  return state.source === '' && !state.hasResult && state.resultText === ''
}

let pendingTranslationSourceFocus = false

export function requestTranslationSourceFocus(): void {
  pendingTranslationSourceFocus = true
}

export function consumeTranslationSourceFocus(): boolean {
  const pending = pendingTranslationSourceFocus
  pendingTranslationSourceFocus = false
  return pending
}

export function focusWorkspaceSource(feature: WorkspaceFeature): boolean {
  const target = document.querySelector(`textarea[data-workspace-source="${feature}"]`)
  if (target instanceof HTMLTextAreaElement) {
    target.focus()
    return true
  }
  return false
}
