export interface ComparisonPart { readonly kind: 'equal' | 'insert' | 'delete'; readonly text: string }
const segmenter = new Intl.Segmenter(undefined, { granularity: 'grapheme' })
const tokens = (text: string) => {
  const out: string[] = []
  let word = ''
  for (const { segment } of segmenter.segment(text)) {
    if (/[\p{L}\p{N}\p{M}_]/u.test(segment)) word += segment
    else { if (word) { out.push(word); word = '' }; out.push(segment) }
  }
  if (word) out.push(word)
  return out
}
/** LCS stays within 200000 token pairs. The fallback preserves exact unmatched strings and complete deletions. */
export function compareText(source: string, result: string): ComparisonPart[] {
  const a = tokens(source), b = tokens(result), out: ComparisonPart[] = []
  const add = (kind: ComparisonPart['kind'], text: string) => {
    if (!text) return
    const last = out.at(-1)
    if (last?.kind === kind) out[out.length - 1] = { kind, text: last.text + text }
    else out.push({ kind, text })
  }
  let prefix = 0
  while (prefix < a.length && prefix < b.length && a[prefix] === b[prefix]) prefix++
  add('equal', a.slice(0, prefix).join(''))
  let ae = a.length, be = b.length
  while (ae > prefix && be > prefix && a[ae - 1] === b[be - 1]) { ae--; be-- }
  const n = ae - prefix, m = be - prefix
  if (n * m > 200000) { add('delete', a.slice(prefix, ae).join('')); add('insert', b.slice(prefix, be).join('')) }
  else {
    const table = Array.from({ length: n + 1 }, () => new Uint32Array(m + 1))
    for (let i = n - 1; i >= 0; i--) for (let j = m - 1; j >= 0; j--)
      table[i][j] = a[prefix + i] === b[prefix + j] ? table[i + 1][j + 1] + 1 : Math.max(table[i + 1][j], table[i][j + 1])
    let i = 0, j = 0
    while (i < n || j < m) {
      if (i < n && j < m && a[prefix + i] === b[prefix + j]) { add('equal', a[prefix + i]); i++; j++ }
      else if (j < m && (i === n || table[i][j + 1] >= table[i + 1][j])) { add('insert', b[prefix + j]); j++ }
      else { add('delete', a[prefix + i]); i++ }
    }
  }
  add('equal', b.slice(be).join(''))
  return out
}
export function changedScalarRanges(parts: readonly ComparisonPart[]) {
  let offset = 0
  const ranges: { start: number; end: number }[] = []
  for (const part of parts) if (part.kind !== 'delete') {
    const end = offset + Array.from(part.text).length
    if (part.kind === 'insert') ranges.push({ start: offset, end })
    offset = end
  }
  return ranges
}
