import react from '@vitejs/plugin-react'
import { readFileSync } from 'node:fs'
import { defineConfig } from 'vitest/config'

const apiBaseUrl = process.env.LINGUADESK_API_BASE_URL ?? 'https://localhost:5080'
const developmentCertificatePath = process.env.LINGUADESK_DEVELOPMENT_CERTIFICATE_PATH
const developmentCertificateKeyPath = process.env.LINGUADESK_DEVELOPMENT_CERTIFICATE_KEY_PATH

const developmentHttps = developmentCertificatePath && developmentCertificateKeyPath
  ? {
      cert: readFileSync(developmentCertificatePath),
      key: readFileSync(developmentCertificateKeyPath),
    }
  : undefined

export default defineConfig({
  plugins: [react()],
  server: {
    host: '127.0.0.1',
    https: developmentHttps,
    proxy: {
      '/api': { target: apiBaseUrl, secure: true },
      '/health': { target: apiBaseUrl, secure: true },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './tests/unit/setup.ts',
    include: ['tests/unit/**/*.test.{ts,tsx}'],
    passWithNoTests: false,
    restoreMocks: true,
  },
})
