# Umsetzung: Vertrag kündigen

Wie das Feature aus [kuendigung.md](kuendigung.md) Schritt für Schritt umgesetzt wurde: welche Dateien neu sind,
was sie enthalten und was an bereits vorhandenen Dateien geändert wurde.

- **Vergleichsbasis:** Stand nach Commit `460b9cc` („Kundenliste: Verträge statt Zahlungen zählen“), also vor dem Feature
- **Stand dieser Übersicht:** 23.10.2026
- **Fachregeln, Entscheidungen und Annahmen:** stehen in [kuendigung.md](kuendigung.md) und werden hier nicht wiederholt

## Auf einen Blick

| | Vorher | Nachher |
|---|---|---|
| Neue Dateien | – | 7 (2 Backend, 4 Backend-Tests, 1 Playwright-Test) |
| Geänderte Dateien | – | 13 (5 Backend, 6 Frontend, 2 Doku) |
| Backend-Tests (`dotnet test`) | 23 | 58 (+35) |
| Playwright-Tests | 7 | 8 (+1) |
| Datenbank | Spalten `CancelledAt`, `CancellationReason` (Patch 1.1.0) | unverändert, **kein neues Patch-Skript** (die Spalte `EndDate` gab es schon) |

**Ausgangslage vor dem Feature:** `POST /api/contracts/{id}/cancel` setzte nur Status, Kündigungsdatum und Grund.
Der Grund wurde nicht geprüft, das Vertragsende nicht berechnet (TODO in `ContractService`). Der Button
„Kündigen“ zeigte nur den Hinweis „Die Kündigung ist noch nicht fertig umgesetzt.“ (TODO in `contract-detail.ts`).

## Ablauf einer Kündigung (fertiger Stand)

```
Vertragsseite (contract-detail)
   │  Klick auf „Kündigen“
   ▼
GET /api/contracts/{id}/cancellation-preview ──► ContractService.PreviewCancellationAsync
   │                                                 └─ Preview() → CancellationCalculator.EndDate(...)
   ▼
Dialog: Feld „Kündigungsgrund“, voraussichtliches Vertragsende
   │  Klick auf „Kündigen“ im Dialog
   ▼
POST /api/contracts/{id}/cancel ──► ContractService.CancelAsync
                                      1. CancellationValidator.Validate      → 400 bei fehlendem/zu langem Grund
                                      2. ContractRepository.GetAsync          → 404 / 409 (nicht gefunden / nicht aktiv)
                                      3. Preview() → CancellationCalculator   → Kündigungsdatum + Vertragsende
                                      4. ContractRepository.CancelAsync       → UPDATE dbo.Contract
   ▼
204 → Dialog schließen, Seite neu laden: Status „Gekündigt“, Ende, „Gekündigt am“, Grund
```

---

## Die Schritte

Jeder Schritt wurde nach demselben Muster umgesetzt: erst ein Test, der rot ist, dann der Code, dann grün.
Im Code sind die Stellen mit Kommentaren wie `Neu (Schritt 2)` oder `Geändert (Schritt 3)` markiert.

| Schritt | Inhalt | Neue Dateien | Geänderte Dateien | Geprüft durch |
|---|---|---|---|---|
| 0 | Plan, offene Fragen an den Betreuer | – | – | – |
| 1 | Vertragsende berechnen (Regeln 4, 5) | `CancellationCalculator.cs`, `CancellationCalculatorTests.cs` | – | 14 Unit-Tests (rot → grün), dazu 1 Uhrzeit-Test per Mutationstest |
| 2 | Kündigungsgrund prüfen (Regel 2), Regeln 8 und 9 | `CancellationValidator.cs`, `CancellationValidatorTests.cs`, `CancelEndpointTests.cs` | `ContractService.cs`, `ContractsController.cs`, `Program.cs`, `CancellationCalculator.cs`, `kuendigung.md` | 7 Validator-Tests, 1 Integrationstest, 5 neue Calculator-Tests |
| 3 | Vertragsende speichern | `ContractServiceCancelTests.cs` | `ContractRepository.cs`, `ContractService.cs`, `kuendigung.md` | 5 Service-Tests; echte API (Hilfsport 5081) und `sqlcmd` |
| 4 | Dialog in der Oberfläche, Vorschau-Endpunkt | – | `ContractModels.cs`, `ContractService.cs`, `ContractsController.cs`, `models.ts`, `contract-detail.ts`, `contract-detail.html`, `de.json`, `en.json`, `styles.css`, `ContractServiceCancelTests.cs`, `kuendigung.md` | 2 Service-Tests; Durchklicken im Browser mit Bildschirmfotos |
| 5 | Übersetzungen (de/en) | – | `de.json`, `en.json`, `contract-detail.html`, `kuendigung.md` | Skript: alle Schlüssel in beiden Sprachen; englische Seiten im Browser |
| 6 | Playwright-Test für den ganzen Ablauf | `cancellation.spec.ts` | `kuendigung.md` | Mutationstest (rot), zwei Läufe hintereinander grün |
| 7 | Doku | – | `api.md`, `kuendigung.md` | Abschlussprüfung gegen alle Regeln |

### Schritt 0 – Plan und offene Fragen

Abgleich von `kuendigung.md` mit dem Code, Plan in 7 Schritten. Offene Fragen an den Betreuer:
befristete Verträge mit vorhandenem Ende, Kündigung vor Vertragsbeginn. Die Antworten wurden später als
Regeln 8 und 9 in `kuendigung.md` aufgenommen. Keine Codeänderungen.

### Schritt 1 – Vertragsende berechnen

Neue, reine Rechenklasse `CancellationCalculator` ohne Datenbank und ohne Oberfläche, dazu Unit-Tests mit den
Beispielen aus der Fachbeschreibung und Sonderfällen (Monatsende, Jahreswechsel, Schaltjahr, Beginn am 31.).
Ein zusätzlicher Test stellt sicher, dass Uhrzeiten das Ergebnis nicht verändern. Dass er wirkt, wurde geprüft,
indem `.Date` vorübergehend entfernt wurde (Test rot) und danach wieder eingesetzt.

### Schritt 2 – Kündigungsgrund prüfen, Regeln 8 und 9

Neuer `CancellationValidator` nach dem Muster der vorhandenen Validatoren. Der Service prüft den Grund **vor**
der Datenbankabfrage, der Controller antwortet bei Fehlern mit 400 und `ValidationProblemDetails`.
Der Calculator bekam den optionalen Parameter `existingEndDate` (Regel 8) und die Regel „Ende nie vor Beginn“ (Regel 9).

### Schritt 3 – Vertragsende speichern

`ContractService.CancelAsync` berechnet das Vertragsende mit dem Calculator, `ContractRepository.CancelAsync`
schreibt es in die Spalte `EndDate`. Die TODO-Notiz im Service ist entfernt. Getestet mit einem selbstgebauten
Fake-Repository (ohne Datenbank) und einmal gegen die echte API und Datenbank.

### Schritt 4 – Dialog und Vorschau

Damit der Dialog das voraussichtliche Ende zeigen kann, ohne die Regeln im Frontend zu wiederholen, gibt es den
neuen Endpunkt `GET /api/contracts/{id}/cancellation-preview`. Kündigung und Vorschau benutzen im Service dieselbe
Methode `Preview()`. Beim Prüfen im Browser fiel ein **älterer Fehler** auf: Dialoge schnitten bei Fehlermeldungen
die Buttons ab (auch der Zahlungsdialog). Behoben in `styles.css`.

### Schritt 5 – Übersetzungen

Alle Übersetzungsschlüssel wurden per Skript verglichen: Danach haben `de.json` und `en.json` dieselben 62 Schlüssel,
und jeder im Code benutzte Schlüssel existiert in beiden.

### Schritt 6 – Playwright-Test

Weil ein Vertrag nur einmal gekündigt werden kann, legt sich der Test bei jedem Lauf über die API einen eigenen
Kunden und Vertrag an. Das erwartete Vertragsende rechnet er aus dem heutigen Datum aus, er funktioniert also an
jedem Tag. Geprüft, indem `EndDate = @endDate` vorübergehend aus dem SQL entfernt wurde (Test rot).

### Schritt 7 – Doku

`docs/api.md` beschreibt die beiden Endpunkte, `kuendigung.md` hat Status „fertig“ und eine Übersicht
„Wo steht was im Code?“.

---

## Neue Dateien

### `src/PrakApp.Api/Contracts/CancellationCalculator.cs` (55 Zeilen, Schritte 1 und 2)

Statische Klasse, berechnet das Vertragsende. Braucht keinen `TimeProvider` (das Kündigungsdatum wird übergeben)
und wird deshalb nicht in `Program.cs` registriert.

```csharp
public static DateTime EndDate(DateTime startDate, DateTime cancelledAt,
    int noticePeriodMonths, int minTermMonths, DateTime? existingEndDate = null)
```

Rechenweg (Uhrzeiten werden mit `.Date` abgeschnitten):

| # | Regel | Berechnung | Beispiel |
|---|---|---|---|
| 1 | 4 | Kündigungsdatum + Kündigungsfrist, davon das **Monatsende** (`EndOfMonth`) | 13.10.2026 + 3 Monate → 31.01.2027 |
| 2 | 5 | letzter Tag der Mindestlaufzeit ab Vertragsbeginn (`LastDayOfMinTerm`): Tag vor demselben Datum n Monate später; fehlt der Tag im Zielmonat, das Monatsende | 01.03.2026 + 12 → 28.02.2027; 29.02.2028 + 12 → 28.02.2029 |
| 3 | 5 | das **spätere** Datum aus 1 und 2 | |
| 4 | 9 | liegt das Ergebnis vor dem Vertragsbeginn, gilt der Vertragsbeginn | BASIS ab 01.12.2026, gekündigt 22.10.2026 → 01.12.2026 |
| 5 | 8 | liegt ein vorhandenes Vertragsende (`existingEndDate`) **früher**, bleibt es | befristet bis 31.12.2026 → 31.12.2026 |

Private Hilfsmethoden: `EndOfMonth(DateTime)`, `LastDayOfMinTerm(DateTime, int)`.

### `src/PrakApp.Api/Contracts/CancellationValidator.cs` (29 Zeilen, Schritt 2)

Prüft `CancelContractRequest` und gibt `ValidationErrors` zurück (Muster wie `ContractValidator`).
Registriert als Singleton in `Program.cs`.

| Fall | Feld | Meldung |
|---|---|---|
| Grund `null`, leer oder nur Leerzeichen | `Reason` | „Kündigungsgrund ist erforderlich.“ |
| Grund ohne Leerzeichen am Rand länger als `ReasonMaxLength` (200) | `Reason` | „Kündigungsgrund darf höchstens 200 Zeichen lang sein.“ |

Die 200 passt zur Datenbankspalte `CancellationReason NVARCHAR(200)`.

### `tests/PrakApp.Api.Tests/Contracts/CancellationCalculatorTests.cs` (141 Zeilen, Schritte 1 und 2) – 20 Testfälle

| Test | Fälle | Prüft |
|---|---|---|
| `Contract_ends_at_the_end_of_the_month_in_which_the_notice_period_expires` | 1 | Beispiel Regel 4 → 31.01.2027 |
| `Contract_does_not_end_before_the_minimum_term` | 1 | Beispiel Regel 5 → 28.02.2027 (Regel 4 allein: 31.07.2026) |
| `Expired_minimum_term_does_not_change_the_end_date` | 1 | abgelaufene Mindestlaufzeit spielt keine Rolle |
| `Contract_without_minimum_term_can_end_after_the_notice_period` | 1 | BASIS: Beginn und Kündigung am selben Tag → 30.11.2026 |
| `Notice_period_edge_cases` | 5 | 31.10. + 1 Monat, Jahreswechsel, Februar im Schaltjahr und im normalen Jahr, Monatserster |
| `Minimum_term_edge_cases` | 4 | Beginn am 1., am 31., am Schalttag, 24 Monate (PREMIUM) |
| `Time_of_day_is_ignored` | 1 | Uhrzeit, wenn Regel 4 entscheidet |
| `Time_of_day_is_ignored_when_the_minimum_term_decides` | 1 | Uhrzeit, wenn Regel 5 entscheidet |
| `Existing_end_date_wins_if_it_is_earlier` | 3 | Regel 8: vorhandenes Ende früher / gleich / später |
| `Contract_cancelled_before_it_starts_still_has_the_minimum_term` | 1 | Regel 9: KOMFORT ab 01.12.2026 → 30.11.2027 |
| `Contract_never_ends_before_it_starts` | 1 | Regel 9: BASIS ab 01.12.2026 → 01.12.2026 |

### `tests/PrakApp.Api.Tests/Contracts/CancellationValidatorTests.cs` (54 Zeilen, Schritt 2) – 7 Testfälle

Gültiger Grund; `null`, `""` und `"   "` sind Pflichtfeld-Fehler; 200 Zeichen erlaubt, 201 nicht;
Leerzeichen am Rand zählen nicht mit.

### `tests/PrakApp.Api.Tests/Contracts/CancelEndpointTests.cs` (30 Zeilen, Schritt 2) – 1 Testfall

Integrationstest mit `WebApplicationFactory<Program>`: anmelden, `POST /api/contracts/1/cancel` mit
`{ "reason": "   " }` → 400 mit Fehler beim Feld `Reason`. Braucht keine Datenbank, weil der Grund vor der
Datenbankabfrage geprüft wird.

### `tests/PrakApp.Api.Tests/Contracts/ContractServiceCancelTests.cs` (153 Zeilen, Schritte 3 und 4) – 7 Testfälle

Testet `ContractService` ohne Datenbank. Hilfsteile in der Datei:

- `FakeContractRepository`: implementiert `IContractRepository`, kennt genau einen Vertrag und merkt sich in
  `Saved`, was `CancelAsync` speichern wollte
- `FixedTimeProvider`: „heute“ ist fest der 13.04.2026
- `Komfort(...)`: KOMFORT-Vertrag ab 01.03.2026 mit wählbarem Status und Vertragsende

| Test | Prüft |
|---|---|
| `Cancelling_saves_status_date_reason_and_calculated_end_date` | Kündigungsdatum = heute, Ende 28.02.2027, Grund ohne Leerzeichen am Rand |
| `Existing_earlier_end_date_is_kept` | Regel 8 im Service |
| `Contract_that_is_not_active_is_not_cancelled` | `NotActive`, nichts gespeichert |
| `Unknown_contract_is_not_found` | `NotFound`, nichts gespeichert |
| `Invalid_reason_saves_nothing` | `Invalid`, Fehler bei `Reason`, nichts gespeichert |
| `Preview_shows_the_expected_end_date_without_saving` | Vorschau liefert Datum und Ende, speichert nichts (Schritt 4) |
| `Preview_is_not_possible_for_a_contract_that_is_not_active` | keine Vorschau bei beendetem Vertrag (Schritt 4) |

### `src/PrakApp.Web/e2e/cancellation.spec.ts` (68 Zeilen, Schritt 6) – 1 Testfall

Playwright-Test „Vertrag kündigen: Dialog, Pflichtfeld, Vertragsende und Anzeige danach“:

1. anmelden als `praktikum`
2. über die API einen neuen Kunden und einen PREMIUM-Vertrag ab dem 1. des aktuellen Monats anlegen
3. Vertragsseite öffnen → Status „Aktiv“
4. Dialog öffnen → voraussichtliches Ende = letzter Tag des Monats 23 Monate später (aus dem heutigen Datum berechnet)
5. ohne Grund kündigen → „Kündigungsgrund ist erforderlich.“
6. mit Grund kündigen → „Gekündigt“, Ende, heutiges Datum und Grund sichtbar, Button „Kündigen“ deaktiviert
7. Seite neu laden → Kündigung ist gespeichert

---

## Geänderte bestehende Dateien

### Backend

#### `src/PrakApp.Api/Contracts/ContractModels.cs` (Schritt 4)

- **Neu:** `record CancellationPreview(DateTime CancelledAt, DateTime EndDate)` – Antwort des Vorschau-Endpunkts

#### `src/PrakApp.Api/Contracts/ContractRepository.cs` (Schritt 3)

| | Vorher | Nachher |
|---|---|---|
| `IContractRepository.CancelAsync` | `(int id, DateTime cancelledAt, string? reason)` | `(int id, DateTime cancelledAt, DateTime endDate, string reason)` |
| SQL in `CancelAsync` | `SET Status, CancelledAt, CancellationReason` | zusätzlich `EndDate = @endDate` |

#### `src/PrakApp.Api/Contracts/ContractService.cs` (Schritte 2, 3 und 4)

- **`enum CancelResult`:** neuer Wert `Invalid` (Kündigungsgrund fehlt oder ist zu lang)
- **Konstruktor:** neuer Parameter `CancellationValidator cancellationValidator`
- **`CancelAsync`**
  - Signatur vorher `Task<CancelResult> CancelAsync(int id, string? reason)`,
    nachher `Task<(CancelResult Result, ValidationErrors Errors)> CancelAsync(int id, CancelContractRequest request)`
    (gleiches Muster wie `CreateAsync`)
  - Ablauf: Grund prüfen → Vertrag laden (404/409) → `Preview()` → speichern mit Vertragsende und `Reason.Trim()`
  - die TODO-Notiz zum Vertragsende ist entfernt
- **Neu:** `PreviewCancellationAsync(int id)` → `(CancelResult, CancellationPreview?)`; `Cancelled` bedeutet hier
  „kann gekündigt werden“, es wird nichts gespeichert
- **Neu:** private Methode `Preview(ContractRecord)` – heute (`TimeProvider`) und Vertragsende per
  `CancellationCalculator.EndDate(StartDate, heute, NoticePeriodMonths, MinTermMonths, EndDate)`;
  wird von Kündigung **und** Vorschau benutzt

#### `src/PrakApp.Api/Contracts/ContractsController.cs` (Schritte 2 und 4)

- **`Cancel`:** gibt die ganze Anfrage an den Service; neuer Fall `CancelResult.Invalid` → 400 `ValidationProblem(...)`
  (404, 409 und 204 wie vorher)
- **Neu:** `GET /api/contracts/{id}/cancellation-preview` (`GetCancellationPreview`) → 200 mit `CancellationPreview`,
  sonst 404 oder 409

#### `src/PrakApp.Api/Program.cs` (Schritt 2)

- **Neu:** `builder.Services.AddSingleton<CancellationValidator>();`

### Frontend

#### `src/PrakApp.Web/src/services/models.ts` (Schritt 4)

- **Neu:** `interface CancellationPreview { cancelledAt: string; endDate: string; }`

#### `src/PrakApp.Web/src/pages/contracts/contract-detail.ts` (Schritt 4)

- Import um `CancellationPreview` erweitert
- **Entfernt:** Eigenschaft `notice` und der Hinweis „noch nicht fertig“ in `cancelContract()`
- **Neu:** Dialog-Zustand `cancelOpen`, `cancelReason`, `cancelErrors`, `cancelPreview`
- **`cancelContract()`** (jetzt `async`): lädt die Vorschau, setzt Grund und Fehler zurück, öffnet den Dialog
- **Neu: `saveCancellation()`**: `POST …/cancel` mit `{ reason }`; bei Erfolg Dialog schließen und Vertrag neu laden,
  bei 400 Meldungen am Feld anzeigen, andere Fehler weiterwerfen (roter Hinweis oben)

#### `src/PrakApp.Web/src/pages/contracts/contract-detail.html` (Schritte 4 und 5)

- **Entfernt:** `<p class="notice">…</p>`
- **Neu:** zweiter `modal-dialog` (Titel `contract.cancelTitle`) mit
  - `<textarea name="reason">` und Fehlerliste `cancelErrors.reason`
  - „Voraussichtliches Vertragsende“ (`data-testid="expected-end-date"`)
  - Hinweis „Die Kündigung kann nicht rückgängig gemacht werden.“
  - Buttons „Abbrechen“ (`common.cancel`) und Bestätigen (`contract.cancelConfirm`, Schritt 5)
- Unverändert: Der Button „Kündigen“ auf der Seite war schon vorhanden und nur bei aktiven Verträgen aktiv.

#### `src/PrakApp.Web/src/locales/de.json` und `en.json` (Schritte 4 und 5)

| Schlüssel | Deutsch | Englisch | Änderung |
|---|---|---|---|
| `contract.cancelNotAvailable` | – | – | in `de.json` entfernt (wird nicht mehr gebraucht) |
| `contract.cancelTitle` | Vertrag kündigen | Cancel contract | neu in beiden |
| `contract.cancelConfirm` | Kündigen | Confirm cancellation | neu in beiden (Schritt 5) |
| `contract.cancelHint` | Die Kündigung kann nicht rückgängig gemacht werden. | The cancellation cannot be undone. | neu in beiden |
| `contract.expectedEndDate` | Voraussichtliches Vertragsende | Expected end date | neu in beiden |
| `contract.cancelledAt` | Gekündigt am | Cancelled on | fehlte vorher in `en.json` |
| `contract.cancellationReason` | Kündigungsgrund | Reason for cancellation | fehlte vorher in `en.json` |

#### `src/PrakApp.Web/src/styles.css` (Schritt 4)

- `textarea` bekommt dieselbe Gestaltung wie `input` und `select`, dazu `resize: vertical`
- **Fehlerbehebung:** `.jqx-window, .jqx-window-content { height: auto !important; }` – jqxWindow legte eine feste
  Höhe fest, sodass Fehlermeldungen die Buttons abschnitten (betraf auch den vorhandenen Zahlungsdialog)

### Dokumentation

#### `docs/features/kuendigung.md` (Schritte 2 bis 7)

- Status „in Arbeit“ → „fertig“, mit Link zur API-Doku
- neue Regeln 8 (befristete Verträge) und 9 (Kündigung vor Vertragsbeginn)
- neue Tabelle „Entscheidungen und Annahmen“ (was vom Betreuer kommt, was angenommen wurde)
- Checkliste vollständig abgehakt, neuer Abschnitt „Wo steht was im Code?“

#### `docs/api.md` (Schritt 7)

- Tabelle „Verträge“ um `GET /api/contracts/{id}/cancellation-preview` ergänzt
- neuer Abschnitt „Kündigung“: Beispielantwort der Vorschau, Body zum Kündigen, Regeln, Statuscodes 204/400/404/409
