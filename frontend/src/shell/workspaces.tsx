import { WorkspaceSessionTeardown } from './WorkspaceReset'
import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { UsageChannel, WorkspaceStoreContext } from './workspaceStores'
import { useTranslateFeatureInstance, useRewriteFeatureInstance } from './workspaceStores'
import type { WorkspaceStores } from './workspaceStores'

export function WorkspaceStoreProvider({ children }: { readonly children: ReactNode }) {
  const [navigationExpanded, setNavigationExpanded] = useState(false)
  const toggleNavigation = useCallback(() => setNavigationExpanded(value => !value), [])
  const [usage] = useState(() => new UsageChannel())
  const translate = useTranslateFeatureInstance(usage)
  const rewrite = useRewriteFeatureInstance(usage)
  // Every teardown (sign-out/expiry and
  // navigation restoration) funnels through this single path so lifted text
  // cannot survive. Each feature aborts its own flights before clearing.
  const resetAll = useCallback(() => {
    setNavigationExpanded(false)
    usage.reset()
    translate.resetWorkspace()
    rewrite.resetWorkspace()
  }, [translate, rewrite, usage])
  const value = useMemo<WorkspaceStores>(
    () => ({ translate, rewrite, resetAll, navigationExpanded, toggleNavigation }),
    [translate, rewrite, resetAll, navigationExpanded, toggleNavigation],
  )
  return <WorkspaceStoreContext.Provider value={value}><WorkspaceSessionTeardown />{children}</WorkspaceStoreContext.Provider>
}
