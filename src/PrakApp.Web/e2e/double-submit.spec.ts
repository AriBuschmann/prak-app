import { test, expect, type Page } from '@playwright/test';

// Ein Doppelklick auf "Speichern" darf nur eine Anfrage an den Server schicken und nur einen Datensatz anlegen.

test.beforeEach(async ({ page }) => {
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);
});

function countPosts(page: Page, path: RegExp): () => number {
  let count = 0;
  page.on('request', (request) => {
    if (request.method() === 'POST' && path.test(new URL(request.url()).pathname)) count++;
  });
  return () => count;
}

async function createCustomer(page: Page): Promise<number> {
  const response = await page.request.post('/api/customers', {
    data: { firstName: 'Erika', lastName: `Doppelklick${Date.now()}` },
  });
  expect(response.status()).toBe(201);
  return (await response.json()).id;
}

test('Doppelklick legt nur einen Kunden an', async ({ page }) => {
  const lastName = `Doppelklick${Date.now()}`;
  const posts = countPosts(page, /^\/api\/customers$/);

  await page.getByRole('button', { name: 'Neuer Kunde' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.locator('input[name=firstName]').fill('Erika');
  await dialog.locator('input[name=lastName]').fill(lastName);
  await dialog.getByRole('button', { name: 'Speichern' }).dblclick();

  await expect(page).toHaveURL(/\/customers\/\d+$/);
  await page.waitForLoadState('networkidle');
  expect(posts()).toBe(1);
  const customers = await (await page.request.get(`/api/customers?q=${lastName}`)).json();
  expect(customers).toHaveLength(1);
});

test('Doppelklick legt nur einen Vertrag an', async ({ page }) => {
  const customerId = await createCustomer(page);
  const posts = countPosts(page, /^\/api\/contracts$/);

  await page.goto(`/customers/${customerId}`);
  await page.getByRole('button', { name: 'Neuer Vertrag' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.locator('select[name=contractTypeId]').selectOption({ index: 1 });
  await dialog.locator('input[name=monthlyFee]').fill('39.90');
  await dialog.getByRole('button', { name: 'Speichern' }).dblclick();

  await expect(page).toHaveURL(/\/contracts\/\d+$/);
  await page.waitForLoadState('networkidle');
  expect(posts()).toBe(1);
  const contracts = await (await page.request.get(`/api/customers/${customerId}/contracts`)).json();
  expect(contracts).toHaveLength(1);
});

test('Doppelklick legt nur eine Zahlung an', async ({ page }) => {
  const customerId = await createCustomer(page);
  const contract = await page.request.post('/api/contracts', {
    data: { customerId, contractTypeId: 1, startDate: '2026-01-01', endDate: null, monthlyFee: 19.9 },
  });
  expect(contract.status()).toBe(201);
  const contractId = (await contract.json()).contract.id;
  const reference = `Doppelklick${Date.now()}`;
  const posts = countPosts(page, /^\/api\/contracts\/\d+\/payments$/);

  await page.goto(`/contracts/${contractId}`);
  await page.getByRole('button', { name: 'Zahlung erfassen' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.locator('input[name=reference]').fill(reference);
  await dialog.getByRole('button', { name: 'Speichern' }).dblclick();

  await expect(page.locator('.grid [role=row]').filter({ hasText: reference }).first()).toBeVisible();
  await page.waitForLoadState('networkidle');
  expect(posts()).toBe(1);
  const payments = await (await page.request.get(`/api/contracts/${contractId}/payments`)).json();
  expect(payments).toHaveLength(1);
});

// Wie oben, aber für das Ändern (PUT) statt Anlegen: Vorher fehlte beim Speichern im Dialog "Kunde bearbeiten" der
// Schutz, ein Doppelklick schickte die Änderung zweimal. Harmlos (zweimal dasselbe), aber unnötig und uneinheitlich
test('Doppelklick beim Bearbeiten schickt die Änderung nur einmal', async ({ page }) => {
  const customerId = await createCustomer(page);
  let puts = 0;
  page.on('request', (request) => {
    if (request.method() === 'PUT' && new URL(request.url()).pathname === `/api/customers/${customerId}`) puts++;
  });

  await page.goto(`/customers/${customerId}`);
  await page.getByRole('button', { name: 'Bearbeiten' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.locator('input[name=city]').fill('Kiel');
  await dialog.getByRole('button', { name: 'Speichern' }).dblclick();

  await expect(dialog).toBeHidden();
  await expect(page.locator('.details')).toContainText('Kiel');
  await page.waitForLoadState('networkidle');
  expect(puts).toBe(1);
});

// Vorher schickte ein Doppelklick auf "Kündigen" im Dialog zwei Kündigungen. Die zweite lehnte der Server mit 409 ab
// ("Nur aktive Verträge können gekündigt werden."), und oben erschien der rote Hinweis "Unerwarteter Fehler"
test('Doppelklick auf Kündigen kündigt nur einmal und zeigt keinen Fehler', async ({ page }) => {
  const customerId = await createCustomer(page);
  const contract = await page.request.post('/api/contracts', {
    data: { customerId, contractTypeId: 1, startDate: '2026-01-01', endDate: null, monthlyFee: 9.9 },
  });
  expect(contract.status()).toBe(201);
  const contractId = (await contract.json()).contract.id;
  const posts = countPosts(page, /^\/api\/contracts\/\d+\/cancel$/);

  await page.goto(`/contracts/${contractId}`);
  await page.locator('.actions').getByRole('button', { name: 'Kündigen' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.locator('textarea[name=reason]').fill('Umzug');
  await dialog.getByRole('button', { name: 'Kündigen' }).dblclick();

  await expect(page.locator('h1 .badge')).toHaveText('Gekündigt');
  await page.waitForLoadState('networkidle');
  expect(posts()).toBe(1);
  await expect(page.locator('.error-banner')).toHaveCount(0);
});
