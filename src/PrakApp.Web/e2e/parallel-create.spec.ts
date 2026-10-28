import { test, expect } from '@playwright/test';

const COUNT = 10;

test('gleichzeitig angelegte Kunden und Verträge bekommen eigene Nummern', async ({ page }) => {
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);
  const stamp = Date.now();

  const customerResponses = await Promise.all(
    Array.from({ length: COUNT }, (_, i) =>
      page.request.post('/api/customers', { data: { firstName: 'Parallel', lastName: `Test${stamp}-${i}` } }),
    ),
  );
  expect(customerResponses.map((r) => r.status())).toEqual(Array(COUNT).fill(201));
  const customers = await Promise.all(customerResponses.map((r) => r.json()));
  expect(new Set(customers.map((c) => c.customerNumber)).size).toBe(COUNT);

  const contractResponses = await Promise.all(
    Array.from({ length: COUNT }, () =>
      page.request.post('/api/contracts', {
        data: { customerId: customers[0].id, contractTypeId: 1, startDate: '2026-10-01', endDate: null, monthlyFee: 9.9 },
      }),
    ),
  );
  expect(contractResponses.map((r) => r.status())).toEqual(Array(COUNT).fill(201));
  const contracts = await Promise.all(contractResponses.map((r) => r.json()));
  expect(new Set(contracts.map((c) => c.contract.contractNumber)).size).toBe(COUNT);
});
