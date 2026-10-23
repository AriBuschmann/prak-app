using PrakApp.Api.Contracts;

namespace PrakApp.Api.Tests.Contracts;

// Regel 2 aus docs/features/kuendigung.md: Ein Kündigungsgrund ist Pflicht (höchstens 200 Zeichen)
public class CancellationValidatorTests
{
    // Der Validator hat keinen Zustand, deshalb reicht ein Objekt für alle Tests dieser Klasse
    private readonly CancellationValidator _validator = new();

    // Normalfall: Ein ganz normaler Grund ist gültig (IsValid = keine Fehler)
    [Fact]
    public void Reason_is_accepted()
    {
        Assert.True(_validator.Validate(new CancelContractRequest("Umzug ins Ausland")).IsValid);
    }

    // Drei Arten, wie ein Grund "fehlen" kann: gar nicht mitgeschickt (null), leer, nur Leerzeichen
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reason_is_required(string? reason)
    {
        var errors = _validator.Validate(new CancelContractRequest(reason));

        // Es muss ein Fehler beim Feld "Reason" stehen, dort zeigt das Frontend später die Meldung an
        Assert.Contains("Reason", errors.ToDictionary().Keys);
    }

    // Grenzwerte: Genau 200 Zeichen sind noch erlaubt, 201 nicht. An solchen Grenzen passieren oft
    // Fehler (z. B. ">" statt ">="), deshalb wird beides geprüft.
    // new string('x', 200) erzeugt einen Text aus 200-mal dem Zeichen x
    [Fact]
    public void Reason_may_have_200_characters()
    {
        Assert.True(_validator.Validate(new CancelContractRequest(new string('x', 200))).IsValid);
    }

    [Fact]
    public void Reason_must_not_have_more_than_200_characters()
    {
        var errors = _validator.Validate(new CancelContractRequest(new string('x', 201)));

        Assert.Contains("Reason", errors.ToDictionary().Keys);
    }

    // Leerzeichen am Anfang und Ende werden beim Speichern entfernt und zählen deshalb nicht mit
    [Fact]
    public void Surrounding_spaces_do_not_count()
    {
        Assert.True(_validator.Validate(new CancelContractRequest("  " + new string('x', 200) + "  ")).IsValid);
    }
}
