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
| GET     | `/api/customers?search=<text>`  | Kunden suchen (ohne Parameter: alle)                 |
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

## Verträge

| Methode | Pfad                          | Beschreibung                                   |
|---------|-------------------------------|------------------------------------------------|
| GET     | `/api/contract-types`         | Tarife                                         |
| GET     | `/api/contracts/{id}`         | Vertrag mit Beitragskonto (`balance`)          |
| POST    | `/api/contracts`              | Vertrag anlegen                                |
| POST    | `/api/contracts/{id}/cancel`  | Vertrag kündigen, Body: `{ "reason": "..." }`  |

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

Regeln: Der Betrag muss größer als 0 sein. Das Zahlungsdatum darf nicht in der Zukunft liegen.

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
