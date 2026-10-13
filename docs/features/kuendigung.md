# Feature: Vertrag kündigen

Status: **in Arbeit** – Datenbank (Patch 1.1.0) und ein erster API-Endpunkt sind vorhanden,
die Fachregeln und die Oberfläche fehlen noch.

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

Kündigungsfristen und Mindestlaufzeiten stehen in der Tabelle `ContractType`
(siehe auch Handbuch, Kapitel „Tarife“).

## Oberfläche

- Auf der Vertragsseite gibt es den Button **Kündigen** (nur bei aktiven Verträgen aktiv).
- Er öffnet einen Dialog mit dem Feld „Kündigungsgrund“ und zeigt das voraussichtliche Vertragsende an.
- Nach dem Speichern wird die Seite neu geladen; Status, Kündigungsdatum, Grund und Vertragsende sind sichtbar.
- Alle Texte auf Deutsch und Englisch.

## Stand der Umsetzung

- [x] Spalten `CancelledAt` und `CancellationReason` (Patch `1.1.0/0001_contract_cancellation.sql`)
- [x] Endpunkt `POST /api/contracts/{id}/cancel` – setzt bisher nur Status, Datum und Grund
- [ ] Regel 2 (Grund ist Pflicht) prüfen
- [ ] Regeln 4 und 5: Vertragsende berechnen und speichern
- [ ] Unit-Tests für die Berechnung des Vertragsendes
- [ ] Dialog in der Oberfläche
- [ ] Übersetzungen (de/en)
- [ ] Playwright-Test für den Ablauf
