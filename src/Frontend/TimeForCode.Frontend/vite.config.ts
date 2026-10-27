/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  build: {
    // The nginx CSP (`default-src 'self'`) blocks `data:` URLs, so never inline assets.
    assetsInlineLimit: 0,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/main.tsx', 'src/**/*.test.{ts,tsx}'],
      // lcov paths are relative to the repository root, where the SonarCloud scan runs.
      reporter: ['text', ['lcov', { projectRoot: '../../..' }]],
    },
  },
})
