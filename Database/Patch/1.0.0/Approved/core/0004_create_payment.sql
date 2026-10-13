-- Zahlungseingänge
CREATE TABLE dbo.Payment
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payment PRIMARY KEY,
    ContractId  INT            NOT NULL CONSTRAINT FK_Payment_Contract REFERENCES dbo.Contract (Id),
    Amount      DECIMAL(10, 2) NOT NULL,
    PaymentDate DATE           NOT NULL,
    Reference   NVARCHAR(100)  NULL,
    RecordedAt  DATETIME2(0)   NOT NULL CONSTRAINT DF_Payment_RecordedAt DEFAULT (SYSUTCDATETIME()),
    RecordedBy  NVARCHAR(50)   NOT NULL
);
GO

CREATE INDEX IX_Payment_ContractId ON dbo.Payment (ContractId, PaymentDate);
GO
