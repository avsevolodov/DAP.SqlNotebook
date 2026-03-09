-- NotebookCells.Title: optional per-cell title for TOC.
-- Run after 001_full_schema.sql on existing DB.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.NotebookCells') AND name = N'Title')
BEGIN
    ALTER TABLE dbo.NotebookCells
        ADD Title NVARCHAR(256) NULL;
END
GO

