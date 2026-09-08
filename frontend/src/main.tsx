import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { App } from './shell/App'
import './shell/shell.css'

const rootElement = document.getElementById('root')

if (!rootElement) {
  throw new Error('LinguaDesk could not find its application root.')
}

createRoot(rootElement).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
)
