import {
  MESSAGE_COPIED,
  MESSAGE_COPY_FAILED,
  MESSAGE_OUTDATED,
  MESSAGE_PROCESSING,
  MESSAGE_PROCESSING_FAILURE,
  MESSAGE_READY,
  MESSAGE_RESULT_EDITED,
  MESSAGE_SIGN_IN,
  MESSAGE_STALE_SUCCESS,
  MESSAGE_UNCERTAIN_SOURCE,
  MESSAGE_UNSUPPORTED_CONTENT,
  MESSAGE_UPDATING,
  MESSAGE_UP_TO_DATE,
  MESSAGE_VERIFY_EMAIL,
  createInitialWorkspace,
  describeReadiness,
  formatStaleChargeNotice,
  formatUsageLine,
  mapSubmitProblem,
  selectInlineValidation,
  selectStatusLine,
  rewritingWorkspaceReducer,
  type RewritingCapabilities,
  type RewritingUsage,
  type RewritingWorkspaceState,
} from '../../src/rewrite/rewritingWorkspace'

const W_OK = 'The report is really ready. We sends it today.'
const W_RESULT = 'The report is ready. We send it today.'

const capabilities: RewritingCapabilities = {
  maximumSourceCharacters: 2000,
  sourceDefault: 'auto',
  sourceValues: ['auto', 'en', 'ru', 'ro', 'zh'],
  languages: { en: 'English', ru: 'Russian', ro: 'Romanian', zh: 'Chinese (Simplified output)' },
  modeDefault: 'correctionOnly',
  modeValues: [
    'correctionOnly',
    'simple',
    'casual',
    'business',
    'academic',
    'enthusiastic',
    'friendly',
    'confident',
    'diplomatic',
  ],
  modeNames: {
    correctionOnly: 'Correction only',
    simple: 'Simple',
    casual: 'Casual',
    business: 'Business',
    academic: 'Academic',
    enthusiastic: 'Enthusiastic',
    friendly: 'Friendly',
    confident: 'Confident',
    diplomatic: 'Diplomatic',
  },
}

const usage: RewritingUsage = {
  consumedCharacters: 7546,
  allowanceCharacters: 20000,
  availableCharacters: 12454,
  resetAtUtc: '2026-09-11T00:00:00Z',
  availability: 'available',
  revision: 4,
}

function loaded(source: string, mode = 'correctionOnly'): RewritingWorkspaceState {
  return {
    ...createInitialWorkspace(),
    capabilities,
    source,
    sourceSelection: 'auto',
    mode,
  }
}

describe('rewriting readiness', () => {
  it('blocks empty source with no request', () => {
    const readiness = describeReadiness(loaded('   '), capabilities.maximumSourceCharacters)
    expect(readiness.inputValid).toBe(false)
    expect(readiness.canSubmit).toBe(false)
  })

  it('allows a valid explicit submission', () => {
    const readiness = describeReadiness(loaded(W_OK), capabilities.maximumSourceCharacters)
    expect(readiness.canSubmit).toBe(true)
  })

  it('defaults to Correction only with exactly one mode selected', () => {
    const state = loaded(W_OK)
    expect(state.mode).toBe('correctionOnly')
    const readiness = describeReadiness(state, capabilities.maximumSourceCharacters)
    expect(readiness.selectorsValid).toBe(true)
    expect(readiness.canSubmit).toBe(true)
  })

  it('accepts a single supported mode choice', () => {
    const readiness = describeReadiness(
      loaded(W_OK, 'friendly'),
      capabilities.maximumSourceCharacters,
    )
    expect(readiness.selectorsValid).toBe(true)
    expect(readiness.canSubmit).toBe(true)
  })

  it('reports oversize excess without submitting', () => {
    const state = loaded('a'.repeat(2312))
    const readiness = describeReadiness(state, capabilities.maximumSourceCharacters)
    expect(readiness.excess).toBe(312)
    expect(readiness.canSubmit).toBe(false)
    expect(selectInlineValidation(state)).toBe(
      'Rewriting is limited to 2,000 characters. Remove 312 characters to continue.',
    )
  })

  it('suppresses submission while composing or unsettled', () => {
    const composing = describeReadiness(
      { ...loaded(W_OK), composing: true },
      capabilities.maximumSourceCharacters,
    )
    expect(composing.canSubmit).toBe(false)
    const unsettled = describeReadiness(
      { ...loaded(W_OK), phase: 'submitting' },
      capabilities.maximumSourceCharacters,
    )
    expect(unsettled.canSubmit).toBe(false)
  })

  it('rejects an unknown mode', () => {
    const readiness = describeReadiness(
      loaded(W_OK, 'unknown-mode'),
      capabilities.maximumSourceCharacters,
    )
    expect(readiness.selectorsValid).toBe(false)
    expect(readiness.canSubmit).toBe(false)
  })
})

describe('rewriting status line', () => {
  it('prompts an idle workspace without a result', () => {
    const state = loaded('')
    expect(selectStatusLine(state)).toBe(MESSAGE_READY)
  })

  it('keeps the previous result editable while updating', () => {
    const state: RewritingWorkspaceState = {
      ...loaded(W_OK),
      phase: 'submitting',
      hasResult: true,
      resultText: W_RESULT,
    }
    expect(selectStatusLine(state)).toBe(MESSAGE_UPDATING)
  })

  it('announces first processing without a previous result', () => {
    const state: RewritingWorkspaceState = { ...loaded(W_OK), phase: 'submitting' }
    expect(selectStatusLine(state)).toBe(MESSAGE_PROCESSING)
  })

  it('marks edited inputs outdated and submits nothing', () => {
    const state: RewritingWorkspaceState = {
      ...loaded('Updated source.'),
      hasResult: true,
      resultText: W_RESULT,
      resultOutdated: true,
    }
    expect(selectStatusLine(state)).toBe(MESSAGE_OUTDATED)
  })

  it('reports up to date after a current success', () => {
    const state: RewritingWorkspaceState = {
      ...loaded(W_OK),
      hasResult: true,
      resultText: W_RESULT,
    }
    expect(selectStatusLine(state)).toBe(MESSAGE_UP_TO_DATE)
  })
})

describe('rewriting reducer guards', () => {
  it('captures one unsettled revision per explicit activation', () => {
    const ready = loaded(W_OK)
    const submitting = rewritingWorkspaceReducer(ready, { type: 'submitRequested' })
    expect(submitting.phase).toBe('submitting')
    expect(submitting.requestRevision).toBe(1)
    const duplicate = rewritingWorkspaceReducer(submitting, { type: 'submitRequested' })
    expect(duplicate.requestRevision).toBe(1)
    expect(duplicate.phase).toBe('submitting')
  })

  it('ignores activation while composing', () => {
    const state = rewritingWorkspaceReducer(
      { ...loaded(W_OK), composing: true },
      { type: 'submitRequested' },
    )
    expect(state.phase).toBe('idle')
    expect(state.requestRevision).toBe(0)
  })

  it('applies only the current revision and fences stale successes', () => {
    const first = rewritingWorkspaceReducer(loaded(W_OK), { type: 'submitRequested' })
    const second = rewritingWorkspaceReducer(
      { ...first, phase: 'idle', source: 'Updated source.' },
      { type: 'submitRequested' },
    )
    expect(second.requestRevision).toBe(2)
    const stale = rewritingWorkspaceReducer(second, {
      type: 'submitSucceeded',
      revision: 1,
      rewrittenText: 'Stale.',
      characterCount: 46,
      usage,
      observedAtMs: 1_000,
    })
    expect(stale.resultText).toBe('')
    expect(stale.hasResult).toBe(false)
    expect(stale.staleChargePending).toBe(46)
    expect(stale.notice).toBe(MESSAGE_STALE_SUCCESS)
    const current = rewritingWorkspaceReducer(stale, {
      type: 'submitSucceeded',
      revision: 2,
      rewrittenText: W_RESULT,
      characterCount: 16,
      usage,
      observedAtMs: 1_000,
    })
    expect(current.resultText).toBe(W_RESULT)
    expect(current.notice).toBe(formatStaleChargeNotice(46))
    expect(current.staleChargePending).toBeNull()
  })

  it('never surfaces a stale failure', () => {
    const first = rewritingWorkspaceReducer(loaded(W_OK), { type: 'submitRequested' })
    const second = rewritingWorkspaceReducer(
      { ...first, phase: 'idle', source: 'Updated source.' },
      { type: 'submitRequested' },
    )
    const staleFailure = rewritingWorkspaceReducer(second, {
      type: 'submitFailed',
      revision: 1,
      error: { text: MESSAGE_PROCESSING_FAILURE, canRetry: true },
    })
    expect(staleFailure.error).toBeNull()
    expect(staleFailure.phase).toBe('submitting')
  })

  it('protects a manually edited result from a pending response', () => {
    const submitting = rewritingWorkspaceReducer(loaded(W_OK), { type: 'submitRequested' })
    const edited = rewritingWorkspaceReducer(submitting, {
      type: 'resultEdited',
      value: 'My edit.',
    })
    const applied = rewritingWorkspaceReducer(edited, {
      type: 'submitSucceeded',
      revision: 1,
      rewrittenText: W_RESULT,
      characterCount: 46,
      usage,
      observedAtMs: 1_000,
    })
    expect(applied.resultText).toBe('My edit.')
    expect(applied.notice).toBe(MESSAGE_RESULT_EDITED)
    expect(applied.usage?.consumedCharacters).toBe(7546)
  })

  it('lets a later explicit submission replace preexisting edits', () => {
    const edited: RewritingWorkspaceState = {
      ...loaded('Updated source.'),
      hasResult: true,
      resultText: 'My edit.',
      resultEdited: true,
    }
    const submitting = rewritingWorkspaceReducer(edited, { type: 'submitRequested' })
    const applied = rewritingWorkspaceReducer(submitting, {
      type: 'submitSucceeded',
      revision: submitting.requestRevision,
      rewrittenText: W_RESULT,
      characterCount: 16,
      usage,
      observedAtMs: 1_000,
    })
    expect(applied.resultText).toBe(W_RESULT)
    expect(applied.resultEdited).toBe(false)
  })

  it('marks the previous result outdated on input and mode edits and clears errors', () => {
    const failed: RewritingWorkspaceState = {
      ...loaded(W_OK),
      hasResult: true,
      resultText: W_RESULT,
      error: { text: MESSAGE_PROCESSING_FAILURE, canRetry: true },
    }
    const edited = rewritingWorkspaceReducer(failed, {
      type: 'sourceChanged',
      source: 'Updated source.',
    })
    expect(edited.resultOutdated).toBe(true)
    expect(edited.error).toBeNull()
    expect(edited.resultText).toBe(W_RESULT)
    const modeEdited = rewritingWorkspaceReducer(failed, {
      type: 'modeChanged',
      value: 'friendly',
    })
    expect(modeEdited.resultOutdated).toBe(true)
    expect(modeEdited.error).toBeNull()
    expect(modeEdited.mode).toBe('friendly')
  })

  it('records copy outcomes without starting operations', () => {
    const state = loaded(W_OK)
    expect(rewritingWorkspaceReducer(state, { type: 'copied', ok: true }).notice).toBe(
      MESSAGE_COPIED,
    )
    expect(rewritingWorkspaceReducer(state, { type: 'copied', ok: false }).copyAlert).toBe(
      MESSAGE_COPY_FAILED,
    )
  })

  it('exposes the stale secondary line identifier', () => {
    expect(MESSAGE_STALE_SUCCESS).toBe('This update is no longer current and was not applied.')
  })
})

describe('submit problem mapping', () => {
  it('maps authentication failures to sign-in and verification messages', () => {
    expect(mapSubmitProblem({ httpStatus: 401 }).text).toBe(MESSAGE_SIGN_IN)
    expect(mapSubmitProblem({ httpStatus: 401 }).canRetry).toBe(false)
    expect(mapSubmitProblem({ httpStatus: 403 }).text).toBe(MESSAGE_VERIFY_EMAIL)
  })

  it('maps allowance denials with the supplied reset', () => {
    const reset = '00:00 UTC (in 6 hours)'
    expect(
      mapSubmitProblem({
        httpStatus: 429,
        category: 'userAllowance',
        resetAtUtc: reset,
      }).text,
    ).toBe(`Your daily allowance is used up. Try again after ${reset}. Your text is safe.`)
    expect(
      mapSubmitProblem({
        httpStatus: 429,
        category: 'globalAllowance',
        resetAtUtc: reset,
      }).text,
    ).toBe(`LinguaDesk’s shared daily allowance is used up. Try again after ${reset}. Your text is safe.`)
  })

  it('maps eligibility reasons to correction messages', () => {
    expect(mapSubmitProblem({ httpStatus: 422, reason: 'uncertain' }).text).toBe(
      MESSAGE_UNCERTAIN_SOURCE,
    )
    expect(mapSubmitProblem({ httpStatus: 422, reason: 'mixed' }).text).toBe(
      MESSAGE_UNSUPPORTED_CONTENT,
    )
    expect(mapSubmitProblem({ httpStatus: 422, reason: null }).text).toBe(
      MESSAGE_UNSUPPORTED_CONTENT,
    )
    expect(
      mapSubmitProblem({ httpStatus: 422, reason: 'oversizedSource', characterCount: 2312, limit: 2000 })
        .text,
    ).toBe('Rewriting is limited to 2,000 characters. Remove 312 characters to continue.')
  })

  it('maps deadline and monetary suspension distinctly', () => {
    expect(mapSubmitProblem({ httpStatus: 504 }).text).toBe(
      'Processing took too long. Your text and previous result are safe.',
    )
    expect(mapSubmitProblem({ httpStatus: 504 }).canRetry).toBe(true)
    expect(mapSubmitProblem({ httpStatus: 503, category: 'monetarySuspension' }).canRetry).toBe(
      false,
    )
  })

  it('maps failures and network loss to Try again', () => {
    expect(mapSubmitProblem({ httpStatus: 503, category: 'processingFailure' }).canRetry).toBe(true)
    expect(mapSubmitProblem({ httpStatus: null }).text).toBe(MESSAGE_PROCESSING_FAILURE)
    expect(mapSubmitProblem({ httpStatus: 400, category: 'invalidRequest' }).canRetry).toBe(true)
  })
})

describe('usage formatting', () => {
  it('renders the authoritative inline usage line', () => {
    const nowMs = Date.parse('2026-09-10T18:00:00Z')
    expect(formatUsageLine(usage, nowMs)).toBe(
      '7,546 of 20,000 characters used · 12,454 remaining · Resets 00:00 UTC (in 6 hours)',
    )
  })
})
