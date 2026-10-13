-- Vertragsarten (Stammdaten)
CREATE TABLE dbo.ContractType
(
    Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContractType PRIMARY KEY,
    Code               NVARCHAR(20)  NOT NULL CONSTRAINT UQ_ContractType_Code UNIQUE,
    Name               NVARCHAR(100) NOT NULL,
    MinTermMonths      INT           NOT NULL CONSTRAINT DF_ContractType_MinTermMonths DEFAULT (0),
    NoticePeriodMonths INT           NOT NULL CONSTRAINT DF_ContractType_NoticePeriodMonths DEFAULT (1)
);
GO
