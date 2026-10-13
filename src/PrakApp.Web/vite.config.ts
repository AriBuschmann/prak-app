import { defineConfig } from 'vite';
import aurelia from '@aurelia/vite-plugin';

export default defineConfig(({ command }) => ({
  // useDev: ausführlichere Aurelia-Fehlermeldungen, nur beim Entwickeln
  plugins: [aurelia({ useDev: command === 'serve' })],
  server: {
    port: 9000,
    strictPort: true,
    // Im Entwicklungsmodus leitet Vite alle /api-Aufrufe an das Backend weiter
    proxy: {
      '/api': 'http://127.0.0.1:5080',
    },
  },
  css: {
    // Die jQWidgets-Styles enthalten alte IE-Hacks (z. B. "*zoom: 1"), die beim Minifizieren entfernt werden
    lightningcss: { errorRecovery: true },
  },
  build: {
    // Das Backend liefert das gebaute Frontend aus wwwroot aus
    outDir: '../PrakApp.Api/wwwroot',
    emptyOutDir: true,
    chunkSizeWarningLimit: 2000,
  },
}));
