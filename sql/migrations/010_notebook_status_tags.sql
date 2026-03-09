-- Notebook status and tags columns.
-- Run after 001_full_schema.sql on existing DB.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Notebooks') AND name = N'Status')
BEGIN
    ALTER TABLE dbo.Notebooks
        ADD Status INT NOT NULL CONSTRAINT DF_Notebooks_Status DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Notebooks') AND name = N'TagsJson')
BEGIN
    ALTER TABLE dbo.Notebooks
        ADD TagsJson NVARCHAR(2048) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Notebooks') AND name = N'IX_Notebooks_Status')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notebooks_Status ON dbo.Notebooks (Status);
END
GO

