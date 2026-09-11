import { useEffect } from 'react'
import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { RewritePage } from '../../src/rewrite/RewritePage'
import { WorkspaceStoreProvider } from '../../src/shell/workspaces'
import {
  useWorkspaceStores,
  type WorkspaceStores,
} from '../../src/shell/workspaceStores'
import { TranslatePage } from '../../src/translate/TranslatePage'

/**
 * M030 lifted-store checks (AC-001/AC-002 wiring plus the additive
 * submitAborted teardown action). Reducer stale/manual-edit guards keep
 * their own suites; these cases prove the store owns cross-page state and
 * hidden settlement while navigation submits nothing.
 */

const TOK_A = 'Hello, the meeting starts at 14:30. Please go.'
const RO_FIXTURE = 'Bună, întâlnirea începe la 14:30. Te rog să mergi.'
const W_OK = 'The report is really ready. We sends it today.'

const modeEntries = [
  { id: 'correctionOnly', name: 'Correction only' },
  { id: 'friendly', name: 'Friendly' },
]

const capabilitiesBody = {
  serverTimeUtc: '2026-09-11T08:30:00Z',
  languages: [
    { id: 'en', name: 'English' },
    { id: 'ru', name: 'Russian' },
    { id: 'ro', name: 'Romanian' },
    { id: 'zh', name: 'Chinese' },
  ],
  sourceSelection: { default: 'auto', values: ['auto', 'en', 'ru', 'ro', 'zh'] },
  chineseScriptPolicy: { acceptedInput: ['simplified', 'traditional'], output: 'simplified' },
  countingPolicy: {
    id: 'unicode-scalar-v1',
    unit: 'unicodeScalar',
    normalization: 'none',
    lineEndings: 'preserve',
    invalidUnicode: 'reject',
    emptyOrWhitespace: 'reject',
    whitespaceCodePointRanges: ['0009-000D'],
  },
  translation: {
    maximumSourceCharacters: 5000,
    oversizeHandling: 'rejectWhole',
    overallDeadlineSeconds: 30,
    targetRequired: true,
    supportedDirections: [{ source: 'en', target: 'ro' }],
  },
  rewriting: {
    maximumSourceCharacters: 2000,
    oversizeHandling: 'rejectWhole',
    overallDeadlineSeconds: 30,
    defaultMode: 'correctionOnly',
    modes: modeEntries,
  },
  operationIdentity: {
    format: 'uuidV7',
    validForSeconds: 86400,
    maximumFutureSkewSeconds: 300,
  },
}

const usageBody = {
  day: '2026-09-11',
  resetAtUtc: '2026-09-12T00:00:00Z',
  consumedCharacters: 7500,
  reservedCharacters: 0,
  allowanceCharacters: 20000,
  availableCharacters: 12500,
  revision: 4,
  availability: 'available',
}

function translationSuccessBody() {
  return {
    operationId: '0193a5b2-2c1d-7a11-9a22-334455667788',
    family: 'translation',
    status: 'succeeded',
    translatedText: RO_FIXTURE,
    characterCount: 46,
    admissionDay: '2026-09-11',
    deadlineUtc: '2026-09-11T08:30:30Z',
    serverTimeUtc: '2026-09-11T08:30:00Z',
    usage: { ...usageBody, consumedCharacters: 7546, availableCharacters: 12454 },
  }
}

function installFetch(operationsHandler: (call: number) => Response | Promise<Response>) {
  const operations: Array<{ body: Record<string, unknown> }> = []
  let operationCalls = 0
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: unknown, init?: RequestInit) => {
      const url = String(input)
      if (url === '/api/capabilities') {
        return new Response(JSON.stringify(capabilitiesBody), { status: 200 })
      }
      if (url === '/api/accounts/antiforgery') {
        return new Response(JSON.stringify({ requestToken: 'test-token', headerName: 'X' }), {
          status: 200,
        })
      }
      if (url === '/api/usage') {
        return new Response(JSON.stringify(usageBody), { status: 200 })
      }
      if (url === '/api/operations' && (init?.method ?? 'GET') === 'POST') {
        operationCalls += 1
        operations.push({ body: JSON.parse(String(init?.body)) as Record<string, unknown> })
        return await operationsHandler(operationCalls)
      }
      return new Response('{}', { status: 200 })
    }),
  )
  return { operations }
}

function captureStores() {
  const captured: { current: WorkspaceStores | null } = { current: null }
  function Probe({ onStores }: { readonly onStores: (stores: WorkspaceStores | null) => void }) {
    const stores = useWorkspaceStores()
    useEffect(() => {
      onStores(stores)
    }, [stores, onStores])
    return null
  }
  function capture(element: React.ReactElement) {
    return render(
      <MemoryRouter>
        <WorkspaceStoreProvider>
          <Probe onStores={(stores) => { captured.current = stores }} />
          {element}
        </WorkspaceStoreProvider>
      </MemoryRouter>,
    )
  }
  return { captured, capture }
}

// Pages own router navigation (M032 reset confirm), so component renders
// provide a memory router alongside the lifted provider.
function renderWorkspace(element: React.ReactElement) {
  return render(
    <MemoryRouter>
      <WorkspaceStoreProvider>{element}</WorkspaceStoreProvider>
    </MemoryRouter>,
  )
}

function rerenderWorkspace(
  view: ReturnType<typeof render>,
  element: React.ReactElement,
) {
  view.rerender(
    <MemoryRouter>
      <WorkspaceStoreProvider>{element}</WorkspaceStoreProvider>
    </MemoryRouter>,
  )
}

describe('lifted workspace store', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('preserves each page text across navigation with zero operations', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch(() => {
      throw new Error('navigation must not submit')
    })
    const view = renderWorkspace(<TranslatePage onSignOut={() => {}} />)
    await screen.findByLabelText('Source language')
    await user.selectOptions(screen.getByLabelText('Source language'), 'en')
    await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
    await user.type(screen.getByLabelText('Source text'), TOK_A)
    expect(operations).toHaveLength(0)

    rerenderWorkspace(view, <RewritePage onSignOut={() => {}} />)
    await screen.findByLabelText('Writing mode')
    expect(screen.getByLabelText('Source text')).toHaveValue('')
    await user.type(screen.getByLabelText('Source text'), W_OK)
    expect(operations).toHaveLength(0)

    rerenderWorkspace(view, <TranslatePage onSignOut={() => {}} />)
    await screen.findByLabelText('Source language')
    expect(screen.getByLabelText('Source text')).toHaveValue(TOK_A)
    expect(operations).toHaveLength(0)
    view.unmount()
  })

  it('settles a hidden operation into its owning page without touching the visible page', async () => {
    const user = userEvent.setup()
    let release!: (response: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    const { operations } = installFetch(() => gate)
    const view = renderWorkspace(<TranslatePage onSignOut={() => {}} />)
    await screen.findByText(/7,500 of 20,000 characters used/)
    await user.selectOptions(screen.getByLabelText('Source language'), 'en')
    await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
    await user.type(screen.getByLabelText('Source text'), TOK_A)
    await user.click(screen.getByRole('button', { name: 'Translate' }))
    await waitFor(() => expect(operations).toHaveLength(1))

    rerenderWorkspace(view, <RewritePage onSignOut={() => {}} />)
    await screen.findByLabelText('Writing mode')
    expect(screen.getByLabelText('Source text')).toHaveValue('')
    expect(screen.queryByLabelText('Result')).not.toBeInTheDocument()

    release(new Response(JSON.stringify(translationSuccessBody()), { status: 201 }))
    await waitFor(() =>
      expect(screen.getByLabelText('Source text')).toHaveValue(''),
    )
    expect(screen.queryByLabelText('Result')).not.toBeInTheDocument()
    expect(operations).toHaveLength(1)

    rerenderWorkspace(view, <TranslatePage onSignOut={() => {}} />)
    expect(await screen.findByLabelText('Result')).toHaveValue(RO_FIXTURE)
    await screen.findByText(/7,546 of 20,000 characters used/)
    expect(operations).toHaveLength(1)
    view.unmount()
  })

  it('releases the busy phase on abort without touching text and ignores stale revisions', () => {
    const { captured, capture } = captureStores()
    const view = capture(<></>)
    const stores = captured.current
    if (stores === null) throw new Error('workspace stores are required')

    act(() => {
      stores.translate.dispatch({ type: 'sourceChanged', source: 'hello' })
      stores.translate.dispatch({
        type: 'capabilitiesLoaded',
        capabilities: {
          maximumSourceCharacters: 5000,
          sourceDefault: 'auto',
          sourceValues: ['auto', 'en'],
          languages: { en: 'English' },
          targetValues: ['ro'],
        },
      })
      stores.translate.dispatch({ type: 'submitRequested' })
    })
    expect(captured.current?.translate.state.phase).toBe('submitting')

    act(() => {
      stores.translate.dispatch({ type: 'submitAborted', revision: 0 })
    })
    expect(captured.current?.translate.state.phase).toBe('submitting')

    act(() => {
      stores.translate.abortFlights()
    })
    expect(captured.current?.translate.state.phase).toBe('idle')
    expect(captured.current?.translate.state.source).toBe('hello')

    act(() => {
      stores.rewrite.dispatch({ type: 'sourceChanged', source: 'rewrite draft' })
    })
    expect(captured.current?.rewrite.state.source).toBe('rewrite draft')
    expect(captured.current?.translate.state.source).toBe('hello')

    stores.translate.saveScroll(120)
    expect(stores.translate.readSavedScroll()).toBe(120)
    expect(stores.rewrite.readSavedScroll()).toBe(0)
    view.unmount()
  })

  it('suppresses a late settlement after abort and keeps usage untouched', async () => {
    const user = userEvent.setup()
    let release!: (response: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    installFetch(() => gate)
    const { captured, capture } = captureStores()
    const view = capture(<TranslatePage onSignOut={() => {}} />)
    await screen.findByText(/7,500 of 20,000 characters used/)
    await user.selectOptions(screen.getByLabelText('Source language'), 'en')
    await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
    await user.type(screen.getByLabelText('Source text'), TOK_A)
    await user.click(screen.getByRole('button', { name: 'Translate' }))
    const stores = captured.current
    if (stores === null) throw new Error('workspace stores are required')
    await waitFor(() => expect(captured.current?.translate.state.phase).toBe('submitting'))

    act(() => {
      stores.translate.abortFlights()
    })
    release(new Response(JSON.stringify(translationSuccessBody()), { status: 201 }))
    await waitFor(() => expect(captured.current?.translate.state.phase).toBe('idle'))
    await new Promise((resolve) => setTimeout(resolve, 100))
    expect(screen.queryByLabelText('Result')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Source text')).toHaveValue(TOK_A)
    await screen.findByText(/7,500 of 20,000 characters used/)
    expect(screen.getByRole('button', { name: 'Translate' })).toBeEnabled()
    view.unmount()
  })

  it('resetAll clears both workspaces to defaults, preserves usage and drops scroll', () => {
    const { captured, capture } = captureStores()
    const view = capture(<></>)
    const stores = captured.current
    if (stores === null) throw new Error('workspace stores are required')

    act(() => {
      stores.translate.dispatch({ type: 'sourceChanged', source: TOK_A })
      stores.translate.dispatch({
        type: 'capabilitiesLoaded',
        capabilities: {
          maximumSourceCharacters: 5000,
          sourceDefault: 'auto',
          sourceValues: ['auto', 'en'],
          languages: { en: 'English' },
          targetValues: ['ro'],
        },
      })
      stores.translate.dispatch({
        type: 'usageUpdated',
        usage: {
          consumedCharacters: 7546,
          allowanceCharacters: 20000,
          availableCharacters: 12454,
          resetAtUtc: '2026-09-12T00:00:00Z',
          availability: 'available',
          revision: 4,
        },
        observedAtMs: 1_000,
      })
      stores.rewrite.dispatch({ type: 'sourceChanged', source: W_OK })
      stores.rewrite.dispatch({
        type: 'capabilitiesLoaded',
        capabilities: {
          maximumSourceCharacters: 2000,
          sourceDefault: 'auto',
          sourceValues: ['auto', 'en'],
          languages: { en: 'English' },
          modeDefault: 'correctionOnly',
          modeValues: ['correctionOnly', 'friendly'],
          modeNames: { correctionOnly: 'Correction only', friendly: 'Friendly' },
        },
      })
      stores.rewrite.dispatch({ type: 'modeChanged', value: 'friendly' })
      stores.translate.saveScroll(90)
      stores.rewrite.saveScroll(40)
    })

    act(() => {
      captured.current?.resetAll()
    })

    const translate = captured.current?.translate.state
    expect(translate?.source).toBe('')
    expect(translate?.sourceSelection).toBe('auto')
    expect(translate?.target).toBe('')
    expect(translate?.hasResult).toBe(false)
    expect(translate?.phase).toBe('idle')
    expect(translate?.error).toBeNull()
    expect(translate?.usage?.consumedCharacters).toBe(7546)

    const rewrite = captured.current?.rewrite.state
    expect(rewrite?.source).toBe('')
    expect(rewrite?.mode).toBe('correctionOnly')
    expect(rewrite?.hasResult).toBe(false)
    expect(rewrite?.phase).toBe('idle')

    expect(stores.translate.readSavedScroll()).toBe(0)
    expect(stores.rewrite.readSavedScroll()).toBe(0)
    view.unmount()
  })

  it('resetAll during a pending submit restores no late text and keeps usage', async () => {
    const user = userEvent.setup()
    let release!: (response: Response) => void
    const gate = new Promise<Response>((resolve) => {
      release = resolve
    })
    installFetch(() => gate)
    const { captured, capture } = captureStores()
    const view = capture(<TranslatePage onSignOut={() => {}} />)
    await screen.findByText(/7,500 of 20,000 characters used/)
    await user.selectOptions(screen.getByLabelText('Source language'), 'en')
    await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
    await user.type(screen.getByLabelText('Source text'), TOK_A)
    await user.click(screen.getByRole('button', { name: 'Translate' }))
    const stores = captured.current
    if (stores === null) throw new Error('workspace stores are required')
    await waitFor(() => expect(captured.current?.translate.state.phase).toBe('submitting'))

    act(() => {
      stores.resetAll()
    })
    expect(captured.current?.translate.state.phase).toBe('idle')
    expect(captured.current?.translate.state.source).toBe('')
    expect(captured.current?.translate.state.target).toBe('')

    release(new Response(JSON.stringify(translationSuccessBody()), { status: 201 }))
    await new Promise((resolve) => setTimeout(resolve, 100))
    expect(screen.queryByLabelText('Result')).not.toBeInTheDocument()
    await screen.findByText(/7,500 of 20,000 characters used/)

    // A response dispatched past the abort still cannot restore cleared text.
    const revision = captured.current?.translate.state.requestRevision ?? 0
    act(() => {
      stores.translate.dispatch({
        type: 'submitSucceeded',
        revision: revision - 1,
        translatedText: 'Late text.',
        characterCount: 46,
        usage: {
          consumedCharacters: 7546,
          allowanceCharacters: 20000,
          availableCharacters: 12454,
          resetAtUtc: '2026-09-12T00:00:00Z',
          availability: 'available',
          revision: 5,
        },
        observedAtMs: 2_000,
      })
    })
    expect(captured.current?.translate.state.resultText).toBe('')
    expect(captured.current?.translate.state.hasResult).toBe(false)
    view.unmount()
  })
})
