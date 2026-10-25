import { useEffect, useRef } from 'react'
import { useWorkspaceStores } from './workspaceStores'
import { WORKSPACE_FOOTER_TEXT } from './workspaceResetModel'

export function WorkspaceLifetimeFooter() {
  return (
    <footer className="workspace-footer">
      <p>{WORKSPACE_FOOTER_TEXT}</p>
    </footer>
  )
}

/**
 * Workspace-route teardown (UX sections 3.3/3.4): real reload, full-document
 * navigation, tab close and bfcache storage all fire pagehide, so clearing
 * there ends the workspace; the defensive pageshow reset covers a real
 * bfcache restoration. Ordinary SPA navigation fires neither event, so
 * UX-AC-002 in-memory preservation is unaffected.
 */
export function WorkspaceSessionTeardown() {
  const stores = useWorkspaceStores()
  const storesRef = useRef(stores)
  useEffect(() => {
    storesRef.current = stores
  }, [stores])
  useEffect(() => {
    const handlePageHide = (): void => {
      storesRef.current?.resetAll()
    }
    const handlePageShow = (event: PageTransitionEvent): void => {
      if (event.persisted) storesRef.current?.resetAll()
    }
    window.addEventListener('pagehide', handlePageHide)
    window.addEventListener('pageshow', handlePageShow)
    return () => {
      window.removeEventListener('pagehide', handlePageHide)
      window.removeEventListener('pageshow', handlePageShow)
    }
  }, [])
  return null
}
