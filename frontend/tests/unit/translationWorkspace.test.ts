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
  translationWorkspaceReducer,
  type TranslationCapabilities,
  type TranslationUsage,
  type TranslationWorkspaceState,
} from '../../src/translate/translationWorkspace'

const capabilities: TranslationCapabilities = {
  maximumSourceCharacters: 5000,
  sourceDefault: 'auto',
  sourceValues: ['auto', 'en', 'ru', 'ro', 'zh'],
  languages: { en: 'English', ru: 'Russian', ro: 'Romanian', zh: 'Chinese (Simplified output)' },
  targetValues: ['en', 'ru', 'ro', 'zh'],
}

const usage: TranslationUsage = {
  consumedCharacters: 7546,
  allowanceCharacters: 20000,
  availableCharacters: 12454,
  resetAtUtc: '2026-09-11T00:00:00Z',
  availability: 'available',
  revision: 4,
}

function loaded(source: string, target = 'ro'): TranslationWorkspaceState {
  return {
    ...createInitialWorkspace(),
    capabilities,
    source,
    sourceSelection: 'en',
    target,
  }
}

describe('translation readiness', () => {
  it('blocks empty source with no request', () => {
    const readiness = describeReadiness(loaded('   '), capabilities.maximumSourceCharacters)
    expect(readiness.inputValid).toBe(false)
    expect(readiness.canSubmit).toBe(false)
  })

  it('allows a valid explicit submission', () => {
    const readiness = describeReadiness(
      loaded('Hello, the meeting starts at 14:30. Please go.'),
      capabilities.maximumSourceCharacters,
    )
    expect(readiness.canSubmit).toBe(true)
  })

  it('reports oversize excess without submitting', () => {
    const state = loaded('a'.repeat(5312))
    const readiness = describeReadiness(state, capabilities.maximumSourceCharacters)
    expect(readiness.excess).toBe(312)
    expect(readiness.canSubmit).toBe(false)
    expect(selectInlineValidation(state, readiness)).toBe(
      'Translation is limited to 5,000 characters. Remove 312 characters to continue.',
    )
  })

  it('retains same-language selections with MSG-007 until corrected', () => {
    const state = loaded('Hello.', 'en')
    const readiness = describeReadiness(state, capabilities.maximumSourceCharacters)
    expect(readiness.isSameLanguage).toBe(true)
    expect(readiness.canSubmit).toBe(false)
    expect(selectInlineValidation(state, readiness)).toBe(
      'Choose a target language different from English.',
    )
  })

  it('suppresses submission while composing or unsettled', () => {
    const composing = describeReadiness(
      { ...loaded('Hello.'), composing: true },
      capabilities.maximumSourceCharacters,
    )
    expect(composing.canSubmit).toBe(false)
    const unsettled = describeReadiness(
      { ...loaded('Hello.'), phase: 'submitting' },
      capabilities.maximumSourceCharacters,
    )
    expect(unsettled.canSubmit).toBe(false)
  })

  it('requires an explicit target', () => {
    const readiness = describeReadiness(
      { ...loaded('Hello.'), target: '' },
      capabilities.maximumSourceCharacters,
    )
    expect(readiness.canSubmit).toBe(false)
  })
})

describe('translation status line', () => {
  it('prompts an idle workspace without a result', () => {
    const state = loaded('')
    expect(selectStatusLine(state)).toBe(MESSAGE_READY)
  })

  it('keeps the previous result editable while updating', () => {
    const state: TranslationWorkspaceState = {
      ...loaded('Hello.'),
      phase: 'submitting',
      hasResult: true,
      resultText: 'Bună.',
    }
    expect(selectStatusLine(state)).toBe(MESSAGE_UPDATING)
  })

  it('announces first processing without a previous result', () => {
    const state: TranslationWorkspaceState = { ...loaded('Hello.'), phase: 'submitting' }
    expect(selectStatusLine(state)).toBe(MESSAGE_PROCESSING)
  })

  it('marks edited inputs outdated and submits nothing', () => {
    const state: TranslationWorkspaceState = {
      ...loaded('Hello, updated.'),
      hasResult: true,
      resultText: 'Bună.',
      resultOutdated: true,
    }
    expect(selectStatusLine(state)).toBe(MESSAGE_OUTDATED)
  })

  it('reports up to date after a current success', () => {
    const state: TranslationWorkspaceState = {
      ...loaded('Hello.'),
      hasResult: true,
      resultText: 'Bună.',
    }
    expect(selectStatusLine(state)).toBe(MESSAGE_UP_TO_DATE)
  })
})

describe('translation reducer guards', () => {
  it('captures one unsettled revision per explicit activation', () => {
    const ready = loaded('Hello.')
    const submitting = translationWorkspaceReducer(ready, { type: 'submitRequested' })
    expect(submitting.phase).toBe('submitting')
    expect(submitting.requestRevision).toBe(1)
    const duplicate = translationWorkspaceReducer(submitting, { type: 'submitRequested' })
    expect(duplicate.requestRevision).toBe(1)
    expect(duplicate.phase).toBe('submitting')
  })

  it('ignores activation while composing', () => {
    const state = translationWorkspaceReducer(
      { ...loaded('Hello.'), composing: true },
      { type: 'submitRequested' },
    )
    expect(state.phase).toBe('idle')
    expect(state.requestRevision).toBe(0)
  })

  it('applies only the current revision and fences stale successes', () => {
    const first = translationWorkspaceReducer(loaded('Hello.'), { type: 'submitRequested' })
    const second = translationWorkspaceReducer(
      { ...first, phase: 'idle', source: 'Hello, again.' },
      { type: 'submitRequested' },
    )
    expect(second.requestRevision).toBe(2)
    const stale = translationWorkspaceReducer(second, {
      type: 'submitSucceeded',
      revision: 1,
      translatedText: 'Stale.',
      characterCount: 7,
      usage,
      observedAtMs: 1_000,
    })
    expect(stale.resultText).toBe('')
    expect(stale.hasResult).toBe(false)
    expect(stale.staleChargePending).toBe(7)
    expect(stale.notice).toBe(MESSAGE_STALE_SUCCESS)
    const current = translationWorkspaceReducer(stale, {
      type: 'submitSucceeded',
      revision: 2,
      translatedText: 'Bună.',
      characterCount: 46,
      usage,
      observedAtMs: 1_000,
    })
    expect(current.resultText).toBe('Bună.')
    expect(current.notice).toBe(formatStaleChargeNotice(7))
    expect(current.staleChargePending).toBeNull()
  })

  it('never surfaces a stale failure', () => {
    const first = translationWorkspaceReducer(loaded('Hello.'), { type: 'submitRequested' })
    const second = translationWorkspaceReducer(
      { ...first, phase: 'idle', source: 'Hello, again.' },
      { type: 'submitRequested' },
    )
    const staleFailure = translationWorkspaceReducer(second, {
      type: 'submitFailed',
      revision: 1,
      error: { text: MESSAGE_PROCESSING_FAILURE, canRetry: true },
    })
    expect(staleFailure.error).toBeNull()
    expect(staleFailure.phase).toBe('submitting')
  })

  it('protects a manually edited result from a pending response', () => {
    const submitting = translationWorkspaceReducer(loaded('Hello.'), { type: 'submitRequested' })
    const edited = translationWorkspaceReducer(submitting, {
      type: 'resultEdited',
      value: 'My edit.',
    })
    const applied = translationWorkspaceReducer(edited, {
      type: 'submitSucceeded',
      revision: 1,
      translatedText: 'Bună.',
      characterCount: 7,
      usage,
      observedAtMs: 1_000,
    })
    expect(applied.resultText).toBe('My edit.')
    expect(applied.notice).toBe(MESSAGE_RESULT_EDITED)
    expect(applied.usage?.consumedCharacters).toBe(7546)
  })

  it('lets a later explicit submission replace preexisting edits', () => {
    const edited: TranslationWorkspaceState = {
      ...loaded('Hello, again.'),
      hasResult: true,
      resultText: 'My edit.',
      resultEdited: true,
    }
    const submitting = translationWorkspaceReducer(edited, { type: 'submitRequested' })
    const applied = translationWorkspaceReducer(submitting, {
      type: 'submitSucceeded',
      revision: submitting.requestRevision,
      translatedText: 'Bună.',
      characterCount: 14,
      usage,
      observedAtMs: 1_000,
    })
    expect(applied.resultText).toBe('Bună.')
    expect(applied.resultEdited).toBe(false)
  })

  it('marks the previous result outdated on input edits and clears errors', () => {
    const failed: TranslationWorkspaceState = {
      ...loaded('Hello.'),
      hasResult: true,
      resultText: 'Bună.',
      error: { text: MESSAGE_PROCESSING_FAILURE, canRetry: true },
    }
    const edited = translationWorkspaceReducer(failed, {
      type: 'sourceChanged',
      source: 'Hello, updated.',
    })
    expect(edited.resultOutdated).toBe(true)
    expect(edited.error).toBeNull()
    expect(edited.resultText).toBe('Bună.')
  })

  it('records copy outcomes without starting operations', () => {
    const state = loaded('Hello.')
    expect(translationWorkspaceReducer(state, { type: 'copied', ok: true }).notice).toBe(
      MESSAGE_COPIED,
    )
    expect(translationWorkspaceReducer(state, { type: 'copied', ok: false }).copyAlert).toBe(
      MESSAGE_COPY_FAILED,
    )
  })

  it('exposes the stale secondary line identifier', () => {
    expect(MESSAGE_STALE_SUCCESS).toBe('This update is no longer current and was not applied.')
  })

  it('releases the busy phase on abort without touching text and ignores stale aborts', () => {
    const submitting = translationWorkspaceReducer(loaded('Hello.'), { type: 'submitRequested' })
    const staleAbort = translationWorkspaceReducer(submitting, {
      type: 'submitAborted',
      revision: 0,
    })
    expect(staleAbort.phase).toBe('submitting')
    const aborted = translationWorkspaceReducer(submitting, {
      type: 'submitAborted',
      revision: 1,
    })
    expect(aborted.phase).toBe('idle')
    expect(aborted.source).toBe('Hello.')
    expect(aborted.error).toBeNull()
    expect(aborted.notice).toBeNull()
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
      mapSubmitProblem({ httpStatus: 422, reason: 'sameLanguage', languageName: 'Romanian' }).text,
    ).toBe('Choose a target language different from Romanian.')
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
