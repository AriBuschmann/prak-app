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
  // 2. Enter drücken: Stammt aus der Zeit, als die Suche erst beim Loslassen einer Taste (keyup) startete. Seit dem
  //    Bug-Fix "Suche reagiert nicht auf X und Einfügen" startet sie bei jeder Änderung im Feld (input), fill() reicht
  //    also schon. Der Tastendruck schadet nicht
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
  // Eindeutiger Vorname: Sonst gibt es den Kunden durch frühere Testläufe schon, und die App warnt vor einem doppelten
  // Kunden, statt zu speichern. Der Nachname, um den es in diesem Test geht, bleibt unverändert
  const firstName = `Erika${Date.now()}`;

  // Dialog "Neuer Kunde" öffnen (beforeEach oben hat schon angemeldet und die Kundenliste geöffnet)
  await page.getByRole('button', { name: 'Neuer Kunde' }).click();
  // Nur den sichtbaren Dialog nehmen: Auf der Seite gibt es noch weitere, versteckte Dialoge mit .dialog-body
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  // Pflichtfelder ausfüllen: Vorname und den 100 Zeichen langen Nachnamen
  await dialog.locator('input[name=firstName]').fill(firstName);
  await dialog.locator('input[name=lastName]').fill(lastName);
  // Speichern schickt POST (neuen Datensatz, also Kunden, anlegen) /api/customers, vor der Korrektur antwortete der Server hier mit Fehler 500
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  // Gespeichert: Die Kundenseite öffnet sich und zeigt den vollständigen Namen
  // Die Adresse muss auf /customers/<Id des neuen Kunden> enden, \d+ steht für eine oder mehrere Ziffern
  await expect(page).toHaveURL(/\/customers\/\d+$/);
  // Die Überschrift zeigt "Vorname Nachname". So ist geprüft, dass alle 100 Zeichen gespeichert und nicht abgeschnitten wurden
  await expect(page.getByTestId('customer-name')).toHaveText(`${firstName} ${lastName}`);
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

// Bug: "Angelegt am" zeigte nachts (0 bis 2 Uhr) das Datum vom Vortag, weil der Browser den UTC-Zeitpunkt
// aus der Datenbank als Ortszeit gelesen hat. Nachts um 1 Uhr können wir den Test nicht laufen lassen, darum geben wir
// dem Browser andere Zeitzonen: Dort tritt derselbe Fehler zu jeder Tageszeit auf.
// Kunde 40 aus den Demo-Daten wurde am 28.03.2023 um 10:15 Uhr UTC angelegt. Das ist in jeder Zeitzone ein anderes Datum:
const createdAtByTimezone = [
  // Berlin (Sommerzeit, UTC+2): 12:15 Uhr am 28.03. Hier stimmte die Anzeige auch vorher, der Fall prüft den Normalfall
  { timezoneId: 'Europe/Berlin', expected: '28.03.2023' },
  // Kiritimati (UTC+14): 00:15 Uhr am 29.03. Genau der Bug: kurz nach Mitternacht wurde vorher der Vortag (28.03.) angezeigt
  { timezoneId: 'Pacific/Kiritimati', expected: '29.03.2023' },
  // Pago Pago (UTC-11): 23:15 Uhr am 27.03. Der umgekehrte Fall, vorher wurde der Folgetag (28.03.) angezeigt
  { timezoneId: 'Pacific/Pago_Pago', expected: '27.03.2023' },
];

// Für jeden Eintrag der Liste einen eigenen Test anlegen. { timezoneId, expected } holt die beiden Werte aus dem Eintrag
for (const { timezoneId, expected } of createdAtByTimezone) {
  // describe fasst Tests zu einer Gruppe zusammen, der Name erscheint in der Ausgabe, z. B. "Zeitzone Pacific/Kiritimati"
  test.describe(`Zeitzone ${timezoneId}`, () => {
    // test.use gilt nur in dieser Gruppe: Der Browser läuft in dieser Zeitzone statt in Europe/Berlin aus playwright.config.ts
    test.use({ timezoneId });

    test('"Angelegt am" zeigt das Datum in Ortszeit', async ({ page }) => {
      // Kundenseite von Kunde 40 öffnen (angemeldet ist der Test schon durch beforeEach oben in der Datei)
      await page.goto('/customers/40');

      // Das Feld "Angelegt am" (data-testid in customer-detail.html) zeigt "Datum (angelegt von)".
      // Das Datum muss das Datum in der Zeitzone des Browsers sein; m.berger hat Kunde 40 in den Demo-Daten angelegt
      await expect(page.getByTestId('customer-created-at')).toHaveText(`${expected} (m.berger)`);
    });
  });
}

// Bug: Die Suche fand keinen Ort, obwohl docs/architektur.md das verspricht (in der SQL-Abfrage fehlte City).
// In den Demo-Daten wohnen genau 6 Kunden in Würzburg; die anderen Tests legen dort keine Kunden an, die Zahl bleibt also gleich
test('Suche findet Kunden über den Ort', async ({ page }) => {
  // Nur den Anfang des Orts eingeben: Wie bei den Namen wird nach dem Anfang gesucht, "Würz" muss "Würzburg" finden.
  // fill() reicht, die Suche startet bei jeder Änderung im Feld (Ereignis input) nach 300 ms
  await page.getByTestId('customer-search').fill('Würz');

  // Die Trefferanzeige neben dem Suchfeld zeigt die 6 Würzburger Kunden (vor der Korrektur: "0 Kunden")
  await expect(page.locator('.toolbar .muted')).toHaveText('6 Kunden');
  // In der Tabelle stehen 6 Zeilen, die "Würzburg" enthalten, also wirklich die Kunden aus diesem Ort
  await expect(page.locator('.grid [role=row]').filter({ hasText: 'Würzburg' })).toHaveCount(6);
});

// Bug: Die Links in der Pfadzeile ("Kunden › Name › Vertragsnummer") führten zum Router-Fehler AUR3401 statt zur Seite.
// Ohne "/" am Anfang hängt der Router den Pfad an die aktuelle Seite an (z. B. /contracts/77/customers), diese Route
// gibt es nicht. Korrektur: load="/customers" in contract-detail.html und customer-detail.html.
// test.describe fasst die drei Tests zu einer Gruppe "Pfadzeile" zusammen (angemeldet durch beforeEach oben in der Datei)
test.describe('Pfadzeile', () => {
  test('Vertragsseite: "Kunden" führt zur Kundenliste', async ({ page }) => {
    // Vertrag 77 aus den Demo-Daten öffnen und den ersten Link der Pfadzeile anklicken (nth(0) = erster, Zählung ab 0)
    await page.goto('/contracts/77');
    await page.locator('.breadcrumb a').nth(0).click();

    // Die Kundenliste ist offen: Adresse endet auf /customers (vorher blieb sie bei /contracts/77) ...
    await expect(page).toHaveURL(/\/customers$/);
    // ... und die Überschrift "Kunden" ist zu sehen
    await expect(page.getByRole('heading', { name: 'Kunden' })).toBeVisible();
    // Kein rotes Fehler-Banner (vorher: "Unerwarteter Fehler AUR3401 ...")
    await expect(page.locator('.error-banner')).toHaveCount(0);
  });

  test('Vertragsseite: Kundenname führt zur Kundenseite', async ({ page }) => {
    await page.goto('/contracts/77');
    // Zweiter Link der Pfadzeile ist der Name des Kunden, dem der Vertrag gehört
    const customerLink = page.locator('.breadcrumb a').nth(1);
    // Den Namen aus dem Link lesen statt ihn fest einzutragen; trim() entfernt Leerzeichen am Anfang und Ende,
    // das ! sagt TypeScript, dass textContent() hier nicht null ist
    const customerName = (await customerLink.textContent())!.trim();
    await customerLink.click();

    // Die Kundenseite ist offen: Adresse /customers/<Id> (\d+ = eine oder mehrere Ziffern) ...
    await expect(page).toHaveURL(/\/customers\/\d+$/);
    // ... und es ist der richtige Kunde: Die Überschrift zeigt denselben Namen wie der angeklickte Link
    await expect(page.getByTestId('customer-name')).toHaveText(customerName);
    await expect(page.locator('.error-banner')).toHaveCount(0);
  });

  test('Kundenseite: "Kunden" führt zur Kundenliste', async ({ page }) => {
    // Auf der Kundenseite hat die Pfadzeile nur einen Link ("Kunden"), danach folgt die Kundennummer als Text
    await page.goto('/customers/40');
    await page.locator('.breadcrumb a').nth(0).click();

    // Wie im ersten Test: Kundenliste offen, kein Fehler (vorher: Adresse /customers/40/customers, Fehler AUR3401)
    await expect(page).toHaveURL(/\/customers$/);
    await expect(page.getByRole('heading', { name: 'Kunden' })).toBeVisible();
    await expect(page.locator('.error-banner')).toHaveCount(0);
  });
});

test('Warnung vor doppeltem Kunden (gleicher Name und PLZ), danach "Trotzdem anlegen"', async ({ page }) => {
  const lastName = `Doppelt${Date.now()}`;
  const existing = await page.request.post('/api/customers', {
    data: { firstName: 'Erika', lastName, postalCode: '24103', city: 'Kiel' },
  });
  expect(existing.status()).toBe(201);
  const existingNumber = (await existing.json()).customerNumber;
  let posted = 0;
  page.on('request', (request) => {
    if (request.method() === 'POST' && new URL(request.url()).pathname === '/api/customers') posted++;
  });

  await page.getByRole('button', { name: 'Neuer Kunde' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.locator('input[name=firstName]').fill('erika');
  await dialog.locator('input[name=lastName]').fill(lastName.toUpperCase());
  await dialog.locator('input[name=postalCode]').fill('24103');
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  await expect(dialog.getByTestId('duplicate-warning')).toContainText(existingNumber);
  await expect(page).toHaveURL(/\/customers$/);
  expect(posted).toBe(0);

  await dialog.getByRole('button', { name: 'Trotzdem anlegen' }).click();
  await expect(page).toHaveURL(/\/customers\/\d+$/);
  expect(posted).toBe(1);
});

test('Suche nach Vertragsnummer führt direkt zum Vertrag', async ({ page }) => {
  await page.getByTestId('customer-search').fill('V-2023-0077');

  const hits = page.getByTestId('contract-hits');
  await expect(hits).toContainText('Clara Quast');
  await hits.getByRole('link', { name: 'V-2023-0077' }).click();

  await expect(page).toHaveURL(/\/contracts\/77$/);
  await expect(page.getByRole('heading', { name: /V-2023-0077/ })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Zahlung erfassen' })).toBeVisible();
});

test('Bearbeiten → Speichern übernimmt die Änderungen und speichert sie in der Datenbank', async ({ page }) => {
  // Eigenen Kunden anlegen, damit die Demo-Kunden nicht verändert werden
  const lastName = `Bearbeiten${Date.now()}`;
  const created = await page.request.post('/api/customers', {
    data: { firstName: 'Erika', lastName, postalCode: '24103', city: 'Kiel' },
  });
  expect(created.status()).toBe(201);
  const { id } = await created.json();

  await page.goto(`/customers/${id}`);
  await page.getByRole('button', { name: 'Bearbeiten' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  // Vorname und Ort ändern, dann speichern (PUT /api/customers/{id})
  await dialog.locator('input[name=firstName]').fill('Erik');
  await dialog.locator('input[name=city]').fill('Lübeck');
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  // Der Dialog schließt sich und die Seite zeigt sofort die neuen Daten
  await expect(dialog).toBeHidden();
  await expect(page.getByTestId('customer-name')).toHaveText(`Erik ${lastName}`);
  await expect(page.locator('.details')).toContainText('24103 Lübeck');

  // Nach dem Neuladen kommen die Daten frisch vom Server, also aus der Datenbank
  await page.reload();
  await expect(page.getByTestId('customer-name')).toHaveText(`Erik ${lastName}`);
  await expect(page.locator('.details')).toContainText('24103 Lübeck');
  // Zusätzlich direkt über die API: gespeichert sind die neuen Werte, unverändert die übrigen
  const saved = await (await page.request.get(`/api/customers/${id}`)).json();
  expect(saved).toMatchObject({ firstName: 'Erik', lastName, postalCode: '24103', city: 'Lübeck' });
});

test('Bearbeiten mit leerem Nachnamen zeigt eine Fehlermeldung und speichert nichts', async ({ page }) => {
  // Der Server lehnt mit 400 ab, deshalb ändert der Test keine Daten und kann einen Demo-Kunden nehmen
  await page.locator('.grid div[role=gridcell]').filter({ hasText: /^K-\d+$/ }).first().click();
  await expect(page).toHaveURL(/\/customers\/\d+$/);
  const heading = page.getByTestId('customer-name');
  const originalName = (await heading.textContent())!;

  await page.getByRole('button', { name: 'Bearbeiten' }).click();
  const dialog = page.locator('.dialog-body').filter({ visible: true });
  await dialog.locator('input[name=lastName]').fill('');
  await dialog.getByRole('button', { name: 'Speichern' }).click();

  // Die Meldung vom Server steht am Feld, der Dialog bleibt offen, die Überschrift ist unverändert
  await expect(dialog.getByText('Nachname ist erforderlich.')).toBeVisible();
  await expect(dialog).toBeVisible();
  await expect(heading).toHaveText(originalName);
});
