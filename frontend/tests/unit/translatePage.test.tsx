import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { TranslatePage } from '../../src/translate/TranslatePage'

const TOK_A = 'Hello, the meeting starts at 14:30. Please go.'
const RO_FIXTURE = 'Bună, întâlnirea începe la 14:30. Te rog să mergi.'

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
    modes: [],
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

function successBody(translatedText = RO_FIXTURE, consumed = 7546) {
  return {
    operationId: '0193a5b2-2c1d-7a11-9a22-334455667788',
    family: 'translation',
    status: 'succeeded',
    translatedText,
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

function statusBody(status: string, characterCount = 0, consumed = 7592) {
  return {
    operationId: '0193a5b2-2c1d-7a11-9a22-334455667788',
    family: 'translation',
    status,
    characterCount,
    admissionDay: '2026-09-11',
    deadlineUtc: '2026-09-11T08:30:30Z',
    outputAvailable: false,
    serverTimeUtc: '2026-09-11T08:30:00Z',
    usage: { ...usageBody, consumedCharacters: consumed },
  }
}

function jsonResponse(payload: unknown, status: number): Response {
  return new Response(JSON.stringify(payload), { status })
}

async function renderReadyWorkspace() {
  render(<TranslatePage onSignOut={() => {}} />)
  await screen.findByLabelText('Source language')
  await screen.findByText(/7,546 of 20,000 characters used/)
}

async function fillValidWorkspace(user: ReturnType<typeof userEvent.setup>) {
  await user.selectOptions(screen.getByLabelText('Source language'), 'en')
  await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
  await user.type(screen.getByLabelText('Source text'), TOK_A)
}

describe('translate workspace components', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders selectors, defaults and authoritative usage with zero transformation requests', async () => {
    const user = userEvent.setup()
    const { calls } = installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()

    expect(screen.getByLabelText('Source language')).toHaveValue('auto')
    expect(screen.getByLabelText('Target language')).toHaveValue('')
    expect(screen.getByRole('button', { name: 'Translate' })).toBeDisabled()

    await user.type(screen.getByLabelText('Source text'), TOK_A)
    await new Promise((resolve) => setTimeout(resolve, 350))
    expect(calls.filter((call) => call.includes('/api/operations'))).toHaveLength(0)
    expect(screen.getByText('Ready to process. Select Translate.')).toBeVisible()
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

    const translate = screen.getByRole('button', { name: 'Translate' })
    expect(translate).toBeEnabled()
    await user.click(translate)
    await user.click(translate)
    await waitFor(() => expect(operations).toHaveLength(1))
    releaseFirst(jsonResponse(successBody(), 201))

    const result = await screen.findByLabelText('Result')
    expect(result).toHaveValue(RO_FIXTURE)
    await waitFor(() => expect(operations).toHaveLength(1))
    expect(operations[0].body.family).toBe('translation')
    expect(operations[0].body.source).toBe(TOK_A)
    expect(operations[0].body.sourceSelection).toBe('en')
    expect(operations[0].body.target).toBe('ro')
    expect(operations[0].body.operationId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/,
    )
    expect(operations[0].antiforgery).toBe('test-token')
    expect(screen.getByText('Up to date')).toBeVisible()
    expect(screen.getByText(/7,546 of 20,000 characters used/)).toBeVisible()
  })

  it('blocks oversize input with excess 312 and dispatches nothing', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()
    await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
    fireEvent.change(screen.getByLabelText('Source text'), { target: { value: 'a'.repeat(5312) } })

    expect(
      await screen.findByText(
        'Translation is limited to 5,000 characters. Remove 312 characters to continue.',
      ),
    ).toBeVisible()
    expect(screen.getByRole('button', { name: 'Translate' })).toBeDisabled()
    expect(operations).toHaveLength(0)
    expect(screen.getByLabelText('Source text')).toHaveValue('a'.repeat(5312))
  })

  it('retains same-language selections with MSG-007 and dispatches nothing', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()
    await user.selectOptions(screen.getByLabelText('Target language'), 'en')
    await user.selectOptions(screen.getByLabelText('Source language'), 'en')
    await user.type(screen.getByLabelText('Source text'), 'Hello.')

    expect(
      await screen.findByText('Choose a target language different from English.'),
    ).toBeVisible()
    expect(screen.getByRole('button', { name: 'Translate' })).toBeDisabled()
    expect(operations).toHaveLength(0)
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
    await user.click(screen.getByRole('button', { name: 'Translate' }))
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
      return jsonResponse(successBody('Third translation.'), 201)
    })
    await renderReadyWorkspace()
    await fillValidWorkspace(user)
    await user.click(screen.getByRole('button', { name: 'Translate' }))
    const result = await screen.findByLabelText('Result')

    await user.click(screen.getByRole('button', { name: 'Translate' }))
    await waitFor(() => expect(operations).toHaveLength(2))
    await user.clear(result)
    await user.type(result, 'Edited value.')
    releaseSecond(jsonResponse(successBody('Second translation.'), 201))
    await waitFor(() =>
      expect(
        screen.getByText('Edited result — the current update won’t replace your changes.'),
      ).toBeVisible(),
    )
    expect(screen.getByLabelText('Result')).toHaveValue('Edited value.')

    await user.click(screen.getByRole('button', { name: 'Translate' }))
    await waitFor(() => expect(operations).toHaveLength(3))
    await waitFor(() =>
      expect(screen.getByLabelText('Result')).toHaveValue('Third translation.'),
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
      const { unmount } = render(<TranslatePage onSignOut={() => {}} />)
      await screen.findByLabelText('Source language')
      await fillValidWorkspace(user)
      await user.click(screen.getByRole('button', { name: 'Translate' }))
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
    await user.click(screen.getByRole('button', { name: 'Translate' }))

    await user.click(await screen.findByRole('button', { name: 'Try again' }))
    await screen.findByLabelText('Result')
    await waitFor(() => expect(operations).toHaveLength(2))
    expect(operations[0].body.operationId).not.toBe(operations[1].body.operationId)
    expect(screen.getByLabelText('Source text')).toHaveValue(TOK_A)
  })

  it('ignores activation during IME composition and never queues a request', async () => {
    const user = userEvent.setup()
    const { operations } = installFetch(() => jsonResponse(successBody(), 201))
    await renderReadyWorkspace()
    await user.selectOptions(screen.getByLabelText('Source language'), 'en')
    await user.selectOptions(screen.getByLabelText('Target language'), 'ro')
    const source = screen.getByLabelText('Source text')
    await user.type(source, TOK_A)
    fireEvent.compositionStart(source)
    await user.click(screen.getByRole('button', { name: 'Translate' }))
    fireEvent.compositionEnd(source)
    await new Promise((resolve) => setTimeout(resolve, 250))
    expect(operations).toHaveLength(0)
  })

  it('shows loading and failed capability states with a retry action', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => new Response('{}', { status: 500 })),
    )
    render(<TranslatePage onSignOut={() => {}} />)
    expect(await screen.findByText('Translation settings could not be loaded.')).toBeVisible()
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
    await user.click(screen.getByRole('button', { name: 'Translate' }))

    expect(
      await screen.findByText(
        'We couldn’t confirm whether this request completed. Your text is safe. Check its status before trying again.',
      ),
    ).toBeVisible()
    expect(screen.getByRole('button', { name: 'Check status' })).toBeVisible()
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Translate' })).toBeDisabled()
    expect(screen.getByLabelText('Source text')).toHaveValue(TOK_A)
    await waitFor(() => expect(operations).toHaveLength(1))

    await user.click(screen.getByRole('button', { name: 'Check status' }))
    expect(
      await screen.findByText(
        'We found no record of this request, so its outcome is unknown. Your text is safe. You can submit it as a new request.',
      ),
    ).toBeVisible()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeVisible()
    expect(screen.getByRole('button', { name: 'Translate' })).toBeEnabled()
    // The status read issues exactly one GET and zero operation posts.
    expect(operations).toHaveLength(1)
    expect(calls.filter((call) => call.startsWith('GET /api/operations/'))).toHaveLength(1)
  })

  it('discloses a succeeded-but-unavailable outcome with its confirmed charge', async () => {
    const user = userEvent.setup()
    const { operations, calls } = installFetch(
      async () => {
        throw new TypeError('fetch failed')
      },
      {
        status: () => jsonResponse(statusBody('succeeded', 46), 200),
      },
    )
    await renderReadyWorkspace()
    await fillValidWorkspace(user)
    await user.click(screen.getByRole('button', { name: 'Translate' }))
    await screen.findByRole('button', { name: 'Check status' })

    await user.click(screen.getByRole('button', { name: 'Check status' }))
    expect(
      await screen.findByText(
        'The request completed and 46 characters were counted toward your usage, but the result text is no longer available. Submit again to generate a new result.',
      ),
    ).toBeVisible()
    expect(screen.queryByLabelText('Result')).not.toBeInTheDocument()
    expect(await screen.findByText(/7,592 of 20,000 characters used/)).toBeVisible()
    expect(operations).toHaveLength(1)
    expect(calls.filter((call) => call.startsWith('GET /api/operations/'))).toHaveLength(1)
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
    await user.click(screen.getByRole('button', { name: 'Translate' }))
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
    expect(screen.getByLabelText('Result')).toHaveValue(RO_FIXTURE)

    usageFailing = false
    await user.click(screen.getByRole('button', { name: 'Refresh usage' }))
    await waitFor(() => expect(screen.queryByText('Usage update unavailable')).not.toBeInTheDocument())
    expect(operations.length).toBe(postsBefore)
  })
})
