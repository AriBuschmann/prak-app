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

test('zu lange Eingaben bei E-Mail, Telefon, Straße und Ort werden gemeldet', async ({ page }) => {
  // Bug: Der Validator prüfte die Länge dieser vier Felder nicht, zu lange Werte führten in der Datenbank zu Fehler 500.
  // Jetzt antwortet der Server mit 400 und je einer Meldung pro Feld. Weil das Speichern abgelehnt wird, legt der Test keinen Kunden an.

  // Dialog "Neuer Kunde" öffnen; nur den sichtbaren Dialog nehmen (auf der Seite gibt es weitere, versteckte Dialoge)
  await page.getByRole('button', { name: 'Neuer Kunde' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  // Pflichtfelder gültig ausfüllen, damit nur die vier Längenfehler übrig bleiben
  await dialog.locator('input[name=firstName]').fill('Erika');
  await dialog.locator('input[name=lastName]').fill('Laengentest');
  // Jedes Feld genau ein Zeichen länger als erlaubt. 'x'.repeat(n) ergibt einen Text aus n-mal x
  // E-Mail: 189 + 12 ("@example.org") = 201 Zeichen; gültiges Format, damit die Längen-Meldung kommt und nicht "ungültig"
  await dialog.locator('input[name=email]').fill('a'.repeat(189) + '@example.org');
  // Telefon: 31 Zeichen (erlaubt sind 30)
  await dialog.locator('input[name=phone]').fill('1'.repeat(31));
  // Straße: 101 Zeichen (erlaubt sind 100)
  await dialog.locator('input[name=street]').fill('S'.repeat(101));
  // Ort: 81 Zeichen (erlaubt sind 80)
  await dialog.locator('input[name=city]').fill('O'.repeat(81));
  // Speichern schickt POST /api/customers; vor der Korrektur kam hier Fehler 500 und keine Meldung an den Feldern
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  // Alle vier Meldungen vom Server müssen im Dialog sichtbar sein.
  // Für Telefon, Straße und Ort gab es im Formular (customer-form.html) vorher gar keine Zeile für Fehlermeldungen
  await expect(dialog.getByText('E-Mail-Adresse darf höchstens 200 Zeichen lang sein.')).toBeVisible();
  await expect(dialog.getByText('Telefon darf höchstens 30 Zeichen lang sein.')).toBeVisible();
  await expect(dialog.getByText('Straße darf höchstens 100 Zeichen lang sein.')).toBeVisible();
  await expect(dialog.getByText('Ort darf höchstens 80 Zeichen lang sein.')).toBeVisible();
  // Nicht gespeichert: Die Adresse endet weiter auf /customers (Kundenliste), es wurde keine Kundenseite /customers/<Id> geöffnet
  await expect(page).toHaveURL(/\/customers$/);
});

test('zweistellig getipptes Jahr beim Vertragsbeginn wird gemeldet', async ({ page }) => {
  // Bug: Tippt man im Datumsfeld beim Jahr nur "26", macht der Browser daraus das Jahr 0026. Der Server stürzte
  // beim Speichern mit Fehler 500 ab. Jetzt antwortet er mit 400 und einer Meldung am Feld, es wird nichts gespeichert.

  // Kundenseite von Kunde 2 aus den Demo-Daten öffnen und den Dialog "Neuer Vertrag" aufmachen
  await page.goto('/customers/2');
  await page.getByRole('button', { name: 'Neuer Vertrag' }).click();
  // Nur den sichtbaren Dialog nehmen (der Dialog "Kunde bearbeiten" ist versteckt auch auf der Seite)
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  // Die übrigen Pflichtfelder gültig ausfüllen, damit nur der Fehler beim Datum übrig bleibt:
  // index: 1 ist die erste echte Vertragsart (index 0 ist "Bitte wählen")
  await dialog.locator('select[name=contractTypeId]').selectOption({ index: 1 });
  await dialog.locator('input[name=monthlyFee]').fill('9.90');
  // Genau den Wert setzen, der beim Tippen von "01 10 26" entsteht. fill() statt Eintippen, weil das Tippen
  // in das Datumsfeld vom Timing abhängt und der Test sonst mal grün, mal rot wäre.
  // Das Datumsfeld erwartet den Wert immer im Format JJJJ-MM-TT, egal wie es im Browser angezeigt wird
  await dialog.locator('input[name=startDate]').fill('0026-10-01');
  // Speichern schickt POST /api/contracts; vor der Korrektur kam hier Fehler 500 ohne Meldung im Dialog
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  // Die Meldung vom Server steht im Dialog am Feld "Vertragsbeginn"
  await expect(dialog.getByText('Vertragsbeginn muss zwischen 01.01.2000 und 31.12.2099 liegen.')).toBeVisible();
  // Nicht gespeichert: Wir sind noch auf der Kundenseite, es wurde keine Vertragsseite /contracts/<Id> geöffnet
  await expect(page).toHaveURL(/\/customers\/2$/);
});

// Bug: Die Suche startete nur beim Loslassen einer Taste (keyup). Leeren mit dem X und Einfügen mit der Maus
// drücken keine Taste, darum passierte nichts. Jetzt startet sie beim Ereignis "input", das bei jeder Änderung kommt.
// Beide Tests prüfen auf "1 Kunden" statt auf die Gesamtzahl: Andere Tests legen gleichzeitig Kunden an,
// die Gesamtzahl kann sich also während des Tests ändern, die Zahl der Treffer für K-100063 nicht.

test('Suchfeld mit dem X leeren zeigt wieder alle Kunden', async ({ page }) => {
  // Das Suchfeld und die Trefferanzeige rechts daneben ("106 Kunden"); beforeEach hat die Kundenliste schon geöffnet
  const search = page.getByTestId('customer-search');
  const found = page.locator('.toolbar .muted');
  // Zuerst eine Suche mit genau einem Treffer: pressSequentially() tippt Buchstabe für Buchstabe mit echten Tastendrücken
  await search.pressSequentially('K-100063');
  await expect(found).toHaveText('1 Kunden');

  // Position und Größe des Suchfelds holen (das ! sagt TypeScript: das Feld ist sicher sichtbar, also nicht null)
  const box = (await search.boundingBox())!;
  // Mit der Maus auf das X klicken. Das X ist kein eigenes Element, das man suchen kann, sondern wird vom Browser
  // im Feld gezeichnet: Es sitzt (in Chromium) 16 Pixel vom rechten Rand, senkrecht in der Mitte des Felds
  await search.click({ position: { x: box.width - 16, y: box.height / 2 } });

  // Das X hat das Feld geleert (sonst hätte der Klick das X verfehlt) ...
  await expect(search).toHaveValue('');
  // ... und die Suche wurde neu gestartet: Es wird nicht mehr nur der eine Treffer angezeigt (vor der Korrektur blieb "1 Kunden" stehen)
  await expect(found).not.toHaveText('1 Kunden');
});

test('Einfügen ohne Tastendruck startet die Suche', async ({ page }) => {
  // fill() setzt den Text auf einmal ein, ohne eine Taste zu drücken, genau wie Einfügen mit der Maus (Rechtsklick → Einfügen).
  // Das Rechtsklick-Menü des Browsers selbst kann Playwright nicht bedienen
  await page.getByTestId('customer-search').fill('K-100063');

  // Die Suche ist gestartet und hat genau einen Kunden gefunden (vor der Korrektur blieb die Gesamtzahl, z. B. "106 Kunden", stehen)
  await expect(page.locator('.toolbar .muted')).toHaveText('1 Kunden');
});
