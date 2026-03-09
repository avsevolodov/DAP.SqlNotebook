-- User workspace favorites: per-user list of favorite workspace IDs (stored on server).
-- Run after 003_workspace_hierarchy.sql.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.UserWorkspaceFavorites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserWorkspaceFavorites (
        UserLogin NVARCHAR(256) NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_UserWorkspaceFavorites PRIMARY KEY (UserLogin, WorkspaceId),
        CONSTRAINT FK_UserWorkspaceFavorites_Workspace FOREIGN KEY (WorkspaceId) REFERENCES dbo.Workspaces(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_UserWorkspaceFavorites_UserLogin ON dbo.UserWorkspaceFavorites (UserLogin);
END
GO
