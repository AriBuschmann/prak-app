import { test, expect } from '@playwright/test';

test('nicht angemeldete Benutzer landen auf der Anmeldeseite', async ({ page }) => {
  await page.goto('/customers');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Anmeldung' })).toBeVisible();
});

test('Anmeldung zeigt die Kundenliste', async ({ page }) => {
  await page.goto('/');

  await page.locator('button[data-user="praktikum"]').click();

  await expect(page).toHaveURL(/\/customers$/);
  await expect(page.getByTestId('current-user')).toHaveText('Praktikum');
  await expect(page.getByRole('heading', { name: 'Kunden' })).toBeVisible();
  await expect(page.locator('.grid [role=row]').first()).toBeVisible();
});
