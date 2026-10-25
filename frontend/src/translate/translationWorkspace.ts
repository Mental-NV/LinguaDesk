import * as model from '../shell/workspaceModel'
export * from '../shell/workspaceModel'
export const TRANSLATION_MAXIMUM_SOURCE_CHARACTERS = 5000 as const
export interface TranslationCapabilities extends model.WorkspaceCapabilities {
  readonly targetValues: readonly string[]
}
export type TranslationUsage = model.WorkspaceUsage
export type TranslationError = model.WorkspaceError
export type TranslationWorkspaceState = model.WorkspaceState<TranslationCapabilities>
export type TranslationWorkspaceAction = model.WorkspaceAction<TranslationCapabilities>
export type TranslationStatusResolution = model.StatusResolution
export const createInitialWorkspace = () => model.createInitialWorkspace<TranslationCapabilities>()
export const translationWorkspaceReducer = model.workspaceReducer<TranslationCapabilities>
export const describeReadiness = (state: TranslationWorkspaceState, limit: number) => model.describeReadiness(state, limit, 'translation')
export const selectInlineValidation = (state: TranslationWorkspaceState) => model.selectInlineValidation(state, 'translation')
export const formatOversizeMessage = () => model.formatOversizeMessage('translation')
export const mapSubmitProblem = (input: model.SubmitProblemInput) => model.mapSubmitProblem(input, 'translation')
