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

test('Nachname mit 100 Zeichen kann gespeichert werden', async ({ page }) => {
  // Bug aus den Logdateien: Der Validator erlaubt 100 Zeichen, die Datenbank-Spalte hatte nur Platz für 80 → Fehler 500.
  // Der Name aus den Logs (dort auf 80 Zeichen abgeschnitten), verlängert auf genau 100 Zeichen = Höchstlänge
  const lastName = 'Albers-Lindqvist von Hohenstein-Marquardt zu Rabenau-Westerholt und Kleinmachnow-Freiherr von Testen';
  // Sicherheitsprüfung für den Test selbst: Hat der Name wirklich genau 100 Zeichen? Sonst würde der Test die Grenze verfehlen
  expect(lastName).toHaveLength(100);

  // Dialog "Neuer Kunde" öffnen (beforeEach oben hat schon angemeldet und die Kundenliste geöffnet)
  await page.getByRole('button', { name: 'Neuer Kunde' }).click();
  // Nur den sichtbaren Dialog nehmen: Auf der Seite gibt es noch weitere, versteckte Dialoge mit .dialog-body
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  // Pflichtfelder ausfüllen: Vorname und den 100 Zeichen langen Nachnamen
  await dialog.locator('input[name=firstName]').fill('Erika');
  await dialog.locator('input[name=lastName]').fill(lastName);
  // Speichern schickt POST (neuen Datensatz, also Kunden, anlegen) /api/customers, vor der Korrektur antwortete der Server hier mit Fehler 500
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  // Gespeichert: Die Kundenseite öffnet sich und zeigt den vollständigen Namen
  // Die Adresse muss auf /customers/<Id des neuen Kunden> enden, \d+ steht für eine oder mehrere Ziffern
  await expect(page).toHaveURL(/\/customers\/\d+$/);
  // Die Überschrift zeigt "Vorname Nachname". So ist geprüft, dass alle 100 Zeichen gespeichert und nicht abgeschnitten wurden
  await expect(page.getByTestId('customer-name')).toHaveText(`Erika ${lastName}`);
});

test('Bearbeiten → Abbrechen verwirft die Änderungen', async ({ page }) => {
  // Bug: Das Formular bearbeitete direkt den angezeigten Kunden, Änderungen blieben nach "Abbrechen" auf der Seite stehen.
  // Der Test speichert nichts, er legt also keinen Kunden an und ändert keine Daten in der Datenbank.

  // Ersten Kunden der Liste öffnen: die erste Zelle, deren Text eine Kundennummer wie "K-100040" ist
  await page.locator('.grid div[role=gridcell]').filter({ hasText: /^K-\d+$/ }).first().click();
  // Die Kundenseite ist offen, wenn die Adresse auf /customers/<Id> endet
  await expect(page).toHaveURL(/\/customers\/\d+$/);
  // Die Überschrift "Vorname Nachname" merken, um sie nach dem Abbrechen zu vergleichen
  const heading = page.getByTestId('customer-name');
  // textContent() kann null liefern, das ! sagt TypeScript: hier ist sicher ein Text vorhanden
  const originalName = (await heading.textContent())!;

  // Dialog "Kunde bearbeiten" öffnen
  await page.getByRole('button', { name: 'Bearbeiten' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  // Eingabefeld für den Vornamen, dessen ursprünglichen Inhalt merken
  const firstName = dialog.locator('input[name=firstName]');
  const originalFirstName = await firstName.inputValue();
  // Vornamen ändern und dann abbrechen statt speichern
  await firstName.fill('Geändert');
  await dialog.getByRole('button', { name: 'Abbrechen' }).click();

  // Prüfung 1: Die Überschrift zeigt weiter den alten Namen (vor der Korrektur stand hier "Geändert Albers")
  await expect(heading).toHaveText(originalName);
  // Prüfung 2: Beim erneuten Öffnen steht wieder der alte Vorname im Feld, die Eingabe "Geändert" ist verworfen.
  // Der Locator firstName kann weiterverwendet werden, Playwright sucht das Feld bei jeder Prüfung neu
  await page.getByRole('button', { name: 'Bearbeiten' }).click();
  await expect(firstName).toHaveValue(originalFirstName);
});
