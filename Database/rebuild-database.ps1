# Löscht die lokale Datenbank "PrakApp" und baut sie aus den Patch-Skripten neu auf.
# Aufruf aus dem Repository-Wurzelverzeichnis:  .\Database\rebuild-database.ps1
param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'PrakApp'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

sqllocaldb start MSSQLLocalDB | Out-Null

dotnet run --project (Join-Path $root 'tools\PrakApp.DbTool') -- rebuild `
    --server $Server --database $Database --patches (Join-Path $PSScriptRoot 'Patch')
if ($LASTEXITCODE -ne 0) { throw "Datenbank-Neuaufbau fehlgeschlagen (Exit-Code $LASTEXITCODE)." }
