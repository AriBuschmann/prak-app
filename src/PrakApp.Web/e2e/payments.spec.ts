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
