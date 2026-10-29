-- Kündigung zurücknehmen (siehe docs/features/kuendigung.md)
-- Beim Kündigen wird EndDate durch das berechnete Vertragsende ersetzt. EndDateBeforeCancellation merkt sich den
-- alten Wert (NULL = unbefristet), damit eine Rücknahme ihn wiederherstellen kann.
-- CancellationUndoable = 1 für Kündigungen ab diesem Patch. Bei früheren Kündigungen ist das alte Vertragsende
-- unbekannt (NULL könnte "unbefristet" oder "nicht bekannt" heißen), deshalb bleiben sie 0 und sind nicht rücknehmbar.
ALTER TABLE dbo.Contract ADD
    EndDateBeforeCancellation DATE NULL,
    CancellationUndoable      BIT  NOT NULL CONSTRAINT DF_Contract_CancellationUndoable DEFAULT (0);
GO
