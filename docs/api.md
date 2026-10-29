# REST-API

Basisadresse: `http://localhost:5080/api`. Alle Daten als JSON (camelCase). Datumswerte als ISO-8601.
Außer den Endpunkten unter `/api/auth` brauchen alle Aufrufe eine Anmeldung (Cookie).

## Anmeldung

| Methode | Pfad               | Beschreibung                                    |
|---------|--------------------|-------------------------------------------------|
| GET     | `/api/auth/users`  | Liste der Demo-Benutzer                         |
| POST    | `/api/auth/login`  | Anmelden, Body: `{ "userName": "m.berger" }`    |
| POST    | `/api/auth/logout` | Abmelden                                        |
| GET     | `/api/auth/me`     | Angemeldeter Benutzer, sonst 401                |

## Kunden

| Methode | Pfad                            | Beschreibung                                         |
|---------|---------------------------------|------------------------------------------------------|
| GET     | `/api/customers?q=<text>`       | Kunden suchen (ohne Parameter: alle)                 |
| GET     | `/api/customers/duplicates?firstName=&lastName=&postalCode=` | Mögliche doppelte Kunden: gleicher Name und gleiche PLZ (ohne PLZ: nur Name) |
| GET     | `/api/customers/{id}`           | Ein Kunde                                            |
| POST    | `/api/customers`                | Kunde anlegen                                        |
| PUT     | `/api/customers/{id}`           | Kunde ändern                                         |
| DELETE  | `/api/customers/{id}`           | Kunde löschen (nur ohne Verträge)                    |
| GET     | `/api/customers/{id}/contracts` | Verträge eines Kunden                                |

Beispiel für `POST /api/customers`:

```json
{
  "firstName": "Anna",
  "lastName": "Albers",
  "email": "anna.albers@example.org",
  "phone": "0431 123456",
  "street": "Lindenweg 3",
  "postalCode": "24103",
  "city": "Kiel"
}
```

Regeln: Vor- und Nachname sind Pflicht, die PLZ hat 5 Ziffern, die E-Mail-Adresse muss gültig sein.
Höchstlängen (Leerzeichen am Anfang und Ende zählen nicht): Vorname 50, Nachname 100, E-Mail 200,
Telefon 30, Straße 100, Ort 80 Zeichen.

## Verträge

| Methode | Pfad                          | Beschreibung                                   |
|---------|-------------------------------|------------------------------------------------|
| GET     | `/api/contract-types`         | Tarife                                         |
| GET     | `/api/contracts?q=<text>`     | Verträge suchen, deren Nummer den Text enthält (max. 10; ohne Text: keine) |
| GET     | `/api/contracts/{id}`         | Vertrag mit Beitragskonto (`balance`)          |
| POST    | `/api/contracts`              | Vertrag anlegen                                |
| GET     | `/api/contracts/{id}/cancellation-preview` | Voraussichtliches Vertragsende bei Kündigung heute (speichert nichts) |
| POST    | `/api/contracts/{id}/cancel`  | Vertrag kündigen, Body: `{ "reason": "..." }`  |
| POST    | `/api/contracts/{id}/undo-cancellation` | Kündigung zurücknehmen (ohne Body)   |

Regeln für `POST /api/contracts`: Kunde, Vertragsart und Vertragsbeginn sind Pflicht, der Monatsbeitrag ist größer
als 0 und höchstens 9999,99. Vertragsbeginn und Vertragsende liegen zwischen 01.01.2000 und 31.12.2099, das
Vertragsende nicht vor dem Vertragsbeginn.

Beitragskonto (`balance`) und Vertragsliste beim Kunden enthalten `reminderDue`: `true`, wenn der Saldo mehr als zwei
Monatsbeiträge beträgt (Zahlungserinnerung fällig, siehe Handbuch 3.5).

Status eines Vertrags (`status`): `Active`, `Cancelled` (gekündigt) oder `Ended` (beendet). Ab dem Tag nach dem
Vertragsende liefert die API immer `Ended`, auch für aktive und gekündigte Verträge. Der Status wird dafür beim Lesen
berechnet, in der Datenbank-Spalte `Status` bleibt der gespeicherte Wert stehen.

### Kündigung

Fachliche Regeln: [docs/features/kuendigung.md](features/kuendigung.md).

**Vorschau** – `GET /api/contracts/{id}/cancellation-preview` berechnet, was eine Kündigung heute ergeben
würde, und wird vom Kündigungsdialog angezeigt. Es wird nichts gespeichert.

```json
{
  "cancelledAt": "2026-10-22T00:00:00",
  "endDate": "2027-02-28T00:00:00"
}
```

**Kündigen** – `POST /api/contracts/{id}/cancel` mit Body `{ "reason": "Umzug ins Ausland" }`.
Setzt Status `Cancelled`, Kündigungsdatum (heute), Grund und das berechnete Vertragsende.

Regeln: Der Kündigungsgrund ist Pflicht und hat höchstens 200 Zeichen (Leerzeichen am Anfang und Ende
werden entfernt). Nur aktive Verträge können gekündigt werden.

| Status | Bedeutung |
|--------|-----------|
| 204    | Gekündigt (ohne Inhalt) |
| 400    | Kündigungsgrund fehlt oder ist zu lang (`errors.Reason`) – nur beim Kündigen |
| 404    | Vertrag nicht gefunden |
| 409    | Vertrag ist nicht aktiv (schon gekündigt oder beendet) |

Die Vorschau antwortet mit 200 und den Daten oben, sonst ebenfalls mit 404 oder 409.

**Kündigung zurücknehmen** – `POST /api/contracts/{id}/undo-cancellation` (ohne Body). Setzt den Status wieder auf
`Active`, löscht Kündigungsdatum und Grund und stellt das Vertragsende von vor der Kündigung wieder her (`null` bei
unbefristeten Verträgen). Ob das geht, zeigt `GET /api/contracts/{id}` in `cancellationUndoable`; das Vertragsende
danach steht in `endDateBeforeCancellation`.

| Status | Bedeutung |
|--------|-----------|
| 204    | Zurückgenommen (ohne Inhalt) |
| 404    | Vertrag nicht gefunden |
| 409    | Vertrag ist nicht gekündigt, schon beendet, oder die Kündigung wurde vor Einführung der Rücknahme erfasst (Grund steht in `title`) |

## Zahlungen

| Methode | Pfad                              | Beschreibung                       |
|---------|-----------------------------------|------------------------------------|
| GET     | `/api/payments?contractId={id}`   | Zahlungen zu einem Vertrag         |
| POST    | `/api/payments`                   | Zahlung erfassen                   |

Beispiel für das Erfassen einer Zahlung:

```json
{
  "contractId": 12,
  "amount": 24.90,
  "paymentDate": "2026-10-06",
  "reference": "Beitrag 10/2026"
}
```

Regeln: Der Betrag muss größer als 0 sein. Das Zahlungsdatum ist Pflicht, darf nicht in der Zukunft und nicht vor
dem 01.01.2000 liegen.

## Fehlerantworten

Validierungsfehler (HTTP 400):

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "LastName": ["Nachname ist erforderlich."]
  }
}
```
