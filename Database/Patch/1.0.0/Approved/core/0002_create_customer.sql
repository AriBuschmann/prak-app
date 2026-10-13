-- Kunden
CREATE TABLE dbo.Customer
(
    Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customer PRIMARY KEY,
    CustomerNumber NVARCHAR(12)  NOT NULL CONSTRAINT UQ_Customer_CustomerNumber UNIQUE,
    FirstName      NVARCHAR(50)  NOT NULL,
    LastName       NVARCHAR(80)  NOT NULL,
    Email          NVARCHAR(200) NULL,
    Phone          NVARCHAR(30)  NULL,
    Street         NVARCHAR(100) NULL,
    PostalCode     NVARCHAR(10)  NULL,
    City           NVARCHAR(80)  NULL,
    CreatedAt      DATETIME2(0)  NOT NULL CONSTRAINT DF_Customer_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy      NVARCHAR(50)  NOT NULL
);
GO

CREATE INDEX IX_Customer_LastName ON dbo.Customer (LastName, FirstName);
GO
