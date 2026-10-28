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
