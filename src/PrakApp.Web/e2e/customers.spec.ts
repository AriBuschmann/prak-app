import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.goto('/login');
  await page.locator('button[data-user="praktikum"]').click();
  await expect(page).toHaveURL(/\/customers$/);
});

test('neuen Kunden anlegen', async ({ page }) => {
  const lastName = `Testkunde${Date.now()}`;

  await page.getByRole('button', { name: 'Neuer Kunde' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.locator('input[name=firstName]').fill('Erika');
  await dialog.locator('input[name=lastName]').fill(lastName);
  await dialog.locator('input[name=postalCode]').fill('24103');
  await dialog.locator('input[name=city]').fill('Kiel');
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  await expect(page).toHaveURL(/\/customers\/\d+$/);
  await expect(page.getByTestId('customer-name')).toHaveText(`Erika ${lastName}`);
});

test('Pflichtfelder werden geprüft', async ({ page }) => {
  await page.getByRole('button', { name: 'Neuer Kunde' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  await expect(dialog.getByText('Nachname ist erforderlich.')).toBeVisible();
});
