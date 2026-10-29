using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;

namespace PrakApp.Api.Tests.DbTool;

// Fehlender Test: das DbTool (tools/PrakApp.DbTool), das die Datenbank-Patches einspielt.
// Getestet wird von außen, so wie rebuild-database.ps1 es benutzt: Der Test startet "dotnet run --project tools/PrakApp.DbTool"
// und schaut danach in die Datenbank. Am DbTool selbst muss dafür nichts geändert werden.
// Sicherheit: "rebuild" LÖSCHT die angegebene Datenbank. Der Test benutzt deshalb eine eigene Wegwerf-Datenbank
// (PrakApp_DbToolTest_<zufällig>) und eigene kleine Test-Patches in einem Temp-Ordner. Die Datenbank PrakApp wird nie
// angefasst. Am Ende (Dispose) werden Test-Datenbank und Temp-Ordner wieder gelöscht.
public sealed class DbToolTests : IDisposable
{
    private const string Server = @"(localdb)\MSSQLLocalDB";
    private readonly string _database = $"PrakApp_DbToolTest_{Guid.NewGuid():N}";
    private readonly string _patchRoot = Path.Combine(Path.GetTempPath(), $"PrakApp.DbToolTest_{Guid.NewGuid():N}", "Patch");

    private string ConnectionString => new SqlConnectionStringBuilder
    {
        DataSource = Server, InitialCatalog = _database, IntegratedSecurity = true, TrustServerCertificate = true,
    }.ConnectionString;

    [Fact]
    public async Task Patches_are_applied_once_in_version_and_file_order_and_logged()
    {
        // Jeder Test-Patch trägt seinen Namen in die Tabelle TestOrder ein. Die Reihenfolge der Zeilen zeigt dann,
        // in welcher Reihenfolge die Patches gelaufen sind. Absichtlich ungeordnet angelegt.
        // Versionen 1.2.0, 1.9.0, 1.10.0: Als Text sortiert käme "1.10.0" vor "1.2.0", als Version kommt 1.10.0 zuletzt
        WritePatch("1.10.0", "0001_a.sql");
        WritePatch("1.9.0", "0002_b.sql");
        WritePatch("1.2.0", "0001_a.sql");
        WritePatch("1.9.0", "0001_a.sql");

        // 1. Lauf: Datenbank neu anlegen und alle Patches einspielen
        var first = await RunDbToolAsync("rebuild");
        Assert.Contains("4 Patch(es) eingespielt.", first);
        string[] expectedOrder = ["1.2.0/0001_a.sql", "1.9.0/0001_a.sql", "1.9.0/0002_b.sql", "1.10.0/0001_a.sql"];
        Assert.Equal(expectedOrder, await QueryAsync("SELECT Name FROM dbo.TestOrder ORDER BY Id"));
        // Jeder Patch steht im PatchLog
        Assert.Equal(expectedOrder.Order(), await QueryAsync("SELECT Version + '/' + FileName FROM dbo.PatchLog ORDER BY Version + '/' + FileName"));

        // 2. Lauf ohne neue Patches: Es wird nichts noch einmal eingespielt
        var second = await RunDbToolAsync("update");
        Assert.Contains("Keine neuen Patches.", second);
        Assert.Equal(expectedOrder, await QueryAsync("SELECT Name FROM dbo.TestOrder ORDER BY Id"));
        Assert.Equal(4, (await QueryAsync("SELECT Version FROM dbo.PatchLog")).Length);

        // 3. Lauf mit einem neuen Patch: Nur dieser eine wird eingespielt und im PatchLog vermerkt
        WritePatch("1.10.0", "0002_b.sql");
        var third = await RunDbToolAsync("update");
        Assert.Contains("1 Patch(es) eingespielt.", third);
        string[] expectedAfterNewPatch = [.. expectedOrder, "1.10.0/0002_b.sql"];
        Assert.Equal(expectedAfterNewPatch, await QueryAsync("SELECT Name FROM dbo.TestOrder ORDER BY Id"));
        Assert.Contains("1.10.0/0002_b.sql", await QueryAsync("SELECT Version + '/' + FileName FROM dbo.PatchLog"));
    }

    // Legt <Version>/Approved/core/<Datei> an. Der Patch legt TestOrder an, falls es sie noch nicht gibt, und trägt
    // dann seinen Namen ein. Das GO dazwischen prüft nebenbei, dass das DbTool Skripte an GO in Teile zerlegt
    private void WritePatch(string version, string fileName)
    {
        var folder = Path.Combine(_patchRoot, version, "Approved", "core");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, fileName), $"""
            IF OBJECT_ID(N'dbo.TestOrder') IS NULL
                CREATE TABLE dbo.TestOrder (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(100) NOT NULL);
            GO
            INSERT INTO dbo.TestOrder (Name) VALUES (N'{version}/{fileName}');
            GO
            """);
    }

    // Startet das DbTool wie rebuild-database.ps1 und gibt seine Ausgabe zurück. Exit-Code ungleich 0 = Test rot
    private async Task<string> RunDbToolAsync(string command)
    {
        Assert.StartsWith("PrakApp_DbToolTest_", _database);   // Sicherheitsnetz: niemals eine andere Datenbank
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList =
            {
                "run", "--project", Path.Combine(RepositoryRoot(), "tools", "PrakApp.DbTool"), "--",
                command, "--server", Server, "--database", _database, "--patches", _patchRoot,
            },
        };
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(3));
        var all = await output + await error;
        Assert.True(process.ExitCode == 0, $"DbTool {command} endete mit Exit-Code {process.ExitCode}:\n{all}");
        return all;
    }

    private async Task<string[]> QueryAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        return [.. await connection.QueryAsync<string>(sql)];
    }

    // Repository-Wurzel = der Ordner mit PrakApp.sln, gesucht vom Ausgabeordner des Tests aufwärts
    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PrakApp.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("PrakApp.sln nicht gefunden");
    }

    // Aufräumen nach dem Test, auch wenn er rot war: Test-Datenbank und Temp-Ordner löschen
    public void Dispose()
    {
        SqlConnection.ClearAllPools();
        var master = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" }.ConnectionString;
        using (var connection = new SqlConnection(master))
        {
            connection.Execute($"""
                IF DB_ID(N'{_database}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{_database}];
                END
                """);
        }
        var tempFolder = Directory.GetParent(_patchRoot)!.FullName;
        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, recursive: true);
    }
}
