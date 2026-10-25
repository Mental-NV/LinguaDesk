import { act, fireEvent, render, screen } from '@testing-library/react'
import { StrictMode, useEffect } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { TranslatePage } from '../../src/translate/TranslatePage'
import { RewritePage } from '../../src/rewrite/RewritePage'
import { WorkspaceStoreProvider } from '../../src/shell/workspaces'
import { useWorkspaceStores, type WorkspaceStores } from '../../src/shell/workspaceStores'
import type { Feature } from '../../src/shell/workspaceModel'

export const FIXED_TIME = '2026-10-25T12:00:00Z'
export const SOURCE = 'Hello, the meeting starts at 14:30. Please go.'
export const RESULT = 'Bună, întâlnirea începe la 14:30. Te rog să mergi.'
export const MODES = ['correctionOnly', 'simple', 'casual', 'business', 'academic', 'enthusiastic', 'friendly', 'confident', 'diplomatic']
export const CAPABILITIES = {
  languages: [{ id: 'en', name: 'English' }, { id: 'ru', name: 'Russian' }, { id: 'ro', name: 'Romanian' }, { id: 'zh', name: 'Chinese' }],
  sourceSelection: { default: 'auto', values: ['auto', 'en', 'ru', 'ro', 'zh'] },
  translation: { maximumSourceCharacters: 5000, overallDeadlineSeconds: 30 },
  rewriting: { maximumSourceCharacters: 2000, overallDeadlineSeconds: 30, modes: MODES.map((id, index) => ({ id, name: ['Correction only', 'Simple', 'Casual', 'Business', 'Academic', 'Enthusiastic', 'Friendly', 'Confident', 'Diplomatic'][index] })), defaultMode: 'correctionOnly' },
}
export const USAGE = { day: '2026-10-25', consumedCharacters: 7500, reservedCharacters: 0, allowanceCharacters: 20000, availableCharacters: 12500, revision: 1, resetAtUtc: '2026-10-26T00:00:00Z', availability: 'available' }
export const json = (body: unknown, status = body && typeof body === 'object' && ('translatedText' in body || 'rewrittenText' in body) ? 201 : 200) => new Response(JSON.stringify(body), { status })
export function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(done => { resolve = done }); return { promise, resolve } }
export async function flush() { await act(async () => { for (let i = 0; i < 12; i++) await Promise.resolve() }) }
export async function advance(ms = 1000) { await act(async () => { await vi.advanceTimersByTimeAsync(ms) }) }
export function installWorkspaceFetch(options: {
  operation?: (body: Record<string, string>, index: number) => Response | Promise<Response>
  usage?: () => Response | Promise<Response>
  status?: () => Response | Promise<Response>
  bootstrap?: () => Response | Promise<Response>
  capabilities?: () => Response | Promise<Response>
} = {}) {
  const operations: Record<string, string>[] = []
  const reads: string[] = []
  let current = { ...USAGE }
  const success = (body: Record<string, string>, text = RESULT) => {
    const count = [...body.source].length
    current = { ...current, consumedCharacters: current.consumedCharacters + count, availableCharacters: current.availableCharacters - count, revision: current.revision + 1 }
    return json({ translatedText: text, rewrittenText: text, admissionDay: current.day, characterCount: count, usage: current })
  }
  vi.stubGlobal('fetch', vi.fn(async (input: unknown, init?: RequestInit) => {
    const url = String(input)
    if (url === '/api/operations' && init?.method === 'POST') {
      const body = JSON.parse(String(init.body)) as Record<string, string>
      operations.push(body)
      return options.operation ? await options.operation(body, operations.length) : success(body)
    }
    reads.push(url)
    if (url === '/api/capabilities') return options.capabilities ? await options.capabilities() : json(CAPABILITIES)
    if (url === '/api/usage') return options.usage ? await options.usage() : json(current)
    if (url === '/api/accounts/antiforgery') return options.bootstrap ? await options.bootstrap() : json({ requestToken: 'test-token' })
    if (url.startsWith('/api/operations/')) return options.status ? await options.status() : json({}, 404)
    throw new Error(`Unexpected test request ${url}`)
  }))
  return { operations, reads, success }
}
export function mountWorkspace(feature: Feature, strict = false) {
  let stores: WorkspaceStores | null = null
  function Probe() { const current = useWorkspaceStores(); useEffect(() => { stores = current }, [current]); return null }
  const tree = (selected: Feature) => {
    const content = <MemoryRouter><WorkspaceStoreProvider><Probe />{selected === 'translation' ? <TranslatePage onSignOut={() => {}} /> : <RewritePage onSignOut={() => {}} />}</WorkspaceStoreProvider></MemoryRouter>
    return strict ? <StrictMode>{content}</StrictMode> : content
  }
  const view = render(tree(feature))
  return { ...view, navigate: (selected: Feature) => view.rerender(tree(selected)), stores: () => stores!, state: () => (feature === 'translation' ? stores!.translate.state : stores!.rewrite.state) }
}
export function editSource(text = SOURCE) { fireEvent.change(screen.getByLabelText('Source text'), { target: { value: text } }) }
export function selectValid(feature: Feature) {
  fireEvent.change(screen.getByLabelText(feature === 'translation' ? 'Source language' : 'Writing language'), { target: { value: 'en' } })
  if (feature === 'translation') fireEvent.change(screen.getByLabelText('Target language'), { target: { value: 'ro' } })
}
export const resultName = (feature: Feature) => feature === 'translation' ? 'Translation result' : 'Improved result'
