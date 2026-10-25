import * as model from '../shell/workspaceModel'
export * from '../shell/workspaceModel'
export const REWRITING_MAXIMUM_SOURCE_CHARACTERS = 2000 as const
export interface RewritingCapabilities extends model.WorkspaceCapabilities {
  readonly modeDefault: string
  readonly modeValues: readonly string[]
  readonly modeNames: Readonly<Record<string, string>>
}
export type RewritingUsage = model.WorkspaceUsage
export type RewritingError = model.WorkspaceError
export type RewritingWorkspaceState = model.WorkspaceState<RewritingCapabilities>
export type RewritingWorkspaceAction = model.WorkspaceAction<RewritingCapabilities>
export type RewritingStatusResolution = model.StatusResolution
export const createInitialWorkspace = () => model.createInitialWorkspace<RewritingCapabilities>()
export const rewritingWorkspaceReducer = model.workspaceReducer<RewritingCapabilities>
export const describeReadiness = (state: RewritingWorkspaceState, limit: number) => model.describeReadiness(state, limit, 'rewriting')
export const selectInlineValidation = (state: RewritingWorkspaceState) => model.selectInlineValidation(state, 'rewriting')
export const formatOversizeMessage = () => model.formatOversizeMessage('rewriting')
export const mapSubmitProblem = (input: model.SubmitProblemInput) => model.mapSubmitProblem(input, 'rewriting')
