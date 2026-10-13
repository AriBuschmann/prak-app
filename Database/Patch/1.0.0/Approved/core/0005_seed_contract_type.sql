-- Stammdaten: Vertragsarten
SET IDENTITY_INSERT dbo.ContractType ON;

INSERT INTO dbo.ContractType (Id, Code, Name, MinTermMonths, NoticePeriodMonths)
VALUES (1, N'BASIS',   N'Basis-Tarif',    0, 1),
       (2, N'KOMFORT', N'Komfort-Tarif', 12, 3),
       (3, N'PREMIUM', N'Premium-Tarif', 24, 3),
       (4, N'JAHRES',  N'Jahresabo',     12, 1);

SET IDENTITY_INSERT dbo.ContractType OFF;
GO
