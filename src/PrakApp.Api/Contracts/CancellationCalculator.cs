namespace PrakApp.Api.Contracts;

/// <summary>
/// Berechnet, wann ein gekündigter Vertrag endet (siehe docs/features/kuendigung.md, Regeln 4, 5, 8 und 9).
/// </summary>
public static class CancellationCalculator
{
    // Geändert (Schritt 2): neuer Parameter existingEndDate = das schon vorhandene Vertragsende eines befristeten
    // Vertrags, null bei unbefristeten Verträgen. "= null" macht ihn optional, alle bisherigen Aufrufe funktionieren weiter.
    public static DateTime EndDate(
        DateTime startDate, DateTime cancelledAt, int noticePeriodMonths, int minTermMonths, DateTime? existingEndDate = null)
    {
        // Regel 4: Kündigungsdatum + Kündigungsfrist ergibt den Fristablauf, der Vertrag endet am Ende dieses Monats.
        // .Date schneidet eine eventuelle Uhrzeit ab. Beispiel: 13.10.2026 + 3 Monate = 13.01.2027 → 31.01.2027
        var endByNotice = EndOfMonth(cancelledAt.Date.AddMonths(noticePeriodMonths));
        // Regel 5: letzter Tag der Mindestlaufzeit, gerechnet ab Vertragsbeginn (bei 0 Monaten liegt er vor dem Beginn)
        var endOfMinTerm = LastDayOfMinTerm(startDate.Date, minTermMonths);
        // Der Vertrag endet frühestens am Ende der Mindestlaufzeit: Es gilt das spätere der beiden Daten
        // Geändert (Schritt 2): Das Ergebnis wird nicht mehr direkt zurückgegeben, sondern in "end" gemerkt,
        // weil danach noch die zwei neuen Regeln 9 und 8 angewendet werden
        var end = endOfMinTerm > endByNotice ? endOfMinTerm : endByNotice;

        // Neu (Schritt 2, Regel 9):
        // Kündigung vor Vertragsbeginn ohne Mindestlaufzeit: Der Vertrag kann nicht vor seinem Beginn enden
        // (Datenbankregel EndDate >= StartDate), also endet er am Tag des Beginns
        if (end < startDate.Date)
            end = startDate.Date;

        // Neu (Schritt 2, Regel 8):
        // Befristeter Vertrag: Ein vorhandenes Vertragsende, das früher liegt, hat Vorrang
        if (existingEndDate is { } existing && existing.Date < end)
            end = existing.Date;

        return end;
    }

    /// <summary>Regel 4: Der Vertrag endet am letzten Tag des Monats, in dem die Kündigungsfrist abläuft.</summary>
    private static DateTime EndOfMonth(DateTime date) =>
        new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

    /// <summary>
    /// Regel 5: Letzter Tag der Mindestlaufzeit = Tag vor dem gleichen Datum n Monate später
    /// (Beginn 01.03.2026 + 12 Monate → 28.02.2027). Gibt es dieses Datum im Zielmonat nicht
    /// (Beginn 29.02.2028 + 12 Monate), endet die Mindestlaufzeit am letzten Tag dieses Monats.
    /// </summary>
    private static DateTime LastDayOfMinTerm(DateTime startDate, int minTermMonths)
    {
        // Dasselbe Datum n Monate nach Vertragsbeginn, z. B. 01.03.2026 + 12 Monate → 01.03.2027.
        // AddMonths setzt einen fehlenden Tag auf das Monatsende (29.02.2028 + 12 Monate → 28.02.2029)
        var sameDayLater = startDate.AddMonths(minTermMonths);
        // Gleicher Tag wie beim Beginn: Die Mindestlaufzeit endet am Tag davor (01.03.2027 → 28.02.2027).
        // Anderer Tag: AddMonths musste kürzen, der Tag fehlt im Zielmonat, also endet sie am Monatsende (28.02.2029)
        return sameDayLater.Day == startDate.Day ? sameDayLater.AddDays(-1) : sameDayLater;
    }
}
