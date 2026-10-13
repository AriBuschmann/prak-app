namespace PrakApp.Api.Common;

/// <summary>
/// Sammelt Validierungsfehler pro Feld. Das Format passt zu ValidationProblemDetails,
/// damit das Frontend die Meldungen direkt am Eingabefeld anzeigen kann.
/// </summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    public bool IsValid => _errors.Count == 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = [];
            _errors[field] = list;
        }
        list.Add(message);
    }

    public IDictionary<string, string[]> ToDictionary() =>
        _errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
}
