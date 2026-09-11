import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RewritePage } from '../../src/rewrite/RewritePage'

const W_OK = 'The report is really ready. We sends it today.'
const W_RESULT = 'The report is ready. We send it today.'
const W_LONG = 'a'.repeat(2312)

const modeEntries: Array<{ id: string; name: string; kind: string }> = [
  { id: 'correctionOnly', name: 'Correction only', kind: 'correction' },
  { id: 'simple', name: 'Simple', kind: 'style' },
  { id: 'casual', name: 'Casual', kind: 'style' },
  { id: 'business', name: 'Business', kind: 'style' },
  { id: 'academic', name: 'Academic', kind: 'style' },
  { id: 'enthusiastic', name: 'Enthusiastic', kind: 'tone' },
  { id: 'friendly', name: 'Friendly', kind: 'tone' },
  { id: 'confident', name: 'Confident', kind: 'tone' },
  { id: 'diplomatic', name: 'Diplomatic', kind: 'tone' },
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
  consumedCharacters: 7546,
  reservedCharacters: 0,
  allowanceCharacters: 20000,
  availableCharacters: 12454,
  revision: 4,
  availability: 'available',
}

function successBody(rewrittenText = W_RESULT, consumed = 7546) {
  return {
    operationId: '0193a5b2-2c1d-7a11-9a22-334455667788',
    family: 'rewriting',
    status: 'succeeded',
    rewrittenText,
    characterCount: 46,
    admissionDay: '2026-09-11',
    deadlineUtc: '2026-09-11T08:30:30Z',
    serverTimeUtc: '2026-09-11T08:30:00Z',
    usage: { ...usageBody, consumedCharacters: consumed },
  }
}

function problemBody(status: number, category: string, extra: Record<string, unknown> = {}) {
  return {
    type: 'about:blank',
    title: 'Problem',
    status,
    detail: 'Safe detail without submitted text.',
    category,
    correlationId: 'test-correlation',
    ...extra,
  }
}

interface OperationPost {
  url: string
  body: Record<string, unknown>
  antiforgery: string | null
}

interface FetchOverrides {
  readonly status?: (url: string) => Response | Promise<Response>
  readonly usage?: () => Response | Promise<Response>
}

function installFetch(
  operationsHandler: (call: number) => Response | Promise<Response>,
  overrides: FetchOverrides = {},
) {
  const operations: OperationPost[] = []
  const calls: string[] = []
  let operationCalls = 0
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: unknown, init?: RequestInit) => {
      const url = String(input)
      calls.push(`${init?.method ?? 'GET'} ${url}`)
      if (url === '/api/capabilities') {
        return new Response(JSON.stringify(capabilitiesBody), { status: 200 })
      }
      if (url === '/api/accounts/antiforgery') {
        return new Response(JSON.stringify({ requestToken: 'test-token', headerName: 'X' }), {
          status: 200,
        })
      }
      if (url === '/api/usage') {
        if (overrides.usage !== undefined) return await overrides.usage()
        return new Response(JSON.stringify(usageBody), { status: 200 })
      }
      if (url.startsWith('/api/operations/') && (init?.method ?? 'GET') === 'GET') {
        if (overrides.status !== undefined) return await overrides.status(url)
        return new Response(JSON.stringify({ title: 'Not found' }), { status: 404 })
      }
      if (url === '/api/operations' && (init?.method ?? 'GET') === 'POST') {
        operationCalls += 1
        const headers = new Headers(init?.headers)
        operations.push({
          url,
          body: JSON.parse(String(init?.body)) as Record<string, unknown>,
          antiforgery: headers.get('X-LinguaDesk-Antiforgery'),
        })
        return await operationsHandler(operationCalls)
      }
      return new Response('{}', { status: 200 })
    }),
  )
  return { operations, calls }
}

function jsonResponse(payload: unknown, status: number): Response {
  return new Response(JSON.stringify(payload), { status })
}

async function renderReadyWorkspace() {
  render(<RewritePage onSignOut={() => {}} />)
  await screen.findByLabelText('Writing language')
  await screen.findByText(/7,546 of 20,000 characters used/)
}

async function fillValidWorkspace(user: ReturnType<typeof userEvent.setup>) {
  await user.selectOptions(screen.getByLabelText('Writing language'), 'en')
  await user.type(screen.getByLabelText('Source text'), W_OK)
}

describe('rewrite workspace components', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders selectors, Correction only default and authoritative usage with zero transformation requests', async () => {
    const user = userEvent.setup()
    const { calls } = installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()

    expect(screen.getByLabelText('Writing language')).toHaveValue('auto')
    expect(screen.getByLabelText('Writing mode')).toHaveValue('correctionOnly')
    expect(screen.getByRole('button', { name: 'Rewrite' })).toBeDisabled()

    await user.type(screen.getByLabelText('Source text'), W_OK)
    await new Promise((resolve) => setTimeout(resolve, 350))
    expect(calls.filter((call) => call.includes('/api/operations'))).toHaveLength(0)
    expect(screen.getByText('Ready to process. Select Rewrite.')).toBeVisible()
  })

  it('offers exactly the nine catalog modes with a single selection', async () => {
    installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()

    const mode = screen.getByLabelText('Writing mode')
    expect(within(mode).getAllByRole('option')).toHaveLength(9)
    expect(screen.getByRole('option', { name: 'Correction only' })).toBeVisible()
    expect(screen.getByRole('option', { name: 'Business' })).toBeVisible()
    expect(screen.getByRole('option', { name: 'Friendly' })).toBeVisible()
  })

  it('submits once with revisions and shows the complete result with usage', async () => {
    const user = userEvent.setup()
    let releaseFirst!: (response: Response) => void
    const firstGate = new Promise<Response>((resolve) => {
      releaseFirst = resolve
    })
    const { operations } = installFetch((call) =>
      call === 1 ? firstGate : jsonResponse(successBody(), 201),
    )
    await renderReadyWorkspace()
    await fillValidWorkspace(user)

    const rewrite = screen.getByRole('button', { name: 'Rewrite' })
    expect(rewrite).toBeEnabled()
    await user.click(rewrite)
    await user.click(rewrite)
    await waitFor(() => expect(operations).toHaveLength(1))
    releaseFirst(jsonResponse(successBody(), 201))

    const result = await screen.findByLabelText('Result')
    expect(result).toHaveValue(W_RESULT)
    await waitFor(() => expect(operations).toHaveLength(1))
    expect(operations[0].body.family).toBe('rewriting')
    expect(operations[0].body.source).toBe(W_OK)
    expect(operations[0].body.sourceSelection).toBe('en')
    expect(operations[0].body.mode).toBe('correctionOnly')
    expect(operations[0].body.operationId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/,
    )
    expect(operations[0].antiforgery).toBe('test-token')
    expect(screen.getByText('Up to date')).toBeVisible()
    expect(screen.getByText(/7,546 of 20,000 characters used/)).toBeVisible()
  })

  it('keeps one final mode value with zero requests before explicit Rewrite', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch(() => jsonResponse(successBody('Friendly rewrite.'), 201))
    await renderReadyWorkspace()
    await fillValidWorkspace(user)

    await user.selectOptions(screen.getByLabelText('Writing mode'), 'business')
    await user.selectOptions(screen.getByLabelText('Writing mode'), 'friendly')
    expect(screen.getByLabelText('Writing mode')).toHaveValue('friendly')
    await new Promise((resolve) => setTimeout(resolve, 350))
    expect(operations).toHaveLength(0)

    await user.click(screen.getByRole('button', { name: 'Rewrite' }))
    await waitFor(() => expect(operations).toHaveLength(1))
    expect(operations[0].body.mode).toBe('friendly')
    expect(await screen.findByLabelText('Result')).toHaveValue('Friendly rewrite.')
  })

  it('blocks oversize input with excess 312 and dispatches nothing', async () => {
    installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()
    fireEvent.change(screen.getByLabelText('Source text'), { target: { value: W_LONG } })

    expect(
      await screen.findByText(
        'Rewriting is limited to 2,000 characters. Remove 312 characters to continue.',
      ),
    ).toBeVisible()
    expect(screen.getByRole('button', { name: 'Rewrite' })).toBeDisabled()
    expect(screen.getByLabelText('Source text')).toHaveValue(W_LONG)
  })

  it('keeps manual result edits uncharged and copies the exact edited value', async () => {
    const user = userEvent.setup()
    const writeText = vi.fn(async () => {})
    Object.defineProperty(window.navigator, 'clipboard', {
      value: { writeText },
      configurable: true,
    })
    const { operations } = installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()
    await fillValidWorkspace(user)
    await user.click(screen.getByRole('button', { name: 'Rewrite' }))
    const result = await screen.findByLabelText('Result')

    await user.clear(result)
    await user.type(result, 'Edited value.')
    await new Promise((resolve) => setTimeout(resolve, 250))
    expect(operations).toHaveLength(1)

    await user.click(screen.getByRole('button', { name: 'Copy result' }))
    await waitFor(() => expect(writeText).toHaveBeenCalledWith('Edited value.'))
    expect(await screen.findByText('Result copied to clipboard.')).toBeVisible()
  })

  it('protects edited results from pending responses and replaces them on later submission', async () => {
    const user = userEvent.setup()
    let releaseSecond!: (response: Response) => void
    const secondGate = new Promise<Response>((resolve) => {
      releaseSecond = resolve
    })
    const { operations } = installFetch((call) => {
      if (call === 1) return jsonResponse(successBody(), 201)
      if (call === 2) return secondGate
      return jsonResponse(successBody('Third rewrite.'), 201)
    })
    await renderReadyWorkspace()
    await fillValidWorkspace(user)
    await user.click(screen.getByRole('button', { name: 'Rewrite' }))
    const result = await screen.findByLabelText('Result')

    await user.click(screen.getByRole('button', { name: 'Rewrite' }))
    await waitFor(() => expect(operations).toHaveLength(2))
    await user.clear(result)
    await user.type(result, 'Edited value.')
    releaseSecond(jsonResponse(successBody('Second rewrite.'), 201))
    await waitFor(() =>
      expect(
        screen.getByText('Edited result — the current update won’t replace your changes.'),
      ).toBeVisible(),
    )
    expect(screen.getByLabelText('Result')).toHaveValue('Edited value.')

    await user.click(screen.getByRole('button', { name: 'Rewrite' }))
    await waitFor(() => expect(operations).toHaveLength(3))
    await waitFor(() =>
      expect(screen.getByLabelText('Result')).toHaveValue('Third rewrite.'),
    )
  })

  it('maps server problems to normative messages with Try again only for retryable failures', async () => {
    const user = userEvent.setup()
    const cases: Array<{
      status: number
      body: unknown
      message: string
      retry: boolean
      partial?: boolean
    }> = [
      {
        status: 503,
        body: problemBody(503, 'processingFailure'),
        message: 'We couldn’t process this text. Your text and previous result are safe.',
        retry: true,
      },
      {
        status: 504,
        body: problemBody(504, 'deadlineExceeded'),
        message: 'Processing took too long. Your text and previous result are safe.',
        retry: true,
      },
      {
        status: 429,
        body: problemBody(429, 'userAllowance', { resetAtUtc: '2026-09-12T00:00:00Z' }),
        message: 'Your daily allowance is used up. Try again after 00:00 UTC',
        retry: false,
        partial: true,
      },
      {
        status: 429,
        body: problemBody(429, 'globalAllowance', { resetAtUtc: '2026-09-12T00:00:00Z' }),
        message: 'LinguaDesk’s shared daily allowance is used up. Try again after 00:00 UTC',
        retry: false,
        partial: true,
      },
      {
        status: 503,
        body: problemBody(503, 'monetarySuspension'),
        message:
          'LinguaDesk processing is temporarily unavailable because its service budget has been reached. Your text is safe. Try again after service resumes.',
        retry: false,
      },
      {
        status: 422,
        body: problemBody(422, 'inputEligibility', { reason: 'mixed' }),
        message:
          'This text contains too much unsupported or mixed-language content. Use one main language: English, Russian, Romanian, or Chinese.',
        retry: false,
      },
      {
        status: 422,
        body: problemBody(422, 'inputEligibility', { reason: 'uncertain' }),
        message: 'Choose the source language to continue.',
        retry: false,
      },
      {
        status: 401,
        body: problemBody(401, 'authenticationRequired'),
        message: 'Sign in to continue. Your text was not processed.',
        retry: false,
      },
      {
        status: 403,
        body: {},
        message: 'Verify your email to use Translation and Rewriting.',
        retry: false,
      },
    ]
    for (const testCase of cases) {
      vi.unstubAllGlobals()
      const { operations } = installFetch(() => jsonResponse(testCase.body, testCase.status))
      const { unmount } = render(<RewritePage onSignOut={() => {}} />)
      await screen.findByLabelText('Writing language')
      await fillValidWorkspace(user)
      await user.click(screen.getByRole('button', { name: 'Rewrite' }))
      if (testCase.partial === true) {
        expect(
          await screen.findByText(
            (_, element) =>
              element?.tagName === 'P' && (element?.textContent?.includes(testCase.message) ?? false),
          ),
        ).toBeVisible()
      } else {
        expect(await screen.findByText(testCase.message)).toBeVisible()
      }
      expect(operations).toHaveLength(1)
      if (testCase.retry) {
        expect(screen.getByRole('button', { name: 'Try again' })).toBeVisible()
      } else {
        expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
      }
      unmount()
    }
  })

  it('retries with a new operation key after a definitive failure', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch((call) =>
      call === 1
        ? jsonResponse(problemBody(503, 'processingFailure'), 503)
        : jsonResponse(successBody(), 201),
    )
    await renderReadyWorkspace()
    await fillValidWorkspace(user)
    await user.click(screen.getByRole('button', { name: 'Rewrite' }))

    await user.click(await screen.findByRole('button', { name: 'Try again' }))
    await screen.findByLabelText('Result')
    await waitFor(() => expect(operations).toHaveLength(2))
    expect(operations[0].body.operationId).not.toBe(operations[1].body.operationId)
    expect(screen.getByLabelText('Source text')).toHaveValue(W_OK)
  })

  it('ignores activation during IME composition and never queues a request', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()
    await user.selectOptions(screen.getByLabelText('Writing language'), 'en')
    const source = screen.getByLabelText('Source text')
    await user.type(source, W_OK)
    fireEvent.compositionStart(source)
    await user.click(screen.getByRole('button', { name: 'Rewrite' }))
    fireEvent.compositionEnd(source)
    await new Promise((resolve) => setTimeout(resolve, 250))
    expect(operations).toHaveLength(0)
  })

  it('shows loading and failed capability states with a retry action', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => new Response('{}', { status: 500 })),
    )
    render(<RewritePage onSignOut={() => {}} />)
    expect(await screen.findByText('Rewriting settings could not be loaded.')).toBeVisible()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeVisible()
  })

  it('exposes a single main landmark through the shell heading', async () => {
    installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()
    const usageSection = screen.getByRole('region', { name: 'Usage' })
    expect(usageSection).toBeVisible()
    expect(within(usageSection).getByText(/characters used/)).toBeVisible()
  })

  it('shows unknown-outcome recovery with Check status and resolves no-record read-only', async () => {
    const user = userEvent.setup()
    const { operations, calls } = installFetch(async () => {
      throw new TypeError('fetch failed')
    })
    await renderReadyWorkspace()
    await fillValidWorkspace(user)
    await user.click(screen.getByRole('button', { name: 'Rewrite' }))

    expect(
      await screen.findByText(
        'We couldn’t confirm whether this request completed. Your text is safe. Check its status before trying again.',
      ),
    ).toBeVisible()
    expect(screen.getByRole('button', { name: 'Check status' })).toBeVisible()
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Rewrite' })).toBeDisabled()
    expect(screen.getByLabelText('Source text')).toHaveValue(W_OK)
    await waitFor(() => expect(operations).toHaveLength(1))

    await user.click(screen.getByRole('button', { name: 'Check status' }))
    expect(
      await screen.findByText(
        'We found no record of this request, so its outcome is unknown. Your text is safe. You can submit it as a new request.',
      ),
    ).toBeVisible()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeVisible()
    expect(screen.getByRole('button', { name: 'Rewrite' })).toBeEnabled()
    // The status read issues exactly one GET and zero operation posts.
    expect(operations).toHaveLength(1)
    expect(calls.filter((call) => call.startsWith('GET /api/operations/'))).toHaveLength(1)
  })

  it('reports an interrupted outcome with zero charge and retry', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch(
      async () => {
        throw new TypeError('fetch failed')
      },
      {
        status: () =>
          jsonResponse(
            {
              operationId: '0193a5b2-2c1d-7a11-9a22-334455667788',
              family: 'rewriting',
              status: 'interrupted',
              characterCount: 0,
              admissionDay: '2026-09-11',
              deadlineUtc: '2026-09-11T08:30:30Z',
              outputAvailable: false,
              serverTimeUtc: '2026-09-11T08:30:00Z',
              usage: usageBody,
            },
            200,
          ),
      },
    )
    await renderReadyWorkspace()
    await fillValidWorkspace(user)
    await user.click(screen.getByRole('button', { name: 'Rewrite' }))
    await screen.findByRole('button', { name: 'Check status' })

    await user.click(screen.getByRole('button', { name: 'Check status' }))
    expect(
      await screen.findByText(
        'We couldn’t process this text. Your text and previous result are safe.',
      ),
    ).toBeVisible()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeVisible()
    expect(operations).toHaveLength(1)
  })

  it('refreshes usage read-only and reports an unavailable update', async () => {
    const user = userEvent.setup()
    let usageFailing = false
    const { operations } = installFetch(() => jsonResponse(successBody(), 201), {
      usage: () =>
        usageFailing
          ? Promise.resolve(new Response('{}', { status: 503 }))
          : Promise.resolve(jsonResponse(usageBody, 200)),
    })
    await renderReadyWorkspace()
    await fillValidWorkspace(user)
    await user.click(screen.getByRole('button', { name: 'Rewrite' }))
    await screen.findByLabelText('Result')

    const postsBefore = operations.length
    await user.click(screen.getByRole('button', { name: 'Refresh usage' }))
    await waitFor(() =>
      expect(screen.getByRole('region', { name: 'Usage' })).toHaveTextContent(
        /7,546 of 20,000 characters used/,
      ),
    )
    expect(operations.length).toBe(postsBefore)

    usageFailing = true
    await user.click(screen.getByRole('button', { name: 'Refresh usage' }))
    expect(await screen.findByText('Usage update unavailable')).toBeVisible()
    expect(operations.length).toBe(postsBefore)
    expect(screen.getByLabelText('Result')).toHaveValue(W_RESULT)

    usageFailing = false
    await user.click(screen.getByRole('button', { name: 'Refresh usage' }))
    await waitFor(() => expect(screen.queryByText('Usage update unavailable')).not.toBeInTheDocument())
    expect(operations.length).toBe(postsBefore)
  })
})
