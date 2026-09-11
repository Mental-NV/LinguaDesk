import {
  fetchOperationStatus as fetchRewritingOperationStatus,
  OPERATIONS_PATH as REWRITING_OPERATIONS_PATH,
} from '../../src/api/rewriting'
import {
  fetchOperationStatus as fetchTranslationOperationStatus,
  OPERATIONS_PATH as TRANSLATION_OPERATIONS_PATH,
} from '../../src/api/translation'

const OPERATION_ID = '0193a5b2-2c1d-7a11-9a22-334455667788'

const usageBody = {
  day: '2026-09-11',
  resetAtUtc: '2026-09-12T00:00:00Z',
  consumedCharacters: 7592,
  reservedCharacters: 0,
  allowanceCharacters: 20000,
  availableCharacters: 12408,
  revision: 5,
  availability: 'available',
}

function statusBody(status: string) {
  return {
    operationId: OPERATION_ID,
    family: 'translation',
    status,
    characterCount: status === 'succeeded' ? 46 : 0,
    admissionDay: '2026-09-11',
    deadlineUtc: '2026-09-11T08:30:30Z',
    outputAvailable: false,
    serverTimeUtc: '2026-09-11T08:30:00Z',
    usage: usageBody,
  }
}

function jsonResponse(payload: unknown, status: number): Response {
  return new Response(JSON.stringify(payload), { status })
}

describe('operation status reads', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it.each([
    ['translation', fetchTranslationOperationStatus, TRANSLATION_OPERATIONS_PATH],
    ['rewriting', fetchRewritingOperationStatus, REWRITING_OPERATIONS_PATH],
  ])(
    'maps every terminal status through the real generated shape for %s',
    async (_family, fetchStatus, operationsPath) => {
      const calls: string[] = []
      vi.stubGlobal(
        'fetch',
        vi.fn(async (input: unknown, init?: RequestInit) => {
          const url = String(input)
          calls.push(`${init?.method ?? 'GET'} ${url}`)
          const status = url.includes('status-succeeded')
            ? 'succeeded'
            : url.includes('status-failed')
              ? 'failed'
              : url.includes('status-interrupted')
                ? 'interrupted'
                : 'pending'
          return jsonResponse(statusBody(status), 200)
        }),
      )

      for (const [suffix, expected] of [
        ['status-succeeded', 'succeeded'],
        ['status-failed', 'failed'],
        ['status-interrupted', 'interrupted'],
        ['status-pending', 'pending'],
      ] as const) {
        const check = await (
          fetchStatus as typeof fetchTranslationOperationStatus
        )(`${OPERATION_ID}-${suffix}`)
        expect(check.kind).toBe('ok')
        if (check.kind === 'ok') {
          expect(check.status).toBe(expected)
          expect(check.admissionDay).toBe('2026-09-11')
          expect(check.usage.consumedCharacters).toBe(7592)
          expect(check.usage.allowanceCharacters).toBe(20000)
        }
      }

      expect(calls).toHaveLength(4)
      for (const call of calls) {
        expect(call.startsWith(`GET ${operationsPath}/`)).toBe(true)
        // The read carries only the operation identity: no source text, no
        // result text and no second paid dispatch.
        expect(call.includes('Hello')).toBe(false)
      }
      expect(calls.some((call) => call.startsWith('POST'))).toBe(false)
    },
  )

  it('maps 404 to unknown-record and 410 to window-expired without claiming a charge', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: unknown) => {
        const url = String(input)
        if (url.endsWith('/missing')) return jsonResponse({ title: 'Not found' }, 404)
        return jsonResponse({ title: 'Gone' }, 410)
      }),
    )

    expect(await fetchTranslationOperationStatus('missing')).toEqual({
      kind: 'unknownRecord',
    })
    expect(await fetchTranslationOperationStatus('expired')).toEqual({
      kind: 'windowExpired',
    })
    expect(await fetchRewritingOperationStatus('missing')).toEqual({
      kind: 'unknownRecord',
    })
    expect(await fetchRewritingOperationStatus('expired')).toEqual({
      kind: 'windowExpired',
    })
  })

  it('maps auth failures and preserves the unknown state on transport problems', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: unknown) => {
        const url = String(input)
        if (url.endsWith('/denied')) return jsonResponse({ title: 'Forbidden' }, 403)
        if (url.endsWith('/signed-out')) return jsonResponse({ title: 'Unauthorized' }, 401)
        if (url.endsWith('/broken')) return jsonResponse({ title: 'Error' }, 503)
        if (url.endsWith('/offline')) throw new TypeError('fetch failed')
        return jsonResponse({ ...statusBody('succeeded'), status: 'bogus' }, 200)
      }),
    )

    expect(await fetchTranslationOperationStatus('signed-out')).toEqual({
      kind: 'unauthorized',
    })
    expect(await fetchTranslationOperationStatus('denied')).toEqual({ kind: 'forbidden' })
    expect(await fetchTranslationOperationStatus('broken')).toEqual({
      kind: 'unavailable',
    })
    expect(await fetchTranslationOperationStatus('offline')).toEqual({
      kind: 'unavailable',
    })
    // An unrecognized terminal value never fabricates a resolution.
    expect(await fetchRewritingOperationStatus('weird')).toEqual({
      kind: 'unavailable',
    })
  })
})
