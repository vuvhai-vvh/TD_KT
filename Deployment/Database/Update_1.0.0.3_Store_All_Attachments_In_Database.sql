USE [TDKT];
GO

IF COL_LENGTH('dbo.DocumentProposals', 'FileData') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentProposals
    ADD FileData VARBINARY(MAX) NULL;
END;
GO

IF COL_LENGTH('dbo.UnitDocumentProposals', 'FileData') IS NULL
BEGIN
    ALTER TABLE dbo.UnitDocumentProposals
    ADD FileData VARBINARY(MAX) NULL;
END;
GO

IF COL_LENGTH('dbo.DecisionAttachments', 'FileData') IS NULL
BEGIN
    ALTER TABLE dbo.DecisionAttachments
    ADD FileData VARBINARY(MAX) NULL;
END;
GO

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME IN
  (
      'DocumentProposals',
      'UnitDocumentProposals',
      'DecisionAttachments'
  )
  AND COLUMN_NAME = 'FileData'
ORDER BY TABLE_NAME;
GO
