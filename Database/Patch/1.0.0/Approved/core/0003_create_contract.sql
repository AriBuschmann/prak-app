-- Verträge
CREATE TABLE dbo.Contract
(
    Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Contract PRIMARY KEY,
    ContractNumber NVARCHAR(20)   NOT NULL CONSTRAINT UQ_Contract_ContractNumber UNIQUE,
    CustomerId     INT            NOT NULL CONSTRAINT FK_Contract_Customer REFERENCES dbo.Customer (Id),
    ContractTypeId INT            NOT NULL CONSTRAINT FK_Contract_ContractType REFERENCES dbo.ContractType (Id),
    StartDate      DATE           NOT NULL,
    EndDate        DATE           NULL,
    MonthlyFee     DECIMAL(10, 2) NOT NULL,
    Status         NVARCHAR(20)   NOT NULL CONSTRAINT DF_Contract_Status DEFAULT (N'Active'),
    CreatedAt      DATETIME2(0)   NOT NULL CONSTRAINT DF_Contract_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy      NVARCHAR(50)   NOT NULL,
    CONSTRAINT CK_Contract_Status CHECK (Status IN (N'Active', N'Cancelled', N'Ended')),
    CONSTRAINT CK_Contract_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate)
);
GO

CREATE INDEX IX_Contract_CustomerId ON dbo.Contract (CustomerId);
GO
