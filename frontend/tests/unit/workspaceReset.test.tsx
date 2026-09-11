import { useEffect } from 'react'
import { render, screen, waitFor, within } from '@testing-library/react'
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
 * M032 reset-dialog/teardown checks (AC-001/002/003/004/008): exact dialog
 * copy and focus, empty-workspace bypass, confirm clearing of both pages with
 * zero operation posts, footer lifetime copy, editor autocomplete exclusion
 * and pagehide/pageshow clearing. Synthetic dispatch proves handlers only;
 * real restoration is proven by the published E2E suite.
 */

const TOK_A = 'Hello, the meeting starts at 14:30. Please go.'
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

function installFetch() {
  const operations: string[] = []
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
        operations.push(url)
        return new Response(JSON.stringify({ status: 'succeeded' }), { status: 201 })
      }
      return new Response('{}', { status: 200 })
    }),
  )
  return { operations }
}

function renderTranslate() {
  return render(
    <MemoryRouter initialEntries={['/translate']}>
      <WorkspaceStoreProvider>
        <TranslatePage onSignOut={() => {}} />
      </WorkspaceStoreProvider>
    </MemoryRouter>,
  )
}

function captureStores() {
  const captured: { current: WorkspaceStores | null } = { current: null }
  function Probe() {
    const stores = useWorkspaceStores()
    useEffect(() => {
      captured.current = stores
    }, [stores])
    return null
  }
  const view = render(
    <MemoryRouter initialEntries={['/translate']}>
      <WorkspaceStoreProvider>
        <Probe />
        <TranslatePage onSignOut={() => {}} />
      </WorkspaceStoreProvider>
    </MemoryRouter>,
  )
  return { captured, view }
}

describe('workspace reset dialog', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    window.localStorage.clear()
    window.sessionStorage.clear()
  })

  it('resets an empty workspace immediately with no dialog and focuses source', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch()
    renderTranslate()
    await screen.findByLabelText('Source language')

    await user.click(screen.getByRole('button', { name: 'Start new workspace' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Source text')).toHaveValue('')
    expect(screen.getByLabelText('Source text')).toHaveFocus()
    expect(operations).toHaveLength(0)
  })

  it('opens the exact dialog with Cancel focused and preserves state on Cancel', async () => {
    const user = userEvent.setup()
    installFetch()
    renderTranslate()
    await screen.findByLabelText('Source language')
    await user.selectOptions(screen.getByLabelText('Source language'), 'en')
    await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
    await user.type(screen.getByLabelText('Source text'), TOK_A)

    await user.click(screen.getByRole('button', { name: 'Start new workspace' }))
    const dialog = screen.getByRole('dialog')
    expect(dialog).toBeVisible()
    expect(
      screen.getByRole('heading', { name: 'Start a new workspace?' }),
    ).toBeVisible()
    expect(
      screen.getByText(
        'Source text, results, and workspace settings in this tab will be cleared. This cannot be undone.',
      ),
    ).toBeVisible()
    expect(
      within(dialog).getByRole('button', { name: 'Start new workspace' }),
    ).toBeVisible()
    expect(within(dialog).getByRole('button', { name: 'Cancel' })).toHaveFocus()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Source text')).toHaveValue(TOK_A)
    expect(screen.getByLabelText('Target language')).toHaveValue('ro')
    expect(screen.getByRole('button', { name: 'Start new workspace' })).toHaveFocus()
  })

  it('confirm clears both pages with zero operations and focuses Source text', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch()
    const { captured } = captureStores()
    await screen.findByLabelText('Source language')
    await user.selectOptions(screen.getByLabelText('Source language'), 'en')
    await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
    await user.type(screen.getByLabelText('Source text'), TOK_A)
    const stores = captured.current
    if (stores === null) throw new Error('workspace stores are required')
    const { act } = await import('react')
    act(() => {
      stores.rewrite.dispatch({ type: 'sourceChanged', source: W_OK })
    })
    expect(captured.current?.rewrite.state.source).toBe(W_OK)

    await user.click(screen.getByRole('button', { name: 'Start new workspace' }))
    const dialog = screen.getByRole('dialog')
    expect(dialog).toBeVisible()
    await user.click(
      within(dialog).getByRole('button', { name: 'Start new workspace' }),
    )

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Source text')).toHaveValue('')
    expect(screen.getByLabelText('Source text')).toHaveFocus()
    expect(screen.getByLabelText('Source language')).toHaveValue('auto')
    expect(screen.getByLabelText('Target language')).toHaveValue('')
    expect(captured.current?.rewrite.state.source).toBe('')
    expect(operations).toHaveLength(0)
  })

  it('shows the lifetime footer and keeps editors out of autocomplete and storage', async () => {
    const user = userEvent.setup()
    installFetch()
    renderTranslate()
    await screen.findByLabelText('Source language')
    await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
    await user.type(screen.getByLabelText('Source text'), TOK_A)

    expect(
      screen.getByText(
        'Text and settings are cleared when this workspace ends, including refresh, sign-out, or session expiry.',
      ),
    ).toBeVisible()
    expect(screen.getByLabelText('Source text')).toHaveAttribute('autocomplete', 'off')
    expect(window.localStorage.length).toBe(0)
    expect(window.sessionStorage.length).toBe(0)
  })

  it('clears both pages on pagehide and on persisted pageshow', async () => {
    const user = userEvent.setup()
    installFetch()
    const { captured, view } = captureStores()
    await screen.findByLabelText('Source language')
    await user.type(screen.getByLabelText('Source text'), TOK_A)
    const stores = captured.current
    if (stores === null) throw new Error('workspace stores are required')
    const { act } = await import('react')
    act(() => {
      stores.rewrite.dispatch({ type: 'sourceChanged', source: W_OK })
    })

    window.dispatchEvent(new Event('pagehide'))
    await waitFor(() => expect(screen.getByLabelText('Source text')).toHaveValue(''))
    expect(captured.current?.rewrite.state.source).toBe('')

    await user.type(screen.getByLabelText('Source text'), TOK_A)
    const restored = new Event('pageshow')
    Object.defineProperty(restored, 'persisted', { value: true })
    window.dispatchEvent(restored)
    await waitFor(() => expect(screen.getByLabelText('Source text')).toHaveValue(''))
    view.unmount()
  })

  it('renders the reset control and footer on the rewriting page', async () => {
    installFetch()
    render(
      <MemoryRouter initialEntries={['/rewrite']}>
        <WorkspaceStoreProvider>
          <RewritePage onSignOut={() => {}} />
        </WorkspaceStoreProvider>
      </MemoryRouter>,
    )
    await screen.findByLabelText('Writing mode')
    expect(
      screen.getByRole('button', { name: 'Start new workspace' }),
    ).toBeVisible()
    expect(
      screen.getByText(
        'Text and settings are cleared when this workspace ends, including refresh, sign-out, or session expiry.',
      ),
    ).toBeVisible()
    expect(screen.getByLabelText('Source text')).toHaveAttribute('autocomplete', 'off')
  })
})
