# Handbuch Kundenservice – 4. IT und Support

## Fehler in der Vertragsverwaltung melden

Bitte jede Störung als Ticket an die IT melden (Durchwahl 400 oder Ticket-Formular im Intranet).
Ein gutes Ticket enthält:

1. **Was** wolltest du tun? (z. B. „Kunde K-100023 bearbeiten“)
2. **Was** ist passiert? Genaue Fehlermeldung abschreiben.
3. **Wann** ist es passiert? (Datum und Uhrzeit – damit die IT die Logdatei durchsuchen kann)
4. Passiert es **immer** oder nur manchmal? Bei allen Kunden oder nur bei einem?
5. Welcher Benutzer war angemeldet?

Keine Passwörter oder vollständigen Kundendaten ins Ticket schreiben – die Kundennummer reicht.

## Logdateien

Die Anwendung schreibt pro Tag eine Logdatei. Beispiele liegen im Ordner `sample-logs/`.
Jede Zeile hat den Aufbau:

```
<Zeitstempel UTC> [<Level>] <Kategorie>: <Nachricht>
```

Level: `DBG` (Detail), `INF` (Information), `WRN` (Warnung), `ERR` (Fehler), `FTL` (Absturz).
Der Zeitstempel ist in **UTC** – für deutsche Zeit im Sommer 2 Stunden, im Winter 1 Stunde addieren.

## Bekannte Wartungsfenster

- Jeden Dienstag 6:00–6:30 Uhr: Datenbanksicherung, die Anwendung kann kurz langsam sein.
- Updates der Vertragsverwaltung werden vorher per Rundmail angekündigt.

## Zugänge

Neue Mitarbeitende bekommen ihren Zugang über die Teamleitung. Zugänge werden nie geteilt.
