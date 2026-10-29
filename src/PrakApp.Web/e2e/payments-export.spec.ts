import { readFile } from 'node:fs/promises';
import { test, expect } from '@playwright/test';

const two = (n: number) => String(n).padStart(2, '0');

test('Zahlungen als CSV exportieren', async ({ page }) => {
  const [year, month] = new Date()
    .toLocaleDateString('en-CA', { timeZone: 'Europe/Berlin' })
    .split('-')
    .map(Number);
  const startDate = `${year}-${two(month)}-01`;

  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);

  const customer = await page.request.post('/api/customers', {
    data: { firstName: 'Erika', lastName: `Export${Date.now()}` },
  });
  expect(customer.status()).toBe(201);
  const created = await page.request.post('/api/contracts', {
    data: { customerId: (await customer.json()).id, contractTypeId: 1, startDate, endDate: null, monthlyFee: 9.9 },
  });
  expect(created.status()).toBe(201);
  const { id, contractNumber } = (await created.json()).contract;

  // Ohne Zahlungen gibt es nichts zu exportieren
  await page.goto(`/contracts/${id}`);
  await expect(page.locator('h1')).toContainText(contractNumber);
  await expect(page.getByRole('link', { name: 'Als CSV exportieren' })).toHaveCount(0);

  // Verwendungszweck mit Semikolon, Anführungszeichen und "=" am Anfang (würde Excel als Formel ausführen)
  const payment = await page.request.post(`/api/contracts/${id}/payments`, {
    data: { amount: 9.9, paymentDate: startDate, reference: '=SUMME(1;2) "Bonus"' },
  });
  expect(payment.status()).toBe(201);

  await page.reload();
  const downloadPromise = page.waitForEvent('download');
  await page.getByRole('link', { name: 'Als CSV exportieren' }).click();
  const download = await downloadPromise;

  expect(download.suggestedFilename()).toBe(`zahlungen-${contractNumber}.csv`);
  const content = await readFile(await download.path(), 'utf8');
  expect(content).toBe(
    '﻿' +
      'Vertragsnummer;Zahlungsdatum;Betrag;Verwendungszweck;Erfasst von\r\n' +
      `${contractNumber};01.${two(month)}.${year};9,90;"'=SUMME(1;2) ""Bonus""";praktikum\r\n`,
  );
});
