# Feature: Vertrag kündigen

Status: **fertig** (22.10.2026) – Backend mit Fachregeln, Dialog in der Oberfläche, Übersetzungen und Tests.
Die Endpunkte sind in [docs/api.md](../api.md#kündigung) beschrieben.

## Ziel

Sachbearbeiter:innen können einen aktiven Vertrag kündigen, wenn ein Kunde anruft oder schreibt.
Die Anwendung berechnet automatisch, wann der Vertrag endet.

## Fachliche Regeln

1. Nur Verträge mit Status **Aktiv** können gekündigt werden.
2. Ein Kündigungsgrund ist Pflicht (höchstens 200 Zeichen).
3. Als Kündigungsdatum (`CancelledAt`) gilt der Tag, an dem die Kündigung erfasst wird.
4. **Vertragsende** (`EndDate`) = Ende des Monats, in dem die Kündigungsfrist des Tarifs abläuft.
   Beispiel: Kündigung am 13.10.2026, Kündigungsfrist 3 Monate → Fristablauf 13.01.2027 →
   Vertragsende 31.01.2027.
5. **Mindestlaufzeit:** Endet die Mindestlaufzeit des Tarifs (gerechnet ab Vertragsbeginn) später als
   das nach Regel 4 berechnete Datum, endet der Vertrag erst am letzten Tag der Mindestlaufzeit.
   Beispiel: Beginn 01.03.2026, Mindestlaufzeit 12 Monate → frühestes Ende 28.02.2027.
6. Nach der Kündigung hat der Vertrag den Status **Gekündigt**. Beiträge sind bis zum Vertragsende fällig.
7. Die Kündigung kann nicht rückgängig gemacht werden (dafür gibt es später ein eigenes Feature).
8. **Befristete Verträge:** Hat der Vertrag schon ein Vertragsende, das vor dem nach Regel 4 und 5
   berechneten Datum liegt, bleibt es bestehen. Liegt es später, gilt das berechnete Datum.
9. **Kündigung vor Vertragsbeginn** ist erlaubt; die Mindestlaufzeit gilt trotzdem. Der Vertrag endet
   aber nie vor seinem Beginn: Ergibt die Berechnung ein früheres Datum (Tarif ohne Mindestlaufzeit),
   endet er am Tag des Vertragsbeginns.

### Entscheidungen und Annahmen (Stand 22.10.2026)

| Punkt | Festlegung | Quelle |
|-------|------------|--------|
| Regel 8, vorhandenes Ende früher | vorhandenes Ende bleibt | Betreuer |
| Regel 8, vorhandenes Ende später | berechnetes Ende gilt | Annahme |
| Regel 9, Kündigung vor Beginn erlaubt, Mindestlaufzeit gilt | wie beschrieben | Betreuer |
| Regel 9, Ende nie vor Beginn | Ende = Tag des Vertragsbeginns | Annahme (Datenbankregel `EndDate >= StartDate`) |
| Regel 5, Beginn am 29., 30. oder 31. | fehlt der Tag im Zielmonat, endet die Mindestlaufzeit am Monatsende (Beginn 29.02.2028 + 12 Monate → 28.02.2029) | Annahme (wie § 188 BGB) |
| Regel 2, Leerzeichen | Leerzeichen am Anfang und Ende werden entfernt und zählen nicht zu den 200 Zeichen | Annahme |
| Reihenfolge der Prüfungen | erst Kündigungsgrund (400), dann Vertrag vorhanden (404) und aktiv (409) | Annahme |
| Voraussichtliches Vertragsende im Dialog | berechnet der Server über `GET /api/contracts/{id}/cancellation-preview`, damit die Regeln nur an einer Stelle stehen | Annahme |

Kündigungsfristen und Mindestlaufzeiten stehen in der Tabelle `ContractType`
(siehe auch Handbuch, Kapitel „Tarife“).

## Oberfläche

- Auf der Vertragsseite gibt es den Button **Kündigen** (nur bei aktiven Verträgen aktiv).
- Er öffnet einen Dialog mit dem Feld „Kündigungsgrund“ und zeigt das voraussichtliche Vertragsende an.
- Nach dem Speichern wird die Seite neu geladen; Status, Kündigungsdatum, Grund und Vertragsende sind sichtbar.
- Alle Texte auf Deutsch und Englisch.

## Stand der Umsetzung

- [x] Spalten `CancelledAt` und `CancellationReason` (Patch `1.1.0/0001_contract_cancellation.sql`)
- [x] Endpunkt `POST /api/contracts/{id}/cancel` – setzt Status, Kündigungsdatum, Grund und Vertragsende
- [x] Regel 2 (Grund ist Pflicht) prüfen – `CancellationValidator`, Antwort 400 mit `ValidationProblemDetails`
- [x] Regeln 4, 5, 8 und 9: Vertragsende berechnen (`CancellationCalculator`) und speichern (`ContractService.CancelAsync`)
- [x] Unit-Tests für die Berechnung des Vertragsendes
- [x] Dialog in der Oberfläche (mit Vorschau-Endpunkt `GET /api/contracts/{id}/cancellation-preview`)
- [x] Übersetzungen (de/en) – Fehlermeldungen des Servers sind wie im ganzen Projekt nur auf Deutsch
- [x] Playwright-Test für den Ablauf (`e2e/cancellation.spec.ts`, legt sich bei jedem Lauf einen eigenen Vertrag an)

## Wo steht was im Code?

| Teil | Datei |
|------|-------|
| Berechnung des Vertragsendes (Regeln 4, 5, 8, 9) | `src/PrakApp.Api/Contracts/CancellationCalculator.cs` |
| Prüfung des Kündigungsgrunds (Regel 2) | `src/PrakApp.Api/Contracts/CancellationValidator.cs` |
| Ablauf: prüfen, berechnen, speichern; Vorschau | `src/PrakApp.Api/Contracts/ContractService.cs` (`CancelAsync`, `PreviewCancellationAsync`) |
| Endpunkte | `src/PrakApp.Api/Contracts/ContractsController.cs` |
| Speichern in der Datenbank | `src/PrakApp.Api/Contracts/ContractRepository.cs` (`CancelAsync`) |
| Dialog | `src/PrakApp.Web/src/pages/contracts/contract-detail.*` |
| Unit- und Integrationstests | `tests/PrakApp.Api.Tests/Contracts/Cancel*Tests.cs`, `ContractServiceCancelTests.cs` |
| Ende-zu-Ende-Test | `src/PrakApp.Web/e2e/cancellation.spec.ts` |
