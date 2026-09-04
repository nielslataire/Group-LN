-- =============================================
-- Migratie: ContractWaarborgDocument
-- Datum: 2026-09-04
-- Omschrijving:
--   - Contract.GuaranteeDocFilename   : bestandsnaam (Storage-API) van het bankwaarborg-document
--   - Contract.GuaranteeDocUploadedAt : tijdstip waarop het document werd toegevoegd
--   Vereist zodra de waarborg een "Bankwaarborg" is; zolang leeg toont de UI een waarschuwing.
-- =============================================

IF COL_LENGTH('dbo.Contract', 'GuaranteeDocFilename') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [GuaranteeDocFilename] NVARCHAR(260) NULL;
    PRINT 'Kolom Contract.GuaranteeDocFilename toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.GuaranteeDocFilename bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Contract', 'GuaranteeDocUploadedAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [GuaranteeDocUploadedAt] DATETIME2 NULL;
    PRINT 'Kolom Contract.GuaranteeDocUploadedAt toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.GuaranteeDocUploadedAt bestaat al, overgeslagen.';
