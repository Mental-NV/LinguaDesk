import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

const apiBaseUrl = process.env.LINGUADESK_API_BASE_URL ?? 'http://127.0.0.1:5080'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '127.0.0.1',
    proxy: {
      '/api': apiBaseUrl,
      '/health': apiBaseUrl,
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
