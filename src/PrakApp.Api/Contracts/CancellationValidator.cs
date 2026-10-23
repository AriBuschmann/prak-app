using PrakApp.Api.Common;

namespace PrakApp.Api.Contracts;

/// <summary>Prüft die Eingaben beim Kündigen (Regel 2 in docs/features/kuendigung.md).</summary>
public sealed class CancellationValidator
{
    // Höchstlänge des Kündigungsgrunds, passt zur Datenbankspalte CancellationReason NVARCHAR(200)
    public const int ReasonMaxLength = 200;

    // Bekommt die Anfrage aus dem Request-Body ({ "reason": "..." }) und gibt alle gefundenen Fehler zurück.
    // Ist die Liste leer (errors.IsValid), darf gekündigt werden.
    public ValidationErrors Validate(CancelContractRequest input)
    {
        // Sammelt die Fehlermeldungen pro Feld; daraus macht der Controller die Antwort 400
        var errors = new ValidationErrors();

        // Der Grund wird ohne Leerzeichen am Anfang und Ende gespeichert, deshalb zählt die gekürzte Länge
        // Fehlt der Grund ganz (null), ist er leer ("") oder besteht er nur aus Leerzeichen → Pflichtfeld-Fehler
        if (string.IsNullOrWhiteSpace(input.Reason))
            // nameof(input.Reason) ergibt "Reason". Unter diesem Feldnamen zeigt das Frontend die Meldung an
            errors.Add(nameof(input.Reason), "Kündigungsgrund ist erforderlich.");
        // Nur wenn ein Grund da ist: ohne Leerzeichen am Rand länger als 200 Zeichen → Längen-Fehler
        else if (input.Reason.Trim().Length > ReasonMaxLength)
            errors.Add(nameof(input.Reason), $"Kündigungsgrund darf höchstens {ReasonMaxLength} Zeichen lang sein.");

        return errors;
    }
}
