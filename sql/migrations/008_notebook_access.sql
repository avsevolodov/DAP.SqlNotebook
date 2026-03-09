-- Notebook access: per-user roles for shared notebooks.
-- Run after 007_notebook_favorites.sql.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.UserNotebookAccess', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserNotebookAccess
    (
        UserLogin NVARCHAR(256) NOT NULL,
        NotebookId UNIQUEIDENTIFIER NOT NULL,
        Role INT NOT NULL CONSTRAINT DF_UserNotebookAccess_Role DEFAULT (0), -- 0 = Viewer, 1 = Editor
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_UserNotebookAccess_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserNotebookAccess PRIMARY KEY (UserLogin, NotebookId),
        CONSTRAINT FK_UserNotebookAccess_Notebook FOREIGN KEY (NotebookId)
            REFERENCES dbo.Notebooks(Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_UserNotebookAccess_UserLogin
        ON dbo.UserNotebookAccess (UserLogin);

    CREATE NONCLUSTERED INDEX IX_UserNotebookAccess_NotebookId
        ON dbo.UserNotebookAccess (NotebookId);
END
GO

