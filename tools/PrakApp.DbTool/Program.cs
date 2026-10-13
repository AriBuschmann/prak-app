using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

// PrakApp.DbTool – spielt die Patch-Skripte aus Database/Patch ein.
//
//   dotnet run --project tools/PrakApp.DbTool -- rebuild   (Datenbank löschen und neu aufbauen)
//   dotnet run --project tools/PrakApp.DbTool -- update    (nur noch fehlende Patches einspielen)
//
// Optionen: --server "(localdb)\MSSQLLocalDB"  --database PrakApp  --patches <Pfad zu Database/Patch>

var command = args.FirstOrDefault(a => !a.StartsWith("--"))?.ToLowerInvariant();
if (command is not ("rebuild" or "update"))
{
    Console.WriteLine("Verwendung: PrakApp.DbTool rebuild|update [--server <server>] [--database <name>] [--patches <pfad>]");
    return 1;
}

var server = Option("--server") ?? @"(localdb)\MSSQLLocalDB";
var database = Option("--database") ?? "PrakApp";
var patchRoot = Option("--patches") ?? FindPatchRoot();

if (patchRoot is null || !Directory.Exists(patchRoot))
{
    Console.Error.WriteLine("Patch-Verzeichnis nicht gefunden. Bitte mit --patches angeben.");
    return 1;
}

var masterCs = new SqlConnectionStringBuilder
{
    DataSource = server,
    InitialCatalog = "master",
    IntegratedSecurity = true,
    TrustServerCertificate = true,
}.ConnectionString;
var dbCs = new SqlConnectionStringBuilder(masterCs) { InitialCatalog = database }.ConnectionString;

try
{
    if (command == "rebuild")
    {
        Console.WriteLine($"Datenbank [{database}] auf {server} wird neu aufgebaut ...");
        await using var master = new SqlConnection(masterCs);
        await master.OpenAsync();
        await Exec(master, $"""
            IF DB_ID(N'{database}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{database}];
            END
            CREATE DATABASE [{database}];
            """);
    }

    await using var db = new SqlConnection(dbCs);
    await db.OpenAsync();
    await Exec(db, """
        IF OBJECT_ID(N'dbo.PatchLog') IS NULL
            CREATE TABLE dbo.PatchLog
            (
                Version   NVARCHAR(20)  NOT NULL,
                FileName  NVARCHAR(200) NOT NULL,
                AppliedAt DATETIME2(0)  NOT NULL CONSTRAINT DF_PatchLog_AppliedAt DEFAULT (SYSUTCDATETIME()),
                CONSTRAINT PK_PatchLog PRIMARY KEY (Version, FileName)
            );
        """);

    var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    await using (var cmd = new SqlCommand("SELECT Version + '/' + FileName FROM dbo.PatchLog", db))
    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync()) applied.Add(reader.GetString(0));
    }

    var count = 0;
    foreach (var (version, file) in DiscoverPatches(patchRoot))
    {
        var fileName = Path.GetFileName(file);
        if (applied.Contains($"{version}/{fileName}")) continue;

        Console.WriteLine($"  {version}/{fileName}");
        var script = await File.ReadAllTextAsync(file);
        foreach (var batch in SplitBatches(script))
        {
            await Exec(db, batch);
        }
        await using var log = new SqlCommand("INSERT INTO dbo.PatchLog (Version, FileName) VALUES (@v, @f)", db);
        log.Parameters.AddWithValue("@v", version);
        log.Parameters.AddWithValue("@f", fileName);
        await log.ExecuteNonQueryAsync();
        count++;
    }

    Console.WriteLine(count == 0 ? "Keine neuen Patches." : $"{count} Patch(es) eingespielt.");
    return 0;
}
catch (SqlException ex)
{
    Console.Error.WriteLine($"SQL-Fehler: {ex.Message}");
    return 2;
}

string? Option(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string? FindPatchRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, "Database", "Patch");
        if (Directory.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    return null;
}

// Reihenfolge: Version aufsteigend (1.0.0 < 1.1.0 < 1.10.0), danach Dateiname.
static IEnumerable<(string Version, string File)> DiscoverPatches(string root) =>
    Directory.GetDirectories(root)
        .Select(d => (Name: Path.GetFileName(d), Parsed: Version.TryParse(Path.GetFileName(d), out var v) ? v : null))
        .Where(d => d.Parsed is not null)
        .OrderBy(d => d.Parsed)
        .SelectMany(d =>
        {
            var core = Path.Combine(root, d.Name, "Approved", "core");
            return Directory.Exists(core)
                ? Directory.GetFiles(core, "*.sql").OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal).Select(f => (d.Name, f))
                : [];
        });

static IEnumerable<string> SplitBatches(string script) =>
    Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
        .Where(b => !string.IsNullOrWhiteSpace(b));

static async Task Exec(SqlConnection connection, string sql)
{
    await using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 120 };
    await cmd.ExecuteNonQueryAsync();
}
