import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);
});

test('Kunde ohne Verträge löschen: nach Bestätigung weg, zurück zur Kundenliste', async ({ page }) => {
  const lastName = `Loeschen${Date.now()}`;
  const created = await page.request.post('/api/customers', { data: { firstName: 'Erika', lastName } });
  expect(created.status()).toBe(201);
  const { id, customerNumber } = await created.json();

  await page.goto(`/customers/${id}`);
  await page.locator('.page-header').getByRole('button', { name: 'Löschen' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await expect(dialog).toContainText(`Erika ${lastName} (${customerNumber})`);
  await dialog.getByRole('button', { name: 'Löschen' }).click();

  await expect(page).toHaveURL(/\/customers$/);
  expect((await page.request.get(`/api/customers/${id}`)).status()).toBe(404);
});

test('Kunde mit Verträgen kann nicht gelöscht werden', async ({ page }) => {
  // Kundin 64 (Clara Quast) aus den Demo-Daten hat Vertrag 77
  await page.goto('/customers/64');
  const button = page.locator('.page-header').getByRole('button', { name: 'Löschen' });
  await expect(button).toBeDisabled();
  await expect(button).toHaveAttribute('title', 'Kunden mit Verträgen können nicht gelöscht werden.');

  expect((await page.request.delete('/api/customers/64')).status()).toBe(409);
});
