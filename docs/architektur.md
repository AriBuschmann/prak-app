# Architektur

## Überblick

```
Browser (Aurelia-App)
   │  fetch /api/...  (JSON, Cookie für die Anmeldung)
   ▼
ASP.NET Core (PrakApp.Api)
   Controller  →  Service / Validator  →  Repository (Dapper, SQL)
   ▼
SQL Server LocalDB (Datenbank "PrakApp")
```

Das Backend liefert auch das gebaute Frontend aus (`wwwroot`). Alle Adressen, die nicht mit `/api`
beginnen und keine Datei sind, werden auf `index.html` umgeleitet, damit Links wie
`/customers/12` direkt im Browser funktionieren.

## Backend

| Schicht     | Aufgabe                                                        | Beispiel                     |
|-------------|----------------------------------------------------------------|------------------------------|
| Controller  | HTTP entgegennehmen, Statuscodes zurückgeben                   | `CustomersController`        |
| Validator   | Eingaben prüfen, Fehlermeldungen pro Feld sammeln              | `CustomerValidator`          |
| Service     | Fachlogik, die mehrere Repositories braucht                    | `ContractService`            |
| Repository  | SQL ausführen, Ergebnisse auf C#-Typen abbilden (Dapper)       | `CustomerRepository`         |

**Dependency Injection:** Alle Klassen werden in `Program.cs` beim eingebauten DI-Container
registriert. Controller bekommen ihre Abhängigkeiten über den Konstruktor.

- Repositories: `Scoped` (eine Instanz pro HTTP-Anfrage)
- Validatoren, `BalanceCalculator`: `Singleton` (haben keinen Zustand)
- `TimeProvider`: `Singleton` – in Tests kann so ein festes „Heute“ verwendet werden

**Anmeldung:** Cookie-Authentifizierung ohne Passwort (`AuthController`). Die Sitzung läuft nach
30 Minuten ohne Aktivität ab. Alle Controller außer `AuthController` verlangen eine Anmeldung.

**Fehler:** Validierungsfehler → HTTP 400 mit `ValidationProblemDetails`. Nicht gefunden → 404.
Unerwartete Fehler → HTTP 500 mit `ProblemDetails`.

## Frontend

- `src/main.ts` – Start der App, Registrierung von Router, Übersetzungen und globalen Komponenten
- `src/my-app.*` – Rahmen der App (Kopfzeile, Navigation) und Routen
- `src/auth-hook.ts` – leitet nicht angemeldete Benutzer zur Anmeldeseite
- `src/pages/` – Seiten (Kundenliste, Kundendetails, Vertragsdetails, Anmeldung)
- `src/components/` – `jqx-grid` und `modal-dialog`, dünne Hüllen um jQWidgets
- `src/services/` – HTTP-Zugriff, Anmeldung, Formatierung, Datentypen
- `src/locales/` – Übersetzungen (de, en)

jQWidgets ist ein jQuery-Plugin. `src/jqx/jquery-global.ts` stellt jQuery deshalb global bereit,
bevor die jqx-Module geladen werden.

## Datenbank

| Tabelle        | Inhalt                                                    |
|----------------|-----------------------------------------------------------|
| `Customer`     | Kunden mit Kontaktdaten und Anschrift                     |
| `ContractType` | Tarife mit Mindestlaufzeit und Kündigungsfrist            |
| `Contract`     | Verträge eines Kunden (Beginn, Ende, Monatsbeitrag)       |
| `Payment`      | Zahlungseingänge zu einem Vertrag                         |
| `PatchLog`     | Welche Patch-Skripte eingespielt wurden (vom DbTool)      |

## Kundensuche

Die Suche in der Kundenliste findet Kunden über Nachname, Vorname, Ort und Kundennummer.
Bei Namen und Ort wird nach dem Anfang gesucht („Schu“ findet „Schulz“, „Würz“ findet „Würzburg“), bei der
Kundennummer überall.

Zusätzlich sucht dasselbe Suchfeld nach Vertragsnummern (`GET /api/contracts?q=`, Nummer enthält den Text). Treffer
stehen über der Kundentabelle unter „Passende Verträge“ und führen direkt zur Vertragsseite, z. B. um eine Zahlung
mit Vertragsnummer im Verwendungszweck zu erfassen.
