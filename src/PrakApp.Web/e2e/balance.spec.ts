import { test, expect } from '@playwright/test';

// Bug: Das Beitragskonto zählte einen Monat zu wenig. Laut Handbuch (docs/handbuch/02-tarife.md) ist der erste
// Beitrag schon am Tag des Vertragsbeginns fällig. Ein Vertrag, der heute beginnt, muss also 1 fälligen Monat zeigen.
// Die Randfälle (Tag vor/an der nächsten Fälligkeit, Vertragsende) prüfen die Unit-Tests in BalanceCalculatorTests.cs;
// dieser Test prüft, dass die Zahl auch richtig in der Oberfläche ankommt.
test('Beitragskonto zählt den ersten Beitrag am Tag des Vertragsbeginns', async ({ page }) => {
  // Heute in deutscher Zeit als Text "JJJJ-MM-TT" (die Schreibweise en-CA liefert genau dieses Format)
  const today = new Date().toLocaleDateString('en-CA', { timeZone: 'Europe/Berlin' });

  // Anmelden
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);

  // Testdaten über die API anlegen (wie in cancellation.spec.ts): page.request benutzt dasselbe Anmelde-Cookie wie die Seite.
  // Ein eigener Kunde mit eindeutigem Namen (Date.now() = Millisekunden seit 1970), 201 heißt "angelegt"
  const customer = await page.request.post('/api/customers', {
    data: { firstName: 'Erika', lastName: `Beitragskonto${Date.now()}` },
  });
  expect(customer.status()).toBe(201);
  // Vertrag für diesen Kunden: Basis-Tarif (Id 1), Beginn heute, kein Ende, 20 € im Monat
  const created = await page.request.post('/api/contracts', {
    data: { customerId: (await customer.json()).id, contractTypeId: 1, startDate: today, endDate: null, monthlyFee: 20 },
  });
  expect(created.status()).toBe(201);

  // Die Vertragsseite öffnen; die Id steht in der Antwort unter contract.id
  await page.goto(`/contracts/${(await created.json()).contract.id}`);

  // Beitragskonto: 1 fälliger Monat (vor der Korrektur stand hier 0)
  await expect(page.getByTestId('months-due')).toHaveText('1');
  // Saldo = Soll − Bezahlt = 20,00 € − 0 € = 20,00 € (positiv = Rückstand; vorher 0,00 €)
  await expect(page.getByTestId('balance')).toHaveText('20,00 €');
});
