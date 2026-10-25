import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, NavLink, useLocation } from 'react-router-dom'
import { useWorkspaceStores } from './workspaceStores'
import { ActionIcon } from './ActionIcon'
import { TranslationIcon, RewritingIcon } from './NavIcons'

export function Navigation({ onSignOut }: { readonly onSignOut: () => void }) {
  const stores = useWorkspaceStores()
  const expanded = stores?.navigationExpanded ?? false
  const [accountOpen, setAccountOpen] = useState(false)
  const toggle = useRef<HTMLButtonElement>(null), account = useRef<HTMLButtonElement>(null), panel = useRef<HTMLElement>(null), menu = useRef<HTMLDivElement>(null)
  const location = useLocation()
  const railToggle = useRef<HTMLButtonElement>(null)
  const focusNavigation = useCallback(() => { (window.matchMedia('(max-width: 767px)').matches ? toggle.current : railToggle.current)?.focus() }, [])
  const closeNavigation = () => { if (expanded) stores?.toggleNavigation?.(); focusNavigation() }
  useEffect(() => {
    const dismiss = (event: PointerEvent) => {
      if (!panel.current?.contains(event.target as Node) && !toggle.current?.contains(event.target as Node)) {
        if (expanded && window.matchMedia('(max-width: 1199px)').matches) { stores?.toggleNavigation?.(); focusNavigation() }
      }
      if (!menu.current?.contains(event.target as Node) && !account.current?.contains(event.target as Node)) setAccountOpen(false)
    }
    const escape = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      if (accountOpen) { setAccountOpen(false); account.current?.focus(); event.stopPropagation() }
      else if (expanded && window.matchMedia('(max-width: 1199px)').matches) { stores?.toggleNavigation?.(); focusNavigation() }
    }
    document.addEventListener('pointerdown', dismiss); document.addEventListener('keydown', escape)
    return () => { document.removeEventListener('pointerdown', dismiss); document.removeEventListener('keydown', escape) }
  }, [accountOpen, expanded, stores, focusNavigation])
  return <>
    <header className="workspace-topband"><Link className="wordmark mobile-brand" to="/" aria-label="LinguaDesk home">LinguaDesk</Link>
      <button ref={toggle} type="button" className="icon-button mobile-navigation-toggle" aria-label="Navigation" aria-expanded={expanded} onClick={stores?.toggleNavigation}><ActionIcon name="panel" /><span>Navigation</span></button>
    </header>
    <aside ref={panel} className={`navigation-rail${expanded ? ' expanded' : ''}`} aria-label="Navigation panel">
      <Link className="rail-brand wordmark" to="/" aria-label="LinguaDesk home"><span aria-hidden="true">L</span>{expanded && <span>LinguaDesk</span>}</Link>
      <nav aria-label="Main navigation">
        <NavLink to="/translate" aria-label="Translator" title="Translator" onClick={() => { if (window.matchMedia('(max-width: 767px)').matches) closeNavigation() }}><TranslationIcon /><span>Translator</span></NavLink>
        <NavLink to="/rewrite" aria-label="Write" title="Write" onClick={() => { if (window.matchMedia('(max-width: 767px)').matches) closeNavigation() }}><RewritingIcon /><span>Write</span></NavLink>
      </nav>
      <div className="rail-bottom">
        <button ref={account} type="button" className="icon-button account-trigger" title="Account" aria-label="Account" aria-haspopup="menu" aria-expanded={accountOpen} onClick={() => setAccountOpen(value => !value)}><ActionIcon name="account" /><span>Account</span></button>
        {accountOpen && <div ref={menu} className="account-menu" role="menu" aria-label="Account"><button type="button" role="menuitem" autoFocus onClick={onSignOut}>Log out</button></div>}
        <button ref={railToggle} type="button" className="icon-button rail-toggle" title={expanded ? 'Collapse navigation' : 'Expand navigation'} aria-label={expanded ? 'Collapse navigation' : 'Expand navigation'} aria-expanded={expanded} onClick={stores?.toggleNavigation}><ActionIcon name="panel" /><span>{expanded ? 'Collapse' : 'Expand'}</span></button>
      </div>
    </aside>
    <span className="visually-hidden" aria-live="off">{location.pathname === '/rewrite' ? 'Write workspace' : 'Translator workspace'}</span>
  </>
}
