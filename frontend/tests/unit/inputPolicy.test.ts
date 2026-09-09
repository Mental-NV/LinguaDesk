import fixture from '../../../contracts/fixtures/unicode-scalar-v1.json'
import {
  COUNTING_POLICY_ID,
  analyzeInput,
  isWhitespaceScalar,
  validateRewriting,
  validateTranslation,
} from '../../src/api/inputPolicy'

describe('unicode-scalar-v1 input policy', () => {
  it('uses the shared fixture policy revision', () => {
    expect(COUNTING_POLICY_ID).toBe(fixture.policyId)
  })

  it.each(fixture.validCases)('counts $id without normalization', (fixtureCase) => {
    const result = analyzeInput(fixtureCase.text, 10_000)
    expect(result.isUnicodeValid).toBe(true)
    expect(result.scalarCount).toBe(fixtureCase.expectedCount)
    expect(result.isEmptyOrWhitespace).toBe(fixtureCase.expectedWhitespaceOnly)
    expect(result.source).toBe(fixtureCase.text)
    expect(result.isValid).toBe(!fixtureCase.expectedWhitespaceOnly)
  })

  it('recognizes every fixed whitespace scalar and no adjacent format scalar', () => {
    expect(fixture.whitespaceCodePoints).toHaveLength(25)
    for (const codePoint of fixture.whitespaceCodePoints) {
      expect(isWhitespaceScalar(codePoint), `U+${codePoint.toString(16)}`).toBe(true)
      expect(analyzeInput(String.fromCodePoint(codePoint), 10).isEmptyOrWhitespace).toBe(true)
    }
    expect(isWhitespaceScalar(0x200b)).toBe(false)
    expect(isWhitespaceScalar(0x200d)).toBe(false)
    expect(isWhitespaceScalar(0x0301)).toBe(false)
  })

  it.each(fixture.malformedCases)('rejects malformed UTF-16 $id', (fixtureCase) => {
    const source = String.fromCharCode(...fixtureCase.utf16CodeUnits)
    const result = analyzeInput(source, 10)
    expect(result.isUnicodeValid).toBe(false)
    expect(result.scalarCount).toBeNull()
    expect(result.isValid).toBe(false)
  })

  it.each(fixture.limitCases)('applies whole-input boundary $id', (fixtureCase) => {
    const source = fixtureCase.scalar.repeat(fixtureCase.repeat)
    const result = analyzeInput(source, fixtureCase.maximum)
    expect(result.scalarCount).toBe(fixtureCase.repeat)
    expect(result.isValid).toBe(fixtureCase.expectedValid)
    expect(result.excess).toBe(fixtureCase.expectedExcess)
    expect(result.source).toBe(source)
  })

  it('defaults translation source and rejects missing, invalid, or same-language selectors', () => {
    const policy = fixture.selectorPolicy
    expect(validateTranslation('text', 5000, undefined, 'en', policy)).toMatchObject({
      effectiveSourceSelection: 'auto',
      isValid: true,
    })
    expect(validateTranslation('text', 5000, 'xx', 'en', policy).isValid).toBe(false)
    expect(validateTranslation('text', 5000, 'en', undefined, policy).isValid).toBe(false)
    expect(validateTranslation('text', 5000, 'en', 'en', policy).isValid).toBe(false)
    expect(validateTranslation('text', 5000, 'en', 'ru', policy).isValid).toBe(true)
  })

  it('defaults rewriting mode and accepts exactly one advertised mode', () => {
    const policy = fixture.selectorPolicy
    expect(validateRewriting('text', 2000, undefined, undefined, policy)).toMatchObject({
      effectiveSourceSelection: 'auto',
      effectiveMode: 'correctionOnly',
      isValid: true,
    })
    expect(validateRewriting('text', 2000, 'ro', 'friendly', policy).isValid).toBe(true)
    expect(validateRewriting('text', 2000, 'xx', 'friendly', policy).isValid).toBe(false)
    expect(validateRewriting('text', 2000, 'ro', 'friendly,business', policy).isValid).toBe(false)
    expect(validateRewriting('text', 2000, 'ro', '', policy).isValid).toBe(false)
  })

  it('fails empty, all-whitespace, malformed, and oversized input before selectors pass', () => {
    const policy = fixture.selectorPolicy
    expect(validateTranslation('', 5000, undefined, 'ru', policy).isValid).toBe(false)
    expect(validateTranslation('\t\u3000', 5000, undefined, 'ru', policy).isValid).toBe(false)
    expect(validateRewriting(String.fromCharCode(0xd800), 2000, undefined, undefined, policy).isValid).toBe(false)
    expect(validateRewriting('a'.repeat(2001), 2000, undefined, undefined, policy).isValid).toBe(false)
  })

  it('is deterministic and preserves significant source content', () => {
    const source = ' é\r\n😀 '
    expect(analyzeInput(source, 20)).toEqual(analyzeInput(source, 20))
    expect(analyzeInput(source, 20)).toMatchObject({ source, scalarCount: 7, isValid: true })
  })

  it('requires a positive safe advertised maximum', () => {
    expect(() => analyzeInput('text', 0)).toThrow(RangeError)
    expect(() => analyzeInput('text', Number.MAX_SAFE_INTEGER + 1)).toThrow(RangeError)
  })
})
