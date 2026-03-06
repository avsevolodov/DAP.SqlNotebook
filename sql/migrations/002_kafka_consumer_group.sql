-- Kafka источники: consumer group prefix и автогенерация (GUID suffix).
-- Run on existing DB after 001_full_schema.sql (or older installations).

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.DataMartNodes') AND name = 'ConsumerGroupPrefix'
)
BEGIN
    ALTER TABLE dbo.DataMartNodes ADD ConsumerGroupPrefix NVARCHAR(256) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.DataMartNodes') AND name = 'ConsumerGroupAutoGenerate'
)
BEGIN
    ALTER TABLE dbo.DataMartNodes
        ADD ConsumerGroupAutoGenerate BIT NOT NULL
            CONSTRAINT DF_DataMartNodes_ConsumerGroupAutoGenerate DEFAULT 0;
END
GO

