import { test, expect } from '@playwright/test';

// Verbesserung: Ein Vertrag gilt ab dem Tag nach dem Vertragsende als "Beendet". Vorher blieb ein befristeter Vertrag
// nach seinem Ende "Aktiv" und ließ sich sogar noch kündigen. Die Einzelfälle der Regel prüft ContractStatusTests.cs,
// dieser Test prüft das Zusammenspiel: Anzeige auf beiden Seiten, Knopf "Kündigen" und Antwort des Servers.
test('befristeter Vertrag ist nach dem Vertragsende "Beendet"', async ({ page }) => {
  // Anmelden
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);

  // Testdaten über die API anlegen (wie in cancellation.spec.ts): einen eigenen Kunden mit eindeutigem Namen ...
  const customer = await page.request.post('/api/customers', {
    data: { firstName: 'Erika', lastName: `Befristet${Date.now()}` },
  });
  expect(customer.status()).toBe(201);
  const customerId = (await customer.json()).id;
  // ... und einen befristeten Vertrag, der am 31.12.2025 geendet hat, also sicher in der Vergangenheit liegt
  const created = await page.request.post('/api/contracts', {
    data: { customerId, contractTypeId: 1, startDate: '2025-01-01', endDate: '2025-12-31', monthlyFee: 9.9 },
  });
  expect(created.status()).toBe(201);
  const contractId = (await created.json()).contract.id;

  // Vertragsseite: Das Abzeichen neben der Vertragsnummer zeigt "Beendet" (vorher "Aktiv") ...
  await page.goto(`/contracts/${contractId}`);
  await expect(page.locator('h1 .badge')).toHaveText('Beendet');
  // ... und der Knopf "Kündigen" ist ausgegraut, weil er nur bei Status "Active" aktiv ist
  await expect(page.getByRole('button', { name: 'Kündigen' })).toBeDisabled();

  // Auch direkt über die API lässt sich der beendete Vertrag nicht mehr kündigen:
  // 409 Conflict = "Nur aktive Verträge können gekündigt werden." (vorher 204, die Kündigung wurde gespeichert)
  const cancel = await page.request.post(`/api/contracts/${contractId}/cancel`, { data: { reason: 'Umzug' } });
  expect(cancel.status()).toBe(409);

  // Kundenseite: In der Vertragsliste steht in der Zeile mit dem Vertragsende 31.12.2025 der Status "Beendet"
  await page.goto(`/customers/${customerId}`);
  await expect(page.locator('.grid [role=row]').filter({ hasText: '31.12.2025' })).toContainText('Beendet');
});
