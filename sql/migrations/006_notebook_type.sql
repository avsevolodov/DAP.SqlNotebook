-- Notebook type: 0 = Db (с выбранной БД), 1 = Generic (без подключения к БД).
-- Run after 005 (or after 001_full_schema if that was used).

SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = N'Notebooks' AND c.name = N'NotebookType'
)
BEGIN
    ALTER TABLE dbo.Notebooks ADD NotebookType INT NOT NULL CONSTRAINT DF_Notebooks_NotebookType DEFAULT 0;
END
GO
