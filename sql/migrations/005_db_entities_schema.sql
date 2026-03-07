-- Add SchemaName to DbEntities (e.g. dbo, sys) for table schema display.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.DbEntities') AND name = N'SchemaName')
BEGIN
    ALTER TABLE dbo.DbEntities ADD SchemaName NVARCHAR(256) NULL;
END
GO
