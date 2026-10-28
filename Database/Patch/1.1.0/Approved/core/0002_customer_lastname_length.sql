-- Nachname: Spalte von 80 auf 100 Zeichen vergrößern, passend zu CustomerValidator.LastNameMaxLength.
-- Vorher führten Nachnamen mit 81 bis 100 Zeichen zu Fehler 500
-- Bei ALTER COLUMN muss NOT NULL wieder angegeben werden, sonst würde die Spalte danach NULL erlauben.
ALTER TABLE dbo.Customer ALTER COLUMN LastName NVARCHAR(100) NOT NULL;
GO
