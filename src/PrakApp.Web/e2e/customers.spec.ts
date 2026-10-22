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

test('Kunden aus der Liste öffnen', async ({ page }) => {
  const customerNumber = page.locator('.grid div[role=gridcell]').filter({ hasText: /^K-\d+$/ }).first();
  const number = (await customerNumber.textContent())!.trim();

  await customerNumber.click();

  await expect(page).toHaveURL(/\/customers\/\d+$/);
  await expect(page.locator('.breadcrumb')).toContainText(number);
});

test('Pflichtfelder werden geprüft', async ({ page }) => {
  await page.getByRole('button', { name: 'Neuer Kunde' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  await expect(dialog.getByText('Nachname ist erforderlich.')).toBeVisible();
});

test('Spalte "Verträge" zählt Verträge, nicht Zahlungen', async ({ page }) => {
  // Beobachtung: K-100063 (Simon Wagner) hat in den Demo-Daten 3 Verträge mit zusammen 43 Zahlungen

  // 1. Die Kundennummer in das Suchfeld schreiben (das Feld hat data-testid="customer-search")
  await page.getByTestId('customer-search').fill('K-100063');
  // 2. Enter drücken: fill() aus Schritt 1 drückt keine Taste, die Suche startet aber erst beim Loslassen einer Taste (keyup)
  await page.getByTestId('customer-search').press('Enter');
  // 3. In der Tabelle die Zeile suchen, die die Kundennummer K-100063 enthält
  const row = page.locator('.grid [role=row]').filter({ hasText: 'K-100063' });
  // 4. Die 5. Zelle dieser Zeile ist die Spalte "Verträge" (Zählung ab 0, also nth(4)): dort muss 3 stehen
  await expect(row.locator('[role=gridcell]').nth(4)).toHaveText('3');
});
