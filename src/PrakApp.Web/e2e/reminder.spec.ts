import { test, expect, type Page } from '@playwright/test';

// Verbesserung: Laut Handbuch (03-arbeitsablaeufe.md, 3.5) ist eine Zahlungserinnerung fällig, wenn der Saldo mehr als
// zwei Monatsbeiträge beträgt. Die App markiert solche Verträge jetzt mit "Zahlungserinnerung fällig": im Beitragskonto
// auf der Vertragsseite und in der Spalte "Zahlungserinnerung" der Vertragsliste beim Kunden.
// Die genauen Grenzfälle (z. B. genau zwei Monatsbeiträge = noch nicht fällig) prüfen die Unit-Tests in BalanceCalculatorTests.cs.

// Ein Datum als Text "JJJJ-MM-TT" in deutscher Zeit (die Schreibweise en-CA liefert genau dieses Format)
const berlinDate = (date: Date) => date.toLocaleDateString('en-CA', { timeZone: 'Europe/Berlin' });

// Hilfsfunktion: legt über die API einen Vertrag für den Kunden an (Basis-Tarif, kein Ende, 20 € im Monat)
// und gibt Id und Vertragsnummer des neuen Vertrags zurück
async function createContract(page: Page, customerId: number, startDate: string): Promise<{ id: number; contractNumber: string }> {
  const response = await page.request.post('/api/contracts', {
    data: { customerId, contractTypeId: 1, startDate, endDate: null, monthlyFee: 20 },
  });
  // 201 = angelegt
  expect(response.status()).toBe(201);
  return (await response.json()).contract;
}

test('Rückstand über zwei Monatsbeiträge wird als "Zahlungserinnerung fällig" markiert', async ({ page }) => {
  // Anmelden
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);

  // Einen eigenen Kunden anlegen, damit der Test nicht von den Demo-Daten abhängt (eindeutiger Name durch Date.now())
  const customer = await page.request.post('/api/customers', {
    data: { firstName: 'Erika', lastName: `Rueckstand${Date.now()}` },
  });
  expect(customer.status()).toBe(201);
  const customerId = (await customer.json()).id;

  // Vertrag MIT Rückstand: Beginn am 1. des Monats vor drei Monaten, ohne Zahlung.
  // Fällig sind dann mindestens 4 Beiträge (z. B. 1.7., 1.8., 1.9., 1.10.) = mindestens 80 €, also mehr als 2 × 20 € = 40 €.
  // new Date(Jahr, Monat − 3, 1, 12): Ein negativer Monat wird ins Vorjahr umgerechnet (Januar − 3 = Oktober des Vorjahrs);
  // 12 Uhr mittags, damit die Umrechnung in deutsche Zeit sicher denselben Tag ergibt
  const today = new Date();
  const threeMonthsAgo = berlinDate(new Date(today.getFullYear(), today.getMonth() - 3, 1, 12));
  const inArrears = await createContract(page, customerId, threeMonthsAgo);
  // Vertrag OHNE Rückstand: Beginn heute → 1 fälliger Beitrag = 20 €, das ist nicht mehr als 40 €
  const upToDate = await createContract(page, customerId, berlinDate(today));

  // Vertragsseite mit Rückstand: Im Beitragskonto steht der rote Hinweis
  await page.goto(`/contracts/${inArrears.id}`);
  await expect(page.getByTestId('reminder-due')).toContainText('Zahlungserinnerung fällig');
  // Vertragsseite ohne Rückstand: erst warten, bis das Beitragskonto geladen ist ("1" fälliger Monat) ...
  await page.goto(`/contracts/${upToDate.id}`);
  await expect(page.getByTestId('months-due')).toHaveText('1');
  // ... und erst dann prüfen, dass der Hinweis fehlt. Ohne die Zeile davor wäre toHaveCount(0) schon erfüllt,
  // solange die Seite noch lädt, und der Test würde einen Fehler übersehen
  await expect(page.getByTestId('reminder-due')).toHaveCount(0);

  // Kundenseite: In der Vertragsliste steht in der Zeile des Vertrags mit Rückstand "fällig", beim anderen nicht.
  // Die Zeilen werden über die Vertragsnummer gefunden, die die API beim Anlegen zurückgegeben hat
  await page.goto(`/customers/${customerId}`);
  const rows = page.locator('.grid [role=row]');
  await expect(rows.filter({ hasText: inArrears.contractNumber })).toContainText('fällig');
  await expect(rows.filter({ hasText: upToDate.contractNumber })).not.toContainText('fällig');
});
