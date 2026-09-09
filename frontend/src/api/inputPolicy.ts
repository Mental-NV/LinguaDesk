export const COUNTING_POLICY_ID = 'unicode-scalar-v1' as const

export interface InputAnalysis {
  readonly source: string
  readonly isUnicodeValid: boolean
  readonly scalarCount: number | null
  readonly isEmptyOrWhitespace: boolean
  readonly isOversized: boolean
  readonly excess: number
  readonly isValid: boolean
}

export interface SelectorPolicy {
  readonly sourceDefault: string
  readonly sourceValues: readonly string[]
  readonly targetValues: readonly string[]
  readonly modeDefault: string
  readonly modeValues: readonly string[]
}

export interface TranslationInputValidation {
  readonly input: InputAnalysis
  readonly effectiveSourceSelection: string
  readonly isSourceSelectionValid: boolean
  readonly isTargetValid: boolean
  readonly isDistinctSelection: boolean
  readonly isValid: boolean
}

export interface RewritingInputValidation {
  readonly input: InputAnalysis
  readonly effectiveSourceSelection: string
  readonly effectiveMode: string
  readonly isSourceSelectionValid: boolean
  readonly isModeValid: boolean
  readonly isValid: boolean
}

export function analyzeInput(source: string, maximumSourceCharacters: number): InputAnalysis {
  if (!Number.isSafeInteger(maximumSourceCharacters) || maximumSourceCharacters <= 0) {
    throw new RangeError('maximumSourceCharacters must be a positive safe integer')
  }

  let scalarCount = 0
  let onlyWhitespace = true

  for (let index = 0; index < source.length; index += 1) {
    const firstCodeUnit = source.charCodeAt(index)
    let scalar = firstCodeUnit

    if (isHighSurrogate(firstCodeUnit)) {
      if (index + 1 >= source.length) return invalidUnicode(source)
      const secondCodeUnit = source.charCodeAt(index + 1)
      if (!isLowSurrogate(secondCodeUnit)) return invalidUnicode(source)
      scalar = ((firstCodeUnit - 0xd800) << 10) + (secondCodeUnit - 0xdc00) + 0x10000
      index += 1
    } else if (isLowSurrogate(firstCodeUnit)) {
      return invalidUnicode(source)
    }

    scalarCount += 1
    onlyWhitespace = onlyWhitespace && isWhitespaceScalar(scalar)
  }

  const isEmptyOrWhitespace = source.length === 0 || onlyWhitespace
  const excess = Math.max(0, scalarCount - maximumSourceCharacters)
  return {
    source,
    isUnicodeValid: true,
    scalarCount,
    isEmptyOrWhitespace,
    isOversized: excess > 0,
    excess,
    isValid: !isEmptyOrWhitespace && excess === 0,
  }
}

export function validateTranslation(
  source: string,
  maximumSourceCharacters: number,
  sourceSelection: string | undefined,
  target: string | undefined,
  policy: SelectorPolicy,
): TranslationInputValidation {
  const effectiveSourceSelection = sourceSelection ?? policy.sourceDefault
  const isSourceSelectionValid = policy.sourceValues.includes(effectiveSourceSelection)
  const isTargetValid = target !== undefined && policy.targetValues.includes(target)
  const isDistinctSelection =
    !isSourceSelectionValid ||
    !isTargetValid ||
    effectiveSourceSelection === policy.sourceDefault ||
    effectiveSourceSelection !== target
  const input = analyzeInput(source, maximumSourceCharacters)

  return {
    input,
    effectiveSourceSelection,
    isSourceSelectionValid,
    isTargetValid,
    isDistinctSelection,
    isValid: input.isValid && isSourceSelectionValid && isTargetValid && isDistinctSelection,
  }
}

export function validateRewriting(
  source: string,
  maximumSourceCharacters: number,
  sourceSelection: string | undefined,
  mode: string | undefined,
  policy: SelectorPolicy,
): RewritingInputValidation {
  const effectiveSourceSelection = sourceSelection ?? policy.sourceDefault
  const effectiveMode = mode ?? policy.modeDefault
  const isSourceSelectionValid = policy.sourceValues.includes(effectiveSourceSelection)
  const isModeValid = policy.modeValues.includes(effectiveMode)
  const input = analyzeInput(source, maximumSourceCharacters)

  return {
    input,
    effectiveSourceSelection,
    effectiveMode,
    isSourceSelectionValid,
    isModeValid,
    isValid: input.isValid && isSourceSelectionValid && isModeValid,
  }
}

export function isWhitespaceScalar(scalar: number): boolean {
  return (
    (scalar >= 0x0009 && scalar <= 0x000d) ||
    scalar === 0x0020 ||
    scalar === 0x0085 ||
    scalar === 0x00a0 ||
    scalar === 0x1680 ||
    (scalar >= 0x2000 && scalar <= 0x200a) ||
    scalar === 0x2028 ||
    scalar === 0x2029 ||
    scalar === 0x202f ||
    scalar === 0x205f ||
    scalar === 0x3000
  )
}

function isHighSurrogate(codeUnit: number): boolean {
  return codeUnit >= 0xd800 && codeUnit <= 0xdbff
}

function isLowSurrogate(codeUnit: number): boolean {
  return codeUnit >= 0xdc00 && codeUnit <= 0xdfff
}

function invalidUnicode(source: string): InputAnalysis {
  return {
    source,
    isUnicodeValid: false,
    scalarCount: null,
    isEmptyOrWhitespace: false,
    isOversized: false,
    excess: 0,
    isValid: false,
  }
}
