import { test, expect } from '@playwright/test';

const two = (n: number) => String(n).padStart(2, '0');

test('Kündigung zurücknehmen: Vertrag wieder aktiv, ursprüngliches Ende gilt wieder', async ({ page }) => {
  const [year, month] = new Date()
    .toLocaleDateString('en-CA', { timeZone: 'Europe/Berlin' })
    .split('-')
    .map(Number);
  const startDate = `${year}-${two(month)}-01`;
  const originalEnd = `${year + 1}-12-31`;

  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);

  const customer = await page.request.post('/api/customers', {
    data: { firstName: 'Erika', lastName: `Ruecknahme${Date.now()}` },
  });
  expect(customer.status()).toBe(201);
  const created = await page.request.post('/api/contracts', {
    data: { customerId: (await customer.json()).id, contractTypeId: 1, startDate, endDate: originalEnd, monthlyFee: 9.9 },
  });
  expect(created.status()).toBe(201);
  const contractId = (await created.json()).contract.id;
  const cancelled = await page.request.post(`/api/contracts/${contractId}/cancel`, { data: { reason: 'Umzug' } });
  expect(cancelled.status()).toBe(204);

  await page.goto(`/contracts/${contractId}`);
  await expect(page.locator('h1 .badge')).toHaveText('Gekündigt');
  await expect(page.getByTestId('contract-end-date')).not.toHaveText(`31.12.${year + 1}`);

  await page.locator('.page-header').getByRole('button', { name: 'Kündigung zurücknehmen' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await expect(dialog).toContainText(`31.12.${year + 1}`);
  await dialog.getByRole('button', { name: 'Kündigung zurücknehmen' }).click();

  await expect(page.locator('h1 .badge')).toHaveText('Aktiv');
  await expect(page.getByTestId('contract-end-date')).toHaveText(`31.12.${year + 1}`);
  await expect(page.getByRole('button', { name: 'Kündigen', exact: true })).toBeEnabled();
  await expect(page.locator('.page-header').getByRole('button', { name: 'Kündigung zurücknehmen' })).toHaveCount(0);

  const again = await page.request.post(`/api/contracts/${contractId}/undo-cancellation`);
  expect(again.status()).toBe(409);
});
