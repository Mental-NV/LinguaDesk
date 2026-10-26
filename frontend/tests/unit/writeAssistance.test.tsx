import { act, cleanup, fireEvent, screen, within } from '@testing-library/react'
import fixtures from '../../../contracts/fixtures/sentences.v2.json'
import { sentenceSpans, SEGMENTATION_REVISION, scalarSlice } from '../../src/rewrite/sentences'
import { compareText } from '../../src/rewrite/comparison'
import { createInitialWorkspace, workspaceReducer, canReuseResult } from '../../src/shell/workspaceModel'
import { advance, chooseMode, deferred, editSource, expectResult, FIXED_TIME, flush, installWorkspaceFetch, json, mountWorkspace, USAGE } from '../helpers/workspace'

it('uses the same scalar sentence fixtures as the server', () => {
  expect(SEGMENTATION_REVISION).toBe(fixtures.revision)
  for (const fixture of fixtures.cases) expect(sentenceSpans(fixture.text, 'language' in fixture ? fixture.language : undefined), fixture.id).toEqual(fixture.spans)
  expect(sentenceSpans('bad\ud800')).toEqual([])
})
it.each([
  ['👩🏽‍💻 went.\r\nA sentence deleted.', '👩🏽‍💻 came.\r\n'],
  ['Café\n你好。', 'Café\n您好。'],
  ['A '.repeat(700), 'B '.repeat(700)],
])('comparison preserves both complete strings, Unicode clusters and bounded fallback', (source, result) => {
  const parts = compareText(source, result)
  expect(parts.filter(part => part.kind !== 'delete').map(part => part.text).join('')).toBe(result)
  expect(parts.filter(part => part.kind !== 'insert').map(part => part.text).join('')).toBe(source)
  expect(parts.some(part => part.kind === 'delete')).toBe(true)
  const clusters = new Set(Array.from(new Intl.Segmenter(undefined, { granularity: 'grapheme' }).segment(source + result), part => part.segment))
  for (const part of parts) for (const cluster of new Intl.Segmenter(undefined, { granularity: 'grapheme' }).segment(part.text)) expect(clusters.has(cluster.segment)).toBe(true)
})
it('atomic swap compares values and advances exactly one input revision', () => {
  const state = { ...createInitialWorkspace(), source: 'Hello.', sourceSelection: 'auto', target: 'ro', resultText: 'Salut.', hasResult: true, resolvedSourceLanguage: 'en', comparisonSource: 'Hello.', association: 'revision' }
  const next = workspaceReducer(state, { type: 'swapOrReuse', feature: 'translation' })
  expect(next).toMatchObject({ source: 'Salut.', resultText: 'Hello.', sourceSelection: 'ro', target: 'en', inputRevision: 1, comparisonSource: null, resolvedSourceLanguage: null })
  expect(workspaceReducer({ ...state, pendingOperationId: 'unknown' }, { type: 'swapOrReuse', feature: 'translation' }).source).toBe('Hello.')
  expect(canReuseResult({ ...state, resolvedSourceLanguage: null })).toBe(false)
  expect(canReuseResult({ ...state, resultEdited: true })).toBe(true)
  expect(canReuseResult({ ...state, composing: true })).toBe(false)
})
it('manual edit and compositionstart destroy all assistance and undo cannot restore it', () => {
  const state = { ...createInitialWorkspace(), resultText: 'Ready.', hasResult: true, resolvedSourceLanguage: 'en', association: 'old', comparisonSource: 'Ready now.', alternativeSets: { '0:6': { span: { start: 0, length: 6 }, choices: ['Ready.', 'Prepared.', 'Complete.'], current: 'Ready.' } } }
  for (const action of [{ type: 'resultEdited' as const, value: 'Edited.' }, { type: 'resultCompositionStarted' as const }]) {
    const edited = workspaceReducer(state, action)
    const undone = workspaceReducer(edited, { type: 'resultEdited', value: 'Ready.' })
    expect(undone).toMatchObject({ association: null, comparisonSource: null, alternativeSets: {}, resultEdited: true })
  }
})

describe('explicit alternatives lane', () => {
  beforeEach(() => { vi.useFakeTimers(); vi.setSystemTime(new Date(FIXED_TIME)); vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(true) })
  afterEach(() => { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals() })
  const result = 'The report is ready. We send it today.'
  async function ready(alternatives?: NonNullable<Parameters<typeof installWorkspaceFetch>[0]>['alternatives']) {
    const network = installWorkspaceFetch({ operation: body => json({ rewrittenText: result, resolvedSourceLanguage: 'en', characterCount: Array.from(body.source).length, usage: { ...USAGE, revision: 2 }, admissionDay: USAGE.day }), alternatives })
    const view = mountWorkspace('rewriting'); await flush(); editSource('The report is really ready. We sends it today.'); await advance()
    return { ...network, ...view }
  }
  const success = (body: Record<string, unknown>) => json({ ...body, options: ['The report is prepared.', 'The report is complete.'], resolvedSourceLanguage: 'en', mode: 'correctionOnly', characterCount: 20, usage: { ...USAGE, revision: 3 }, admissionDay: USAGE.day }, 201)
  it('requests one explicit scalar span, reuses choices with no full rewrite, and clears other context caches', async () => {
    const h = await ready(success)
    fireEvent.click(screen.getByRole('button', { name: 'Sentence alternatives: The report is ready.' })); await flush()
    expect(h.alternatives).toHaveLength(1)
    expect(h.alternatives[0]).toMatchObject({ context: result, start: 0, length: 20, language: 'en', mode: 'correctionOnly' })
    fireEvent.click(screen.getByRole('button', { name: 'The report is prepared.' }))
    expectResult('rewriting', 'The report is prepared. We send it today.')
    expect(h.state().comparisonSource).toBe('The report is really ready. We sends it today.')
    fireEvent.click(screen.getByRole('button', { name: 'The report is ready.' }))
    expectResult('rewriting', result)
    fireEvent.click(screen.getByRole('button', { name: 'Sentence alternatives: The report is ready.' })); await advance(2000)
    expect(h.alternatives).toHaveLength(1); expect(h.operations).toHaveLength(1)
  })
  it('late success caches a different selected sentence but cannot reopen it', async () => {
    const pending = deferred<Response>()
    const h = await ready(() => pending.promise)
    fireEvent.click(screen.getByRole('button', { name: 'Sentence alternatives: The report is ready.' })); await flush()
    fireEvent.click(screen.getByRole('button', { name: 'Sentence alternatives: We send it today.' }))
    await act(async () => pending.resolve(success(h.alternatives[0])))
    expect(screen.queryByRole('region', { name: 'Sentence alternatives' })).not.toBeInTheDocument()
    expect(h.state().alternativeSets['0:20']).toBeDefined()
    expect(h.alternatives).toHaveLength(1)
  })
  it('source edits queue only the latest full rewrite and stale alternatives only reconcile usage', async () => {
    const pending = deferred<Response>()
    const h = await ready(() => pending.promise)
    fireEvent.click(screen.getByRole('button', { name: 'Sentence alternatives: The report is ready.' })); await flush()
    editSource('First revision.'); await advance(); editSource('Latest revision.'); await advance()
    expect(h.operations).toHaveLength(1)
    await act(async () => pending.resolve(success(h.alternatives[0]))); await flush()
    expect(h.operations).toHaveLength(2); expect(h.operations[1].source).toBe('Latest revision.')
    expect(h.state().alternativeSets).toEqual({})
    expect(h.alternatives).toHaveLength(1)
  })
  it('unknown alternatives keep the original identity and 15-second deadline; read-only status never regenerates', async () => {
    const pending = deferred<Response>()
    const h = await ready(() => pending.promise)
    fireEvent.click(screen.getByRole('button', { name: 'Sentence alternatives: The report is ready.' })); await flush(); await advance(15000)
    expect(h.state().pendingOperationId).toBe(h.alternatives[0].operationId)
    fireEvent.click(screen.getByRole('button', { name: 'Check status' })); await flush()
    expect(h.reads).toContain(`/api/operations/${h.alternatives[0].operationId}`)
    fireEvent.click(screen.getByRole('button', { name: 'Sentence alternatives: We send it today.' })); await flush()
    expect(h.alternatives).toHaveLength(1)
  })
  it('panels, Show changes and equivalent correction controls never restart the pause', async () => {
    const h = installWorkspaceFetch(); mountWorkspace('rewriting'); await flush(); editSource(); await advance(500)
    fireEvent.click(screen.getByRole('button', { name: 'Toolbar' }))
    fireEvent.click(screen.getByRole('switch', { name: 'Correction Only' }))
    fireEvent.click(screen.getByRole('switch', { name: 'Show changes' }))
    await advance(499); expect(h.operations).toHaveLength(0)
    await advance(1); expect(h.operations).toHaveLength(1)
    chooseMode('business'); await advance()
    fireEvent.click(screen.getByRole('switch', { name: 'Correction Only' })); await advance()
    fireEvent.click(screen.getByRole('switch', { name: 'Correction Only' })); await advance(2000)
    expect(h.operations).toHaveLength(3)
    expect(screen.getByRole('button', { name: 'Styles/Tones' })).toHaveTextContent('None set')
  })
  it('manual editing keeps native surface stable, clears all assistance, and Show changes stays local', async () => {
    const h = await ready(success)
    fireEvent.click(screen.getByRole('button', { name: 'Toolbar' })); fireEvent.click(screen.getByRole('switch', { name: 'Show changes' }))
    expect(screen.getByLabelText('Deleted source text')).toHaveTextContent('Deleted from source:')
    fireEvent.click(screen.getByRole('button', { name: 'Edit result' }))
    const editor = screen.getByRole('textbox', { name: 'Improved result' })
    expect(editor).toHaveFocus(); fireEvent.compositionStart(editor); fireEvent.compositionEnd(editor)
    fireEvent.change(editor, { target: { value: 'Manual\ntext.' } }); expect(screen.getByRole('textbox', { name: 'Improved result' })).toBe(editor)
    fireEvent.click(screen.getByRole('button', { name: 'Done editing' })); expectResult('rewriting', 'Manual\ntext.')
    expect(screen.queryByRole('button', { name: /Sentence alternatives:/ })).not.toBeInTheDocument()
    expect(h.state().comparisonSource).toBeNull(); expect(h.operations).toHaveLength(1)
    expect(scalarSlice(result, 0, 20)).toBe('The report is ready.')
    expect(within(screen.getByLabelText('Improved result')).queryByText('Deleted from source:')).not.toBeInTheDocument()
  })
})
