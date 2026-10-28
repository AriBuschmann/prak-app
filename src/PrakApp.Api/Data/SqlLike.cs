namespace PrakApp.Api.Data;

/// <summary>Hilfe für Suchen mit SQL LIKE (Kundensuche, Vertragssuche).</summary>
public static class SqlLike
{
    // %, _ und [ sind bei LIKE Platzhalter (z. B. findet "%" sonst alle Kunden). In eckigen Klammern gelten sie als
    // normales Zeichen. "[" zuerst ersetzen, sonst würden die Klammern aus den beiden anderen Ersetzungen mit ersetzt
    public static string? Escape(string? value) =>
        value?.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
