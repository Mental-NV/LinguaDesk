import { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useWorkspaceStores } from './workspaceStores'
import {
  RESET_DIALOG_BODY,
  RESET_DIALOG_CANCEL,
  RESET_DIALOG_CONFIRM,
  RESET_DIALOG_HEADING,
  WORKSPACE_FOOTER_TEXT,
  consumeTranslationSourceFocus,
  focusWorkspaceSource,
  isRewritingWorkspaceEmpty,
  isTranslationWorkspaceEmpty,
  requestTranslationSourceFocus,
  type WorkspaceFeature,
} from './workspaceResetModel'

interface StartNewWorkspaceButtonProps {
  readonly feature: WorkspaceFeature
  readonly fallbackReset: () => void
  readonly fallbackEmpty: boolean
}

/**
 * M032 inline reset control (UX section 3.4). Empty workspaces reset
 * immediately with no dialog; non-empty workspaces open the exact native
 * reset dialog. Confirm clears both pages through the single reset-all path,
 * navigates to /translate and focuses Source text without submitting.
 * Escape/Cancel preserves all state and returns focus to the trigger.
 */
export function StartNewWorkspaceButton({
  feature,
  fallbackReset,
  fallbackEmpty,
}: StartNewWorkspaceButtonProps) {
  const stores = useWorkspaceStores()
  const navigate = useNavigate()
  const location = useLocation()
  const triggerRef = useRef<HTMLButtonElement>(null)
  const dialogRef = useRef<HTMLDialogElement>(null)
  const cancelRef = useRef<HTMLButtonElement>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
  const focusAfterCloseRef = useRef(false)

  const translateEmpty =
    stores === null
      ? feature === 'translation'
        ? fallbackEmpty
        : true
      : isTranslationWorkspaceEmpty(stores.translate.state)
  const rewriteEmpty =
    stores === null
      ? feature === 'rewrite'
        ? fallbackEmpty
        : true
      : isRewritingWorkspaceEmpty(stores.rewrite.state)
  const workspacesEmpty = translateEmpty && rewriteEmpty

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog === null) return
    if (dialogOpen) {
      focusAfterCloseRef.current = false
      if (typeof dialog.showModal === 'function') {
        if (!dialog.open) dialog.showModal()
      } else {
        dialog.setAttribute('open', '')
      }
      cancelRef.current?.focus()
    } else {
      if (typeof dialog.close === 'function' && dialog.open) dialog.close()
      else dialog.removeAttribute('open')
      // Closing a modal dialog returns focus to its trigger, so the
      // confirm focus request runs after the close completes.
      if (focusAfterCloseRef.current) {
        focusAfterCloseRef.current = false
        focusWorkspaceSource('translation')
      }
    }
  }, [dialogOpen])

  const reset = (): void => {
    if (stores === null) fallbackReset()
    else stores.resetAll()
  }

  const finishOnTranslate = (): void => {
    if (location.pathname !== '/translate') {
      // The reset control unmounts on navigation, so the mounted
      // TranslatePage owns focusing through the pending request.
      focusAfterCloseRef.current = false
      requestTranslationSourceFocus()
      navigate('/translate')
    } else {
      focusAfterCloseRef.current = true
    }
  }

  const handleTrigger = (): void => {
    if (workspacesEmpty) {
      reset()
      if (location.pathname !== '/translate') {
        requestTranslationSourceFocus()
        navigate('/translate')
      } else {
        focusWorkspaceSource(feature)
      }
      return
    }
    setDialogOpen(true)
  }

  const closeDialog = (): void => {
    setDialogOpen(false)
    triggerRef.current?.focus()
  }

  const handleConfirm = (): void => {
    reset()
    setDialogOpen(false)
    finishOnTranslate()
  }

  return (
    <>
      <button ref={triggerRef} type="button" className="secondary-button" onClick={handleTrigger}>
        Start new workspace
      </button>
      <dialog
        ref={dialogRef}
        aria-labelledby="workspace-reset-heading"
        aria-describedby="workspace-reset-body"
        onCancel={closeDialog}
        onClose={() => setDialogOpen(false)}
      >
        <h2 id="workspace-reset-heading">{RESET_DIALOG_HEADING}</h2>
        <p id="workspace-reset-body">{RESET_DIALOG_BODY}</p>
        <div className="form-actions">
          <button ref={cancelRef} type="button" className="secondary-button" onClick={closeDialog}>
            {RESET_DIALOG_CANCEL}
          </button>
          <button type="button" className="primary-button" onClick={handleConfirm}>
            {RESET_DIALOG_CONFIRM}
          </button>
        </div>
      </dialog>
    </>
  )
}

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

export function useTranslationSourceFocusRequest(): void {
  useEffect(() => {
    if (consumeTranslationSourceFocus()) focusWorkspaceSource('translation')
  }, [])
}
