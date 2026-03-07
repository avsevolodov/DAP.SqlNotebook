-- SqlNotebook: полная миграция (одна схема для пустой БД).
-- Объединяет все изменения из 001..014. Запускать один раз на пустой базе.

SET NOCOUNT ON;
GO

-- ========== Workspaces ==========
IF OBJECT_ID(N'dbo.Workspaces', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Workspaces (
        Id UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(256) NOT NULL,
        Description NVARCHAR(2048) NULL,
        OwnerLogin NVARCHAR(256) NULL,
        CONSTRAINT PK_Workspaces PRIMARY KEY (Id)
    );
    CREATE NONCLUSTERED INDEX IX_Workspaces_OwnerLogin ON dbo.Workspaces (OwnerLogin);
END
GO

-- ========== Users ==========
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id UNIQUEIDENTIFIER NOT NULL,
        Login NVARCHAR(256) NULL,
        Role NVARCHAR(32) NULL,
        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_Users_Login ON dbo.Users (Login);
END
GO

-- ========== DbEntities ==========
IF OBJECT_ID(N'dbo.DbEntities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DbEntities (
        Id UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(256) NOT NULL,
        DisplayName NVARCHAR(512) NULL,
        Description NVARCHAR(MAX) NULL,
        CONSTRAINT PK_DbEntities PRIMARY KEY (Id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_DbEntities_Name ON dbo.DbEntities (Name);
END
GO

-- ========== DbFields ==========
IF OBJECT_ID(N'dbo.DbFields', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DbFields (
        Id UNIQUEIDENTIFIER NOT NULL,
        EntityId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(256) NOT NULL,
        DataType NVARCHAR(256) NULL,
        IsNullable BIT NOT NULL,
        IsPrimaryKey BIT NOT NULL,
        Description NVARCHAR(MAX) NULL,
        CONSTRAINT PK_DbFields PRIMARY KEY (Id),
        CONSTRAINT FK_DbFields_DbEntities FOREIGN KEY (EntityId) REFERENCES dbo.DbEntities(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_DbFields_EntityId_Name ON dbo.DbFields (EntityId, Name);
END
GO

-- ========== DbRelations ==========
IF OBJECT_ID(N'dbo.DbRelations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DbRelations (
        Id UNIQUEIDENTIFIER NOT NULL,
        FromFieldName NVARCHAR(256) NOT NULL,
        ToFieldName NVARCHAR(256) NOT NULL,
        Name NVARCHAR(512) NULL,
        Description NVARCHAR(MAX) NULL,
        CONSTRAINT PK_DbRelations PRIMARY KEY (Id)
    );
END
GO

-- ========== DataMartNodes (каталог: источники, БД, таблицы) ==========
IF OBJECT_ID(N'dbo.DataMartNodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DataMartNodes (
        Id UNIQUEIDENTIFIER NOT NULL,
        ParentId UNIQUEIDENTIFIER NULL,
        Type INT NOT NULL,
        Name NVARCHAR(256) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Owner NVARCHAR(256) NULL,
        Provider NVARCHAR(64) NULL,
        ConnectionInfo NVARCHAR(2048) NULL,
        SortOrder INT NOT NULL,
        EntityId UNIQUEIDENTIFIER NULL,
        DatabaseName NVARCHAR(256) NULL,
        AuthType NVARCHAR(32) NULL,
        Login NVARCHAR(256) NULL,
        PasswordEncrypted NVARCHAR(2048) NULL,
        ConsumerGroupPrefix NVARCHAR(256) NULL,
        ConsumerGroupAutoGenerate BIT NOT NULL CONSTRAINT DF_DataMartNodes_ConsumerGroupAutoGenerate DEFAULT 0,
        CONSTRAINT PK_DataMartNodes PRIMARY KEY (Id),
        CONSTRAINT FK_DataMartNodes_Parent FOREIGN KEY (ParentId) REFERENCES dbo.DataMartNodes(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_DataMartNodes_Entity FOREIGN KEY (EntityId) REFERENCES dbo.DbEntities(Id) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX IX_DataMartNodes_ParentId ON dbo.DataMartNodes (ParentId);
END
GO

-- ========== Notebooks ==========
IF OBJECT_ID(N'dbo.Notebooks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notebooks (
        Id UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(512) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL,
        CreatedBy NVARCHAR(256) NULL,
        UpdatedBy NVARCHAR(256) NULL,
        WorkspaceId UNIQUEIDENTIFIER NULL,
        CatalogNodeId UNIQUEIDENTIFIER NULL,
        CatalogNodeDisplayName NVARCHAR(256) NULL,
        NotebookType INT NOT NULL CONSTRAINT DF_Notebooks_NotebookType DEFAULT 0,
        CONSTRAINT PK_Notebooks PRIMARY KEY (Id),
        CONSTRAINT FK_Notebooks_Workspaces FOREIGN KEY (WorkspaceId) REFERENCES dbo.Workspaces(Id) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX IX_Notebooks_UpdatedAt ON dbo.Notebooks (UpdatedAt);
    CREATE NONCLUSTERED INDEX IX_Notebooks_WorkspaceId ON dbo.Notebooks (WorkspaceId);
END
GO

-- ========== NotebookCells ==========
IF OBJECT_ID(N'dbo.NotebookCells', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotebookCells (
        Id INT IDENTITY(1,1) NOT NULL,
        NotebookId UNIQUEIDENTIFIER NOT NULL,
        OrderIndex INT NOT NULL,
        CellType INT NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        ExecutionResultJson NVARCHAR(MAX) NULL,
        CreatedBy NVARCHAR(256) NULL,
        CatalogNodeId UNIQUEIDENTIFIER NULL,
        DatabaseDisplayName NVARCHAR(256) NULL,
        CONSTRAINT PK_NotebookCells PRIMARY KEY (Id),
        CONSTRAINT FK_NotebookCells_Notebooks FOREIGN KEY (NotebookId) REFERENCES dbo.Notebooks(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_NotebookCells_NotebookId ON dbo.NotebookCells (NotebookId);
END
GO

-- ========== AiAssistSessions ==========
IF OBJECT_ID(N'dbo.AiAssistSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiAssistSessions (
        Id UNIQUEIDENTIFIER NOT NULL,
        UserLogin NVARCHAR(256) NULL,
        Title NVARCHAR(512) NULL,
        CreatedAt DATETIME2 NOT NULL,
        NotebookId UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_AiAssistSessions PRIMARY KEY (Id)
    );
    CREATE NONCLUSTERED INDEX IX_AiAssistSessions_UserLogin_CreatedAt ON dbo.AiAssistSessions (UserLogin, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_AiAssistSessions_NotebookId ON dbo.AiAssistSessions (NotebookId);
END
GO

-- ========== AiAssistMessages ==========
IF OBJECT_ID(N'dbo.AiAssistMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiAssistMessages (
        Id UNIQUEIDENTIFIER NOT NULL,
        NotebookId UNIQUEIDENTIFIER NULL,
        SessionId UNIQUEIDENTIFIER NULL,
        Content NVARCHAR(MAX) NULL,
        Role INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UserLogin NVARCHAR(256) NULL,
        CONSTRAINT PK_AiAssistMessages PRIMARY KEY (Id),
        CONSTRAINT FK_AiAssistMessages_Notebooks FOREIGN KEY (NotebookId) REFERENCES dbo.Notebooks(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AiAssistMessages_Sessions FOREIGN KEY (SessionId) REFERENCES dbo.AiAssistSessions(Id) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX IX_AiAssistMessages_NotebookId ON dbo.AiAssistMessages (NotebookId);
    CREATE NONCLUSTERED INDEX IX_AiAssistMessages_NotebookId_CreatedAt ON dbo.AiAssistMessages (NotebookId, CreatedAt);
    CREATE NONCLUSTERED INDEX IX_AiAssistMessages_SessionId ON dbo.AiAssistMessages (SessionId);
    CREATE NONCLUSTERED INDEX IX_AiAssistMessages_UserLogin ON dbo.AiAssistMessages (UserLogin);
    CREATE NONCLUSTERED INDEX IX_AiAssistMessages_UserLogin_CreatedAt ON dbo.AiAssistMessages (UserLogin, CreatedAt);
END
GO

-- ========== Seed: default workspace ==========
IF NOT EXISTS (SELECT 1 FROM dbo.Workspaces)
BEGIN
    INSERT INTO dbo.Workspaces (Id, Name, Description, OwnerLogin)
    VALUES (
        '11111111-1111-1111-1111-111111111111',
        N'Default',
        N'Default workspace',
        NULL
    );
END
GO
