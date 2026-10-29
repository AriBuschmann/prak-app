import { test, expect } from '@playwright/test';

test('nicht angemeldete Benutzer landen auf der Anmeldeseite', async ({ page }) => {
  await page.goto('/customers');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Anmeldung' })).toBeVisible();
});

test('Anmeldung zeigt die Kundenliste', async ({ page }) => {
  await page.goto('/');

  await page.locator('button[data-user="praktikum"]').click();

  await expect(page).toHaveURL(/\/customers$/);
  await expect(page.getByTestId('current-user')).toHaveText('Praktikum');
  await expect(page.getByRole('heading', { name: 'Kunden' })).toBeVisible();
  await expect(page.locator('.grid [role=row]').first()).toBeVisible();
});

// Bug: Lief die Sitzung ab, antwortete der Server mit 401 ("nicht angemeldet"), die App zeigte aber nur das rote Banner
// "Unerwarteter Fehler 401" und blieb auf der Seite. Jetzt meldet sie ab und leitet zur Anmeldung weiter (http.ts, my-app.ts).
// Abgelaufene Sitzung wird simuliert, indem der Test das Anmelde-Cookie löscht: Für den Server ist das dasselbe,
// die Anfrage hat kein gültiges Cookie mehr und er antwortet mit 401. (8 Stunden warten geht im Test nicht.)

test('abgelaufene Sitzung bei der Suche führt zur Anmeldung', async ({ page, context }) => {
  // Anmelden wie ein Benutzer: Anmeldeseite öffnen, Demo-Benutzer "praktikum" anklicken, Kundenliste erscheint
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);

  // Sitzung "ablaufen lassen": context ist das Browserfenster des Tests, clearCookies() löscht alle seine Cookies
  await context.clearCookies();
  // Etwas Normales tun: suchen. Die Suchanfrage an den Server bekommt jetzt 401
  await page.getByTestId('customer-search').fill('Albers');

  // Weitergeleitet zur Anmeldeseite (vor der Korrektur blieb die App auf /customers)
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Anmeldung' })).toBeVisible();
  // Kein rotes Fehler-Banner: toHaveCount(0) heißt, das Element gibt es gar nicht auf der Seite
  await expect(page.locator('.error-banner')).toHaveCount(0);
  // Oben rechts steht kein angemeldeter Benutzer mehr (vorher stand dort weiter "Praktikum")
  await expect(page.getByTestId('current-user')).toHaveCount(0);

  // Erneut anmelden funktioniert wieder normal: Kundenliste, oben steht "Praktikum"
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);
  await expect(page.getByTestId('current-user')).toHaveText('Praktikum');
});

test('abgelaufene Sitzung beim Öffnen eines Kunden führt zur Anmeldung', async ({ page, context }) => {
  // Anmelden wie im Test oben
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);

  // Sitzung "ablaufen lassen" (Cookie löschen)
  await context.clearCookies();
  // Zählen, wie viele Antworten mit 401 ab jetzt vom Server kommen. page.on('response', ...) wird für jede Antwort aufgerufen
  let unauthorized = 0;
  page.on('response', (response) => {
    if (response.status() === 401) unauthorized++;
  });
  // Einen Kunden in der Liste anklicken: die erste Zelle mit einer Kundennummer wie "K-100040"
  await page.locator('.grid div[role=gridcell]').filter({ hasText: /^K-\d+$/ }).first().click();

  // Weitergeleitet zur Anmeldeseite, ohne rotes Fehler-Banner
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.locator('.error-banner')).toHaveCount(0);
  // Noch 1 Sekunde warten, damit eine Endlosschleife (falls es sie noch gäbe) genug Anfragen erzeugen könnte
  await page.waitForTimeout(1000);
  // Keine Endlosschleife: Vor der Korrektur versuchte der Router immer wieder Kundenseite und Kundenliste zu laden
  // (über 900 Anfragen in einer Sekunde). Die Kundenseite lädt 3 Dinge gleichzeitig (Kunde, Verträge, Vertragsarten),
  // also sind 3 Antworten mit 401 normal; 5 lässt etwas Spielraum
  expect(unauthorized).toBeLessThanOrEqual(5);
});

// Fehlender Test: Abmelden. Danach muss man auf der Anmeldeseite sein, und die API darf keine Kundendaten mehr liefern
test('Abmelden führt zur Anmeldeseite, danach liefert die API keine Kundendaten mehr', async ({ page }) => {
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);
  // Vorher: angemeldet, die API liefert die Kundenliste
  expect((await page.request.get('/api/customers')).status()).toBe(200);

  await page.getByRole('button', { name: 'Abmelden' }).click();

  // Anmeldeseite, oben steht kein angemeldeter Benutzer mehr
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Anmeldung' })).toBeVisible();
  await expect(page.getByTestId('current-user')).toHaveCount(0);
  // page.request schickt die Cookies des Browserfensters mit. Nach dem Abmelden ist das Anmelde-Cookie weg → 401
  expect((await page.request.get('/api/customers')).status()).toBe(401);
  // Auch die Kundenliste im Browser ist nicht mehr erreichbar, man landet wieder auf der Anmeldeseite
  await page.goto('/customers');
  await expect(page).toHaveURL(/\/login$/);
});
