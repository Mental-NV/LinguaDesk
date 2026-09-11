import { useCallback, useMemo, type ReactNode } from 'react'
import { WorkspaceStoreContext } from './workspaceStores'
import { useTranslateFeatureInstance, useRewriteFeatureInstance } from './workspaceStores'
import type { WorkspaceStores } from './workspaceStores'

export function WorkspaceStoreProvider({ children }: { readonly children: ReactNode }) {
  const translate = useTranslateFeatureInstance()
  const rewrite = useRewriteFeatureInstance()
  // M032 safe reset: every teardown (explicit reset, sign-out/expiry and
  // navigation restoration) funnels through this single path so lifted text
  // cannot survive. Each feature aborts its own flights before clearing.
  const resetAll = useCallback(() => {
    translate.resetWorkspace()
    rewrite.resetWorkspace()
  }, [translate, rewrite])
  const value = useMemo<WorkspaceStores>(
    () => ({ translate, rewrite, resetAll }),
    [translate, rewrite, resetAll],
  )
  return <WorkspaceStoreContext.Provider value={value}>{children}</WorkspaceStoreContext.Provider>
}
