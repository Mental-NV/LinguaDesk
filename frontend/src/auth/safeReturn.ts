export const DEFAULT_SAFE_RETURN = '/translate' as const

export type SafeReturnPath = '/translate' | '/rewrite'

export function resolveSafeReturn(path: unknown): SafeReturnPath {
  if (path === '/translate' || path === '/rewrite') return path
  return DEFAULT_SAFE_RETURN
}

export function isProtectedPath(path: unknown): boolean {
  return path === '/translate' || path === '/rewrite'
}
