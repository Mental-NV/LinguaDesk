import { analyzeInput, isWhitespaceScalar } from '../api/inputPolicy'

export const SEGMENTATION_REVISION = 'sentence-boundaries-v1'
export interface SentenceSpan { readonly start: number; readonly length: number }
const abbreviations = new Set(['mr.', 'mrs.', 'ms.', 'dr.', 'prof.', 'e.g.', 'i.e.', 'etc.', 'dl.', 'dna.', 'г.', 'ул.', 'т.д.', 'т.п.'])
const white = (s: string) => isWhitespaceScalar(s.codePointAt(0)!)
export const scalarSlice = (text: string, start: number, length?: number) => Array.from(text).slice(start, length === undefined ? undefined : start + length).join('')
export const spanKey = (span: SentenceSpan) => `${span.start}:${span.length}`
export function sentenceSpans(text: string): SentenceSpan[] {
  if (!analyzeInput(text, Number.MAX_SAFE_INTEGER).isUnicodeValid) return []
  const chars = Array.from(text), spans: SentenceSpan[] = []
  let start = 0
  const emit = (to: number) => {
    let end = to
    while (start < end && white(chars[start])) start++
    while (end > start && white(chars[end - 1])) end--
    if (end > start) spans.push({ start, length: end - start })
  }
  for (let i = 0; i < chars.length; i++) {
    const ch = chars[i]
    if (['\r', '\n', '\u2028', '\u2029'].includes(ch)) {
      emit(i)
      if (ch === '\r' && chars[i + 1] === '\n') i++
      start = i + 1
      continue
    }
    if (!'.!?。！？'.includes(ch)) continue
    let end = i + 1
    while (end < chars.length && '!?。！？'.includes(chars[end])) end++
    while (end < chars.length && `"'”’»)]}」』】）`.includes(chars[end])) end++
    if (ch === '.') {
      if (end < chars.length && !white(chars[end])) continue
      const token = chars.slice(0, i + 1).join('').match(/[\p{L}.]+\.$/u)?.[0] ?? ''
      const nextWord = /^[^\S\r\n]*[\p{L}\p{N}]/u.test(chars.slice(end).join(''))
      if (abbreviations.has(token.toLowerCase()) || /^(?:\p{L}\.){2,}$/u.test(token) || (/^\p{L}\.$/u.test(token) && nextWord)) continue
    }
    emit(end); start = end; i = end - 1
  }
  emit(chars.length)
  return spans
}
