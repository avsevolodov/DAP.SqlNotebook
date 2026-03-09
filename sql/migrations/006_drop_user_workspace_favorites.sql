-- Drop workspace favorites table (feature removed; only notebook favorites are used).
-- Run after 005_db_entities_schema.sql.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.UserWorkspaceFavorites', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.UserWorkspaceFavorites;
END
GO
