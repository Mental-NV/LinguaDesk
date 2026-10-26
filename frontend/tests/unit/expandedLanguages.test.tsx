import { cleanup, fireEvent, screen } from '@testing-library/react'
import catalog from '../../../contracts/fixtures/languages.v1.json'
import { toCapabilities as translationCapabilities } from '../../src/api/translation'
import { toCapabilities as rewritingCapabilities } from '../../src/api/rewriting'
import { CAPABILITIES, advance, editSource, flush, installWorkspaceFetch, json, mountWorkspace } from '../helpers/workspace'

const body = { ...CAPABILITIES, languages: catalog.languages, sourceSelection: { default: 'auto', values: ['auto', ...catalog.languages.map(l => l.id)] } } as unknown as Parameters<typeof translationCapabilities>[0]
it('parses all advertised canonical languages once, orders native choices by English name and retains direction/script labels', () => {
  const translation = translationCapabilities(body), rewriting = rewritingCapabilities(body)
  expect(Object.keys(translation.languages)).toHaveLength(124)
  expect(translation.sourceValues).toHaveLength(125)
  expect(translation.targetValues).toHaveLength(124)
  expect(new Set(translation.targetValues).size).toBe(124)
  expect(translation.targetValues).toEqual(rewriting.sourceValues.filter(id => id !== 'auto'))
  expect(translation.targetValues?.map(id => body.languages.find(l => l.id === id)!.name)).toEqual(body.languages.map(l => l.name).sort((a, b) => a.localeCompare(b, 'en')))
  expect(translation.languageDirections?.ar).toBe('rtl')
  expect(translation.languageDirections?.he).toBe('rtl')
  expect(translation.languageDirections?.hi).toBe('ltr')
  expect(translation.languages.pa).toBe('Punjabi (Gurmukhi)')
  expect(translation.languages.mn).toBe('Mongolian (Cyrillic)')
  expect(translation.languages.zh).toBe('Chinese (Simplified output)')
  expect(translation.sourceValues).toContain('fil')
  expect(translation.sourceValues).not.toContain('tl')
})

describe('expanded capability workspace', () => {
  beforeEach(() => { vi.useFakeTimers(); vi.setSystemTime(new Date('2026-10-25T12:00:00Z')) })
  afterEach(() => { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals() })
  it('a three-letter native selection submits exactly once after the existing pause', async () => {
    const network = installWorkspaceFetch({ capabilities: () => json(body) })
    mountWorkspace('rewriting'); await flush()
    expect(screen.getByRole('option', { name: 'Kurdish (Sorani)' })).toHaveValue('ckb')
    fireEvent.change(screen.getByLabelText('Writing language'), { target: { value: 'fil' } })
    editSource('May 12 aklat si Mira.')
    await advance(999); expect(network.operations).toHaveLength(0)
    await advance(1); expect(network.operations).toHaveLength(1)
    expect(network.operations[0].sourceSelection).toBe('fil')
    await advance(2000); expect(network.operations).toHaveLength(1)
  })
  it.each(['sentence-boundaries-v1', 'future-policy', undefined])('revision %s disables only unsafe alternatives and preserves editing/copy actions', async revision => {
    const network = installWorkspaceFetch({ capabilities: () => json({ ...body, alternatives: { segmentationRevision: revision } }) })
    const h = mountWorkspace('rewriting'); await flush(); editSource('The report is ready.'); await advance()
    expect(screen.getByText('Refresh LinguaDesk to use sentence alternatives. You can still edit and copy your text.')).toBeVisible()
    expect(screen.queryByRole('button', { name: /Sentence alternatives:/ })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Copy result' })).toBeEnabled()
    h.stores().rewrite.requestAlternatives?.({ start: 0, length: 20 }); await flush()
    expect(network.alternatives).toHaveLength(0)
    fireEvent.click(screen.getByRole('button', { name: 'Edit result' }))
    fireEvent.change(screen.getByRole('textbox', { name: 'Improved result' }), { target: { value: 'Manual edit.' } })
    expect(screen.getByRole('textbox', { name: 'Improved result' })).toHaveValue('Manual edit.')
  })
})
