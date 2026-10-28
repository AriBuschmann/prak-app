-- Zähler (SEQUENCE) für Kunden- und Vertragsnummern.
-- Vorher wurde die nächste Nummer aus MAX(Id) berechnet. Bei gleichzeitigen Anfragen bekamen zwei Datensätze
-- dieselbe Nummer, die UNIQUE-Regel lehnte den zweiten ab → Fehler 500. NEXT VALUE FOR vergibt jede Zahl nur einmal.
-- Startwert wie bisher berechnet, damit die Zähler hinter allen schon vergebenen Nummern beginnen.
-- START WITH erlaubt nur eine feste Zahl, darum wird der Befehl als Text gebaut und mit sp_executesql ausgeführt.
DECLARE @customerStart INT = 100001 + ISNULL((SELECT MAX(Id) FROM dbo.Customer), 0);
DECLARE @sql NVARCHAR(200) = N'CREATE SEQUENCE dbo.CustomerNumberSeq AS INT START WITH '
    + CAST(@customerStart AS NVARCHAR(20)) + N' INCREMENT BY 1;';
EXEC sp_executesql @sql;
GO

DECLARE @contractStart INT = ISNULL((SELECT MAX(Id) FROM dbo.Contract), 0) + 1;
DECLARE @sql NVARCHAR(200) = N'CREATE SEQUENCE dbo.ContractNumberSeq AS INT START WITH '
    + CAST(@contractStart AS NVARCHAR(20)) + N' INCREMENT BY 1;';
EXEC sp_executesql @sql;
GO
