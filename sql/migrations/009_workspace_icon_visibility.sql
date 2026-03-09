-- Workspace icon & visibility fields.
-- Run after 003_workspace_hierarchy.sql on existing DB.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Workspaces') AND name = N'Icon')
BEGIN
    ALTER TABLE dbo.Workspaces
        ADD Icon NVARCHAR(64) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Workspaces') AND name = N'Visibility')
BEGIN
    ALTER TABLE dbo.Workspaces
        ADD Visibility INT NOT NULL CONSTRAINT DF_Workspaces_Visibility DEFAULT (0);
END
GO

