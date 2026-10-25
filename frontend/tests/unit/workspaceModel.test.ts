import { createInitialWorkspace, describeReadiness, mapSubmitProblem, orderUsage, selectorPolicyFor, workspaceReducer, type WorkspaceAction, type WorkspaceState } from '../../src/shell/workspaceModel'
import { USAGE } from '../helpers/workspace'

const capabilities = { maximumSourceCharacters: 5000, sourceDefault: 'auto', sourceValues: ['auto', 'en', 'ru', 'ro', 'zh'], targetValues: ['en', 'ru', 'ro', 'zh'], languages: { en: 'English', ro: 'Romanian' }, modeDefault: 'correctionOnly', modeValues: ['correctionOnly', 'friendly'] }
function ready(): WorkspaceState { return { ...createInitialWorkspace(), capabilities, source: 'Hello.', sourceSelection: 'en', target: 'ro' } }
function success(state: WorkspaceState, extras: Partial<Extract<WorkspaceAction, { type: 'submitSucceeded' }>> = {}) {
  return workspaceReducer(state, { type: 'submitSucceeded', revision: state.requestRevision, translatedText: 'Bună.', characterCount: 6, admissionDay: USAGE.day, usage: USAGE, observedAtMs: Date.parse('2026-10-25T12:00:00Z'), ...extras })
}
it('requires an explicit target and respects local unicode/selector validation', () => {
  expect(describeReadiness(ready(), 5000, 'translation').canSubmit).toBe(true)
  expect(describeReadiness({ ...ready(), target: '' }, 5000, 'translation').canSubmit).toBe(false)
  expect(describeReadiness({ ...ready(), source: '\ud800' }, 5000, 'translation').canSubmit).toBe(false)
  expect(selectorPolicyFor(capabilities).modeValues).toEqual(['correctionOnly', 'friendly'])
})
it('guards duplicate dispatches and composition', () => {
  const first = workspaceReducer(ready(), { type: 'submitRequested' })
  expect(workspaceReducer(first, { type: 'submitRequested' })).toBe(first)
  const composing = workspaceReducer(ready(), { type: 'compositionStarted' })
  expect(workspaceReducer(composing, { type: 'submitRequested' })).toBe(composing)
})
it.each<WorkspaceAction>([
  { type: 'sourceChanged', source: 'Changed.' }, { type: 'sourceSelectionChanged', value: 'ru' }, { type: 'targetChanged', value: 'zh' }, { type: 'modeChanged', value: 'friendly' }, { type: 'resultEdited', value: 'Manual.' }, { type: 'compositionStarted' }, { type: 'resultCompositionStarted' },
])('protects text against earlier output after $type', action => {
  const pending = workspaceReducer(ready(), { type: 'submitRequested' })
  const edited = workspaceReducer(pending, action)
  const result = success(edited)
  expect(result.resultText).toBe(action.type === 'resultEdited' ? 'Manual.' : '')
  expect(result.phase).toBe('idle')
  expect(result.usage).toEqual(USAGE)
  expect(result.notice).toContain('6 characters counted toward your usage')
  expect(result.appliedRevision).toBe(0)
})
it('a later submission may replace preexisting edits but only when no newer edit intervenes', () => {
  const edited = workspaceReducer(ready(), { type: 'resultEdited', value: 'Manual' })
  expect(success(workspaceReducer(edited, { type: 'submitRequested' })).resultText).toBe('Bună.')
})
it('clearing increments generation and drops text, timers identities and old account usage', () => {
  const pending = workspaceReducer(ready(), { type: 'submitRequested' })
  const cleared = workspaceReducer(pending, { type: 'workspaceCleared' })
  expect(cleared).toMatchObject({ source: '', resultText: '', sourceSelection: 'auto', target: '', mode: 'correctionOnly', generation: 1, pendingOperationId: null, usage: null })
  expect(success(cleared, { revision: pending.requestRevision })).toBe(cleared)
})
it('unknown identity survives editing and no record never authorizes a paid retry', () => {
  let state = workspaceReducer(ready(), { type: 'submitRequested' })
  state = workspaceReducer(state, { type: 'submitUnknownOutcome', revision: state.requestRevision, operationId: 'original' })
  state = workspaceReducer(state, { type: 'sourceChanged', source: 'Changed' })
  state = workspaceReducer(state, { type: 'statusResolved', revision: state.requestRevision, operationId: 'original', resolution: { outcome: 'noRecord' } })
  expect(state.pendingOperationId).toBe('original')
  expect(state.error).toMatchObject({ canRetry: false, canCheckStatus: true })
})
it('metadata-only success discloses unavailable output and its admission day', () => {
  let state = workspaceReducer(ready(), { type: 'submitRequested' })
  state = workspaceReducer(state, { type: 'submitUnknownOutcome', revision: state.requestRevision, operationId: 'original' })
  state = workspaceReducer(state, { type: 'statusResolved', revision: state.requestRevision, operationId: 'original', resolution: { outcome: 'succeededLostOutput', characterCount: 6, admissionDay: '2026-10-24', usage: USAGE, observedAtMs: 1 } })
  expect(state.resultText).toBe('')
  expect(state.notice).toContain('result text is no longer available')
  expect(state.notice).toContain('2026-10-24')
})
it('out-of-order usage never replaces newer snapshots, while a new period resets it', () => {
  const state = workspaceReducer(ready(), { type: 'usageUpdated', usage: USAGE, observedAtMs: 1 })
  const old = workspaceReducer(state, { type: 'usageUpdated', usage: { ...USAGE, revision: 0, consumedCharacters: 0 }, observedAtMs: 2 })
  expect(old.usage).toEqual(USAGE)
  const next = { ...USAGE, resetAtUtc: '2026-10-27T00:00:00Z', consumedCharacters: 0, revision: 0 }
  expect(workspaceReducer(state, { type: 'usageUpdated', usage: next, observedAtMs: 3 }).usage).toEqual(next)
})
it('equal revisions with inconsistent values require a new read', () => {
  expect(orderUsage(USAGE, { ...USAGE, availableCharacters: 1 })).toBe('ambiguous')
  expect(orderUsage(USAGE, { ...USAGE, revision: 2, availableCharacters: 12600 })).toBe('newer')
})
it.each([
  [401, undefined, undefined, 'Sign in to continue.'], [403, undefined, undefined, 'Verify your email'],
  [422, undefined, 'uncertain', 'Choose the source language'], [422, undefined, 'sameLanguage', 'Choose a target language different from English.'],
  [422, undefined, 'oversizedSource', 'Shorten the text'], [422, undefined, 'unsupported', 'unsupported or mixed-language'],
  [429, 'globalAllowance', undefined, 'shared daily allowance'], [429, 'userAllowance', undefined, 'Your daily allowance'],
  [503, 'monetarySuspension', undefined, 'service budget'], [504, undefined, undefined, 'Processing took too long'], [502, undefined, undefined, 'We couldn’t process'],
])('maps HTTP %s/%s/%s to essential feedback', (httpStatus, category, reason, text) => {
  expect(mapSubmitProblem({ httpStatus, category, reason, languageName: 'English' }, 'translation').text).toContain(text)
})
