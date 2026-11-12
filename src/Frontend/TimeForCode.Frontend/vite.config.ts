/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  build: {
    // The BFF's CSP (`default-src 'self'`) blocks `data:` URLs, so never inline assets.
    assetsInlineLimit: 0,
  },
  server: {
    // Same route as the BFF in the image: /api/donation/v1/Project -> the local Donation API's /api/v1/Project.
    proxy: {
      // Trailing slash as in the BFF's route `/api/donation/{**rest}`, so `/api/donationfoo` is not proxied.
      '/api/donation/': {
        target: 'http://localhost:8082',
        rewrite: (path) => path.replace(/^\/api\/donation\//, '/api/'),
      },
      // Sign-in and the session need the BFF (front-end container on 8084): it turns the cookie into a bearer token
      // and answers /api/session. The GitHub callback stays on 8084; cookies on localhost are shared across ports.
      '/api/auth/': 'http://localhost:8084',
      '/api/session': 'http://localhost:8084',
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./tests/setup.ts'],
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/main.tsx', 'src/api/*.d.ts', 'src/**/*.test.{ts,tsx}'],
      // lcov paths are relative to the repository root, where the SonarCloud scan runs.
      reporter: ['text', ['lcov', { projectRoot: '../../..' }]],
    },
  },
})
