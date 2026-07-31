import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Запросы к /api уходят на ASP.NET Core — CORS не нужен.
    proxy: {
      '/api': {
        target: 'http://localhost:5187',
        changeOrigin: true,
      },
    },
  },
})
