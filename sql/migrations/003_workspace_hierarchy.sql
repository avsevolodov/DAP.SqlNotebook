-- Workspace hierarchy: folders and tree structure.
-- Run after 001_full_schema.sql on existing DB.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Workspaces') AND name = N'ParentId')
BEGIN
    ALTER TABLE dbo.Workspaces ADD ParentId UNIQUEIDENTIFIER NULL;
    CREATE NONCLUSTERED INDEX IX_Workspaces_ParentId ON dbo.Workspaces (ParentId);
    ALTER TABLE dbo.Workspaces
        ADD CONSTRAINT FK_Workspaces_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Workspaces(Id) ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Workspaces') AND name = N'IsFolder')
BEGIN
    ALTER TABLE dbo.Workspaces ADD IsFolder BIT NOT NULL CONSTRAINT DF_Workspaces_IsFolder DEFAULT 0;
END
GO
