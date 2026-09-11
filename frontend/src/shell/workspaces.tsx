import type { ReactNode } from 'react'
import { WorkspaceStoreContext } from './workspaceStores'
import { useTranslateFeatureInstance, useRewriteFeatureInstance } from './workspaceStores'
import { useMemo } from 'react'
import type { WorkspaceStores } from './workspaceStores'

export function WorkspaceStoreProvider({ children }: { readonly children: ReactNode }) {
  const translate = useTranslateFeatureInstance()
  const rewrite = useRewriteFeatureInstance()
  const value = useMemo<WorkspaceStores>(() => ({ translate, rewrite }), [translate, rewrite])
  return <WorkspaceStoreContext.Provider value={value}>{children}</WorkspaceStoreContext.Provider>
}
