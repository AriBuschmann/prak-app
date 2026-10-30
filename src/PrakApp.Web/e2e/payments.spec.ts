import { test, expect } from '@playwright/test';

test('Zahlungsdatum wird so gespeichert, wie es im Dialog steht', async ({ page }) => {
  const reference = `E2E-${Date.now()}`;
  // Das erwartete Datum: heute, in deutscher Zeit, im Format TT.MM.JJJJ
  const today = new Date().toLocaleDateString('de-DE', {
    timeZone: 'Europe/Berlin', day: '2-digit', month: '2-digit', year: 'numeric',
  });

  // 1. Anmelden
  // Die Anmeldeseite öffnen
  await page.goto('/login');
  // Den Button des Demo-Benutzers "praktikum" suchen und anklicken
  await page.locator('button[data-user="praktikum"]').click();
  // Warten, bis die App nach der Anmeldung auf die Kundenliste (/customers) weitergeleitet hat
  await expect(page).toHaveURL(/\/customers$/);

  // 2. Vertrag öffnen
  // Direkt die Vertragsseite von Vertrag 77 aus den Demo-Daten aufrufen (Route "contracts/:id" in my-app.ts)
  await page.goto('/contracts/77');
  // Warten, bis die Überschrift mit der Vertragsnummer sichtbar ist, denn erst dann sind die Daten geladen
  await expect(page.getByRole('heading', { name: /V-2023-0077/ })).toBeVisible();

  // 3. Zahlung erfassen
  // Den Button "Zahlung erfassen" anklicken; der Dialog öffnet sich mit Betrag und heutigem Datum vorausgefüllt
  await page.getByRole('button', { name: 'Zahlung erfassen' }).click();
  // Den Inhalt des gerade sichtbaren Dialogs merken, damit wir nur darin nach Feldern und Buttons suchen
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  // In das Feld "Verwendungszweck" unseren eindeutigen Text schreiben, damit wir die Zahlung später wiederfinden
  await dialog.locator('input[name=reference]').fill(reference);
  // Im Dialog auf "Speichern" klicken, jetzt wird die Zahlung an die API geschickt
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  // 4. Ergebnis prüfen
  // In der Zahlungstabelle die Zeile suchen, die unseren Verwendungszweck enthält
  const row = page.locator('.grid [role=row]').filter({ hasText: reference });
  // Prüfen, dass in dieser Zeile das heutige Datum steht
  await expect(row).toContainText(today);
});

// Bug: Backspace im Zahlungsdatum setzte das Jahr 1900 ein ("28.10.1900"), und der Server speicherte die Zahlung so.
// Jetzt löscht Backspace das ganze Datum (contract-detail.ts), und der Server lehnt ein leeres Datum ab.
// Der Test speichert nichts: Der Server antwortet mit 400, es entsteht keine Zahlung.
test('Backspace im Zahlungsdatum leert das Feld statt 1900 einzusetzen', async ({ page }) => {
  // Eindeutiger Verwendungszweck, um am Ende zu prüfen, dass keine Zahlung mit diesem Text in der Tabelle steht
  const reference = `Backspace-${Date.now()}`;
  // Zählen, wie oft eine Zahlung an den Server geschickt wird (POST auf .../payments)
  let posted = 0;
  page.on('request', (request) => {
    if (request.method() === 'POST' && request.url().endsWith('/payments')) posted++;
  });

  // Anmelden, Vertrag 77 aus den Demo-Daten öffnen und den Dialog "Zahlung erfassen" aufmachen
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);
  await page.goto('/contracts/77');
  await page.getByRole('button', { name: 'Zahlung erfassen' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  // Das eigentliche Eingabefeld im jQWidgets-Datumsfeld (data-testid="payment-date" ist der Rahmen darum)
  const dateInput = page.getByTestId('payment-date').locator('input').first();

  // dateInput.press() setzt selbst den Fokus ins Feld und drückt dann die Taste. Mit click() und danach
  // page.keyboard.press() kam der Tastendruck bei vielen gleichzeitigen Tests manchmal zu früh an.
  // End: Cursor ans Ende (ins Jahr), dort hat Backspace vorher 1900 eingesetzt
  await dateInput.press('End');
  await dateInput.press('Backspace');
  // Verwendungszweck ausfüllen; dabei verlässt der Fokus das Datumsfeld
  await dialog.locator('input[name=reference]').fill(reference);

  // Das Datumsfeld ist leer (vorher stand hier "28.10.1900", je nach Cursor auch das unveränderte Datum)
  await expect(dateInput).toHaveValue('');
  // Speichern schickt paymentDate: null; der Server meldet das Pflichtfeld, die Meldung steht im Dialog
  await dialog.getByRole('button', { name: 'Speichern' }).click();
  await expect(dialog.getByText('Zahlungsdatum ist erforderlich.')).toBeVisible();
  // Genau eine Anfrage an den Server (die abgelehnt wurde) ...
  expect(posted).toBe(1);
  // ... und in der Zahlungstabelle steht keine Zeile mit unserem Verwendungszweck, es wurde also nichts gespeichert
  await expect(page.locator('.grid [role=row]').filter({ hasText: reference })).toHaveCount(0);
});

// Verbesserung: Zahlungen in der Zukunft ablehnen. Der Server lehnte ein Datum in der Zukunft schon ab (PaymentValidator),
// im Kalender des Dialogs ließen sich zukünftige Tage aber noch auswählen. Jetzt sind sie dort gesperrt.
// Der Test speichert nichts: Der Klick auf den gesperrten Tag ändert nichts, die Anfrage an die API wird abgelehnt.
test('Zahlungsdatum in der Zukunft: im Kalender gesperrt und vom Server abgelehnt', async ({ page }) => {
  const two = (n: number) => String(n).padStart(2, '0');
  // Heute und morgen in deutscher Zeit (wie die App im Browser rechnet)
  const [y, m, d] = new Date().toLocaleDateString('en-CA', { timeZone: 'Europe/Berlin' }).split('-').map(Number);
  const tomorrow = new Date(Date.UTC(y, m - 1, d + 1));
  const todayText = `${two(d)}.${two(m)}.${y}`;
  const tomorrowIso = tomorrow.toISOString().slice(0, 10);
  // Ist morgen schon im nächsten Monat (heute ist der Monatsletzte), steht der Tag im Kalender als "othermonth"-Zelle
  const tomorrowInNextMonth = tomorrow.getUTCMonth() !== m - 1;

  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);
  await page.goto('/contracts/77');
  await page.getByRole('button', { name: 'Zahlung erfassen' }).click();
  const dateInput = page.getByTestId('payment-date').locator('input').first();
  await expect(dateInput).toHaveValue(todayText);

  // Kalender über den Knopf im Datumsfeld öffnen und die Zelle für morgen suchen
  await page.getByTestId('payment-date').locator('.jqx-action-button, [class*="jqx-icon-calendar"]').first().click();
  const tomorrowCell = page
    .locator('[class*="jqx-calendar-cell"]')
    .filter({ visible: true })
    .filter({ hasText: new RegExp(`^${tomorrow.getUTCDate()}$`) })
    .and(page.locator(tomorrowInNextMonth ? '[class*="othermonth"]' : ':not([class*="othermonth"])'));
  // Morgen ist gesperrt, ein Klick darauf ändert das Datum nicht
  await expect(tomorrowCell).toHaveClass(/jqx-calendar-cell-disabled/);
  await tomorrowCell.click();
  await expect(dateInput).toHaveValue(todayText);

  // Am Kalender vorbei direkt an die API geschickt: Der Server lehnt ab, es entsteht keine Zahlung
  const before = (await (await page.request.get('/api/contracts/77/payments')).json()).length;
  const response = await page.request.post('/api/contracts/77/payments', {
    data: { amount: 24.9, paymentDate: tomorrowIso, reference: `Zukunft-${Date.now()}` },
  });
  expect(response.status()).toBe(400);
  expect((await response.json()).errors.PaymentDate).toEqual(['Zahlungsdatum darf nicht in der Zukunft liegen.']);
  expect((await (await page.request.get('/api/contracts/77/payments')).json()).length).toBe(before);
});
