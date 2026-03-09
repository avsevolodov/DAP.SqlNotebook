-- Notebook favorites: per-user folders and favorite notebook list (notebooks only).
-- Run after 006_drop_user_workspace_favorites.sql.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.UserFavoriteFolders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserFavoriteFolders (
        Id UNIQUEIDENTIFIER NOT NULL,
        UserLogin NVARCHAR(256) NOT NULL,
        Name NVARCHAR(256) NOT NULL,
        ParentId UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_UserFavoriteFolders PRIMARY KEY (Id)
    );
    CREATE NONCLUSTERED INDEX IX_UserFavoriteFolders_UserLogin ON dbo.UserFavoriteFolders (UserLogin);
    CREATE NONCLUSTERED INDEX IX_UserFavoriteFolders_ParentId ON dbo.UserFavoriteFolders (ParentId);
END
GO

IF OBJECT_ID(N'dbo.UserNotebookFavorites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserNotebookFavorites (
        UserLogin NVARCHAR(256) NOT NULL,
        NotebookId UNIQUEIDENTIFIER NOT NULL,
        FolderId UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_UserNotebookFavorites PRIMARY KEY (UserLogin, NotebookId),
        CONSTRAINT FK_UserNotebookFavorites_Notebook FOREIGN KEY (NotebookId) REFERENCES dbo.Notebooks(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserNotebookFavorites_Folder FOREIGN KEY (FolderId) REFERENCES dbo.UserFavoriteFolders(Id) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX IX_UserNotebookFavorites_UserLogin ON dbo.UserNotebookFavorites (UserLogin);
    CREATE NONCLUSTERED INDEX IX_UserNotebookFavorites_FolderId ON dbo.UserNotebookFavorites (FolderId);
END
GO
