using System.Globalization;
using System.Text;

namespace PrakApp.Api.Payments;

// Zahlungen eines Vertrags als CSV, so wie ein deutsches Excel die Datei per Doppelklick richtig öffnet:
// Semikolon als Trennzeichen, Komma bei Beträgen, Datum TT.MM.JJJJ, Zeilenende CRLF
public static class PaymentCsv
{
    private const string Header = "Vertragsnummer;Zahlungsdatum;Betrag;Verwendungszweck;Erfasst von";
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public static string Build(string contractNumber, IEnumerable<Payment> payments)
    {
        var csv = new StringBuilder();
        csv.Append(Header).Append("\r\n");
        foreach (var payment in payments)
        {
            csv.AppendJoin(';',
                Field(contractNumber),
                payment.PaymentDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                payment.Amount.ToString("0.00", German),
                Field(payment.Reference),
                Field(payment.RecordedBy));
            csv.Append("\r\n");
        }
        return csv.ToString();
    }

    private static string Field(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        // CSV-Injection: Text mit = + - @ am Anfang führt Excel als Formel aus. Mit ' davor bleibt er normaler Text
        if ("=+-@\t\r".Contains(value[0]))
        {
            value = "'" + value;
        }
        // Trennzeichen, Anführungszeichen oder Zeilenumbruch im Text: in Anführungszeichen setzen, " darin verdoppeln
        if (value.IndexOfAny([';', '"', '\r', '\n']) >= 0)
        {
            value = "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
