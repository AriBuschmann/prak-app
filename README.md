# prak-app – Vertragsverwaltung (Übungsprojekt)

Eine kleine, aber realistische Webanwendung zum Üben: Kunden, Verträge und Zahlungen eines
fiktiven Tarifanbieters verwalten. Alle Daten und Namen sind erfunden.

| Teil       | Technik                                                                     |
|------------|-----------------------------------------------------------------------------|
| Backend    | .NET 9, ASP.NET Core (REST-API, Controller), Dapper, eingebaute Dependency Injection |
| Frontend   | Aurelia 2, TypeScript, Vite, jQWidgets (Grid, Fenster, Datumsfeld), i18next (de/en) |
| Datenbank  | SQL Server LocalDB, Schema als nummerierte Patch-Skripte                    |
| Tests      | xUnit (Backend), Playwright (Ende-zu-Ende)                                  |

## Voraussetzungen

Prüfe in einem Terminal:

```powershell
dotnet --list-sdks     # 9.x muss dabei sein
node --version         # 22 oder neuer
pnpm --version
sqllocaldb info        # sollte MSSQLLocalDB anzeigen
git --version
```

## Einrichtung (einmalig)

```powershell
# 1. Datenbank aufbauen (löscht eine vorhandene Datenbank "PrakApp" und spielt alle Patches ein)
.\Database\rebuild-database.ps1

# 2. Frontend-Abhängigkeiten installieren
cd src\PrakApp.Web
pnpm install
cd ..\..
```

## Entwickeln in VS Code

1. In VS Code **Datei → Ordner öffnen…** → `C:\dev\prak-app` (den ganzen Ordner, nicht einzelne Dateien).
2. Die vorgeschlagenen Erweiterungen installieren (Hinweis unten rechts). Wichtig ist **C#**.
   *C# Dev Kit* wird nicht benötigt.
3. Links **Ausführen und Debuggen** (Strg+Umschalt+D) → oben **„App starten (Backend + Frontend)“**
   auswählen → **F5**.

F5 baut das Backend, startet es mit Debugger, startet den Vite-Entwicklungsserver und öffnet Edge
auf http://localhost:9000. Haltepunkte funktionieren in C#-Dateien und in den TypeScript-Dateien
des Frontends. Beenden mit Umschalt+F5. Der Vite-Server läuft im Terminal weiter und wird beim
nächsten F5 wiederverwendet; zum Beenden das Terminal „frontend: dev“ schließen.

Weitere Aufgaben (Tests, Frontend bauen, Datenbank neu aufbauen) gibt es unter
**Terminal → Aufgabe ausführen…**.

## Starten über die Kommandozeile

**Variante A – alles über das Backend** (so wie später auf einem Server):

```powershell
cd src\PrakApp.Web
pnpm build                                  # baut das Frontend nach src/PrakApp.Api/wwwroot
cd ..\..
dotnet run --project src\PrakApp.Api        # http://localhost:5080
```

**Variante B – Entwicklung mit automatischem Neuladen** (zwei Terminals):

```powershell
# Terminal 1: Backend
dotnet watch --project src\PrakApp.Api

# Terminal 2: Frontend-Entwicklungsserver
cd src\PrakApp.Web
pnpm dev                                    # http://localhost:9000, leitet /api an Port 5080 weiter
```

Anmelden: Auf der Startseite einen Benutzer auswählen. Es gibt kein Passwort – die Anmeldung ist nur
eine Demo und **keine** echte Sicherheit.

## Tests

```powershell
# Backend-Tests (xUnit)
dotnet test

# Ende-zu-Ende-Tests (Playwright) – Datenbank muss aufgebaut und das Frontend gebaut sein
cd src\PrakApp.Web
pnpm exec playwright install chromium       # nur beim ersten Mal
pnpm build
pnpm test:e2e
```

Playwright startet das Backend selbst, falls es nicht bereits läuft.

## Datenbank

- Patch-Skripte liegen unter `Database/Patch/<Version>/Approved/core/` und werden nach Version und
  Dateiname sortiert eingespielt. Bereits eingespielte Patches merkt sich die Tabelle `dbo.PatchLog`.
- Neue Änderung am Schema = neues Skript mit der nächsten Nummer. Bereits eingespielte Skripte
  werden nicht mehr verändert.
- Nur fehlende Patches einspielen, ohne die Datenbank zu löschen:
  `dotnet run --project tools\PrakApp.DbTool -- update`
- Die Demo-Daten (`0006_seed_demo_data.sql`) sind generiert und frei erfunden.

## Ordnerstruktur

```
Database/               Patch-Skripte und Skript zum Neuaufbau der Datenbank
docs/                   Dokumentation, fachliche Beschreibungen, Firmenhandbuch (fiktiv)
sample-logs/            Beispiel-Logdateien (für Übungen zur Fehleranalyse)
src/PrakApp.Api/        Backend: Controller, Repositories, Validierung, Services
src/PrakApp.Web/        Frontend: Aurelia-App, Komponenten, Übersetzungen, E2E-Tests
tests/PrakApp.Api.Tests xUnit-Tests für das Backend
tools/PrakApp.DbTool    Kommandozeilen-Tool, das die Patch-Skripte einspielt
```

Mehr zum Aufbau: [docs/architektur.md](docs/architektur.md) und [docs/api.md](docs/api.md).

## Demo-Benutzer

| Benutzer      | Name           | Rolle           |
|---------------|----------------|-----------------|
| `m.berger`    | Marta Berger   | Sachbearbeitung |
| `s.okafor`    | Samuel Okafor  | Teamleitung     |
| `t.lindqvist` | Tove Lindqvist | Sachbearbeitung |
| `praktikum`   | Praktikum      | Sachbearbeitung |

Die Benutzer stehen in `src/PrakApp.Api/appsettings.json`.

## Hinweis zu jQWidgets

jQWidgets ist für nicht-kommerzielle Zwecke (z. B. Lernen) frei nutzbar. Für den kommerziellen
Einsatz ist eine Lizenz nötig, siehe https://www.jqwidgets.com/license/.
