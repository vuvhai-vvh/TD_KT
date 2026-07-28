USE [TDKT];
GO

IF COL_LENGTH('dbo.DocumentProposals', 'FileData') IS NULL
BEGIN
    ALTER TABLE dbo.DocumentProposals ADD FileData VARBINARY(MAX) NULL;
END;
GO

IF COL_LENGTH('dbo.UnitDocumentProposals', 'FileData') IS NULL
BEGIN
    ALTER TABLE dbo.UnitDocumentProposals ADD FileData VARBINARY(MAX) NULL;
END;
GO
