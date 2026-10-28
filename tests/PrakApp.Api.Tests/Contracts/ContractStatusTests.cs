using PrakApp.Api.Contracts;

namespace PrakApp.Api.Tests.Contracts;

// Verbesserung: Der Status "Beendet" (Ended) wurde nie gesetzt. Jetzt gilt ein Vertrag ab dem Tag nach dem
// Vertragsende als beendet, egal ob er gespeichert "Active" oder "Cancelled" ist. Die Regel steckt in
// ContractStatus.Effective und wird beim Lesen im ContractRepository angewendet.
public class ContractStatusTests
{
    // Festes "heute" für alle Fälle. Effective bekommt das Datum als Parameter, darum hängt der Test nicht vom echten Datum ab
    private static readonly DateTime Today = new(2026, 10, 28);

    // Jede Zeile: gespeicherter Status, Vertragsende (als Text, null = kein Ende), erwarteter Status.
    // ContractStatus.Active usw. sind Konstanten (const), darum dürfen sie in [InlineData] stehen
    [Theory]
    // Ende war gestern → beendet (vor der Änderung blieb der Vertrag "Active")
    [InlineData(ContractStatus.Active, "2026-10-27", ContractStatus.Ended)]
    // Ende ist heute → noch nicht beendet: Das Vertragsende ist der letzte Tag, an dem der Vertrag noch läuft
    [InlineData(ContractStatus.Active, "2026-10-28", ContractStatus.Active)]
    // Ende ist morgen → weiter aktiv
    [InlineData(ContractStatus.Active, "2026-10-29", ContractStatus.Active)]
    // Kein Vertragsende (unbefristet) → bleibt aktiv
    [InlineData(ContractStatus.Active, null, ContractStatus.Active)]
    // Gekündigter Vertrag, Ende war gestern → beendet (vorher blieb er für immer "Cancelled")
    [InlineData(ContractStatus.Cancelled, "2026-10-27", ContractStatus.Ended)]
    // Gekündigter Vertrag, Ende liegt noch in der Zukunft → bleibt "Gekündigt", Beiträge sind bis dahin fällig
    [InlineData(ContractStatus.Cancelled, "2027-01-31", ContractStatus.Cancelled)]
    // Schon als "Ended" gespeichert (wie in den Demo-Daten) → bleibt beendet
    [InlineData(ContractStatus.Ended, "2024-06-21", ContractStatus.Ended)]
    public void Contract_is_ended_after_its_end_date(string storedStatus, string? endDate, string expected)
    {
        // Text in ein Datum umwandeln; bei null bleibt es null. (DateTime?)null ist nötig, damit beide Seiten
        // des ? : denselben Typ "DateTime?" haben. InvariantCulture: Format JJJJ-MM-TT unabhängig von der Rechner-Sprache
        var end = endDate is null ? (DateTime?)null : DateTime.Parse(endDate, System.Globalization.CultureInfo.InvariantCulture);

        // Die Regel liefert den Status, mit dem die App arbeitet (Anzeige und Kündigungsprüfung)
        Assert.Equal(expected, ContractStatus.Effective(storedStatus, end, Today));
    }
}
