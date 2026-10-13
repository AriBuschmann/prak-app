import { defineConfig, devices } from '@playwright/test';

// Voraussetzungen: Datenbank aufgebaut (Database/rebuild-database.ps1) und Frontend gebaut (pnpm build).
// Playwright startet das Backend selbst, falls es nicht schon auf Port 5080 läuft.
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:5080',
    locale: 'de-DE',
    timezoneId: 'Europe/Berlin',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'dotnet run --project ../PrakApp.Api --no-launch-profile --urls http://localhost:5080',
    url: 'http://localhost:5080/health',
    env: { ASPNETCORE_ENVIRONMENT: 'Development' },
    reuseExistingServer: true,
    timeout: 120_000,
  },
});
