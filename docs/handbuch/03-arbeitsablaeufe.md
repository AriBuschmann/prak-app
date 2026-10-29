# Handbuch Kundenservice – 3. Arbeitsabläufe

## 3.1 Neuen Kunden anlegen

1. Erst suchen, ob der Kunde schon existiert (Name **und** Postleitzahl vergleichen). Beim Speichern warnt die
   Vertragsverwaltung zusätzlich, wenn es schon Kunden mit gleichem Namen und gleicher PLZ gibt. Ist es wirklich
   eine andere Person, mit „Trotzdem anlegen“ bestätigen.
2. „Neuer Kunde“ → Vorname, Nachname (Pflicht), Anschrift, E-Mail, Telefon erfassen.
3. Die Kundennummer (`K-…`) vergibt das System automatisch. Sie wird dem Kunden mitgeteilt.
4. Doppelnamen und Namenszusätze vollständig übernehmen (z. B. „von Albers-Lindqvist“).

## 3.2 Vertrag abschließen

1. Kunden öffnen → „Neuer Vertrag“.
2. Tarif wählen, Vertragsbeginn und Monatsbeitrag eintragen. Ein Vertragsende nur bei befristeten
   Verträgen eintragen.
3. Die Vertragsnummer (`V-<Jahr>-<Nummer>`) vergibt das System.

## 3.3 Zahlungen erfassen

Zahlungen kommen fast immer per Überweisung. Die Buchhaltung gibt jeden Morgen die Kontoauszüge
des Vortags weiter.

1. Vertrag über die Vertragsnummer im Verwendungszweck suchen.
2. „Zahlung erfassen“ → Betrag, **Zahlungsdatum = Datum der Gutschrift laut Kontoauszug**,
   Verwendungszweck übernehmen.
3. Teilzahlungen werden genauso erfasst. Der Saldo zeigt dann den restlichen Rückstand.
4. Kann eine Zahlung keinem Vertrag zugeordnet werden: zurück an die Buchhaltung.

Das Zahlungsdatum ist wichtig für Mahnungen und für Rückfragen von Kunden. Es muss exakt mit dem
Kontoauszug übereinstimmen.

Für Rückfragen (z. B. von der Buchhaltung) lassen sich die Zahlungen eines Vertrags auf der Vertragsseite mit
„Als CSV exportieren“ als Datei speichern und in Excel öffnen.

## 3.4 Kündigungen

1. Kündigungen kommen schriftlich, per E-Mail oder telefonisch (dann Rückruf zur Bestätigung).
2. Vertrag öffnen → „Kündigen“ → Kündigungsgrund eintragen.
3. Das System berechnet das Vertragsende. Dieses Datum in der Bestätigung an den Kunden nennen.
4. Bis zum Vertragsende sind die Beiträge weiter fällig.
5. Zieht der Kunde die Kündigung zurück, solange der Vertrag noch läuft: Vertrag öffnen → „Kündigung zurücknehmen“.
   Der Vertrag ist dann wieder aktiv mit dem ursprünglichen Vertragsende.

## 3.5 Rückstände und Mahnungen

- Liegt der Saldo eines Vertrags bei **mehr als zwei Monatsbeiträgen**, bekommt der Kunde eine
  Zahlungserinnerung. Die Vertragsverwaltung markiert solche Verträge mit „Zahlungserinnerung fällig“
  (im Beitragskonto auf der Vertragsseite und in der Vertragsliste beim Kunden).
- Nach weiteren 14 Tagen ohne Zahlung: 1. Mahnung (durch die Buchhaltung).
- Das Mahnwesen ist noch nicht in der Vertragsverwaltung abgebildet; die Buchhaltung führt dazu
  eine eigene Liste.

## 3.6 Datenschutz

- Auskunftsersuchen und Löschwünsche immer an das Team Datenschutz weiterleiten.
- Keine Bildschirmfotos mit Kundendaten verschicken.
- Testdaten und echte Daten nie vermischen.
