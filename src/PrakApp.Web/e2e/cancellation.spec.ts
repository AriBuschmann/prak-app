import { test, expect } from '@playwright/test';

// Ablauf "Vertrag kündigen" (docs/features/kuendigung.md).
// Ein Vertrag kann nur einmal gekündigt werden. Deshalb legt sich der Test bei jedem Lauf über die API
// einen eigenen Kunden und Vertrag an und kündigt genau diesen über die Oberfläche.

const two = (n: number) => String(n).padStart(2, '0');

test('Vertrag kündigen: Dialog, Pflichtfeld, Vertragsende und Anzeige danach', async ({ page }) => {
  // Heute in deutscher Zeit, z. B. "2026-10-22" (en-CA liefert genau das Format JJJJ-MM-TT)
  const [year, month, day] = new Date()
    .toLocaleDateString('en-CA', { timeZone: 'Europe/Berlin' })
    .split('-')
    .map(Number);
  // PREMIUM-Vertrag ab dem 1. dieses Monats: 24 Monate Mindestlaufzeit, 3 Monate Kündigungsfrist.
  // Die Mindestlaufzeit endet am letzten Tag des Monats 23 Monate später und liegt immer nach dem
  // Ende der Kündigungsfrist. Damit ist das erwartete Vertragsende an jedem Tag eindeutig.
  const startDate = `${year}-${two(month)}-01`;
  const lastDay = new Date(Date.UTC(year, month - 1 + 24, 0)); // Tag 0 = letzter Tag des Vormonats
  const expectedEnd = `${two(lastDay.getUTCDate())}.${two(lastDay.getUTCMonth() + 1)}.${lastDay.getUTCFullYear()}`;
  const today = `${two(day)}.${two(month)}.${year}`;
  const reason = `Umzug (E2E ${Date.now()})`;

  // Anmelden
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);

  // Testdaten über die API anlegen (page.request benutzt dasselbe Anmelde-Cookie wie die Seite)
  const customer = await page.request.post('/api/customers', {
    data: { firstName: 'Erika', lastName: `Kuendigung${Date.now()}` },
  });
  expect(customer.status()).toBe(201);
  const created = await page.request.post('/api/contracts', {
    data: { customerId: (await customer.json()).id, contractTypeId: 3, startDate, endDate: null, monthlyFee: 39.9 },
  });
  expect(created.status()).toBe(201);
  const { id, contractNumber } = (await created.json()).contract;

  // Vertragsseite öffnen: Vertrag ist aktiv
  await page.goto(`/contracts/${id}`);
  await expect(page.getByRole('heading', { name: new RegExp(contractNumber) })).toBeVisible();
  await expect(page.locator('h1 .badge')).toHaveText('Aktiv');

  // Dialog öffnen: zeigt das voraussichtliche Vertragsende
  await page.locator('.actions').getByRole('button', { name: 'Kündigen' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await expect(dialog.getByTestId('expected-end-date')).toHaveText(expectedEnd);

  // Ohne Grund: Meldung am Feld, nichts gespeichert
  await dialog.getByRole('button', { name: 'Kündigen' }).click();
  await expect(dialog.getByText('Kündigungsgrund ist erforderlich.')).toBeVisible();

  // Mit Grund kündigen: Seite wird neu geladen und zeigt Status, Ende, Datum und Grund
  await dialog.locator('textarea[name=reason]').fill(reason);
  await dialog.getByRole('button', { name: 'Kündigen' }).click();
  await expect(page.locator('h1 .badge')).toHaveText('Gekündigt');
  const details = page.locator('.details');
  await expect(details).toContainText(expectedEnd);
  await expect(details).toContainText(today);
  await expect(details).toContainText(reason);
  await expect(page.locator('.actions').getByRole('button', { name: 'Kündigen' })).toBeDisabled();

  // Auch nach dem erneuten Laden ist die Kündigung gespeichert
  await page.reload();
  await expect(page.locator('h1 .badge')).toHaveText('Gekündigt');
  await expect(details).toContainText(reason);
});
