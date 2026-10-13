-- Kündigung von Verträgen (siehe docs/features/kuendigung.md)
ALTER TABLE dbo.Contract ADD
    CancelledAt        DATE          NULL,
    CancellationReason NVARCHAR(200) NULL;
GO
