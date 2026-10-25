export function ActionIcon({ name }: { readonly name: 'copy' | 'swap' | 'tools' | 'panel' | 'account' | 'close' }) {
  const paths = {
    copy: 'M9 9h11v12H9z M5 16H3V3h12v2',
    swap: 'M3 7h17m-4-4 4 4-4 4M21 17H4m4-4-4 4 4 4',
    tools: 'M4 6h16M4 12h16M4 18h16M8 3v6m8 0v6m-6 0v6',
    panel: 'M3 3h18v18H3zM9 3v18m7-13-4 4 4 4',
    account: 'M20 21v-2a6 6 0 0 0-12 0v2M14 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8',
    close: 'm6 6 12 12M6 18 18 6',
  }
  return <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name]} /></svg>
}
