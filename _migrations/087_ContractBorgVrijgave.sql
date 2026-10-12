-- =============================================
-- Migratie: ContractBorgVrijgave
-- Datum: 2026-10-10
-- Omschrijving:
--   - Contract.GuaranteeReleasedVOAt      : datum waarop de borg (deels) vrijgegeven werd bij
--                                            voorlopige oplevering (Project.DeliveryDate)
--   - Contract.GuaranteeReleasedVOAmount  : vrijgegeven bedrag bij die vrijgave (meestal 50 %)
--   - Contract.GuaranteeReleasedDOAt      : idem bij definitieve oplevering (Project.DeliveryDateDef)
--   - Contract.GuaranteeReleasedDOAmount  : vrijgegeven bedrag bij die vrijgave
--   Design-handoff 42e punt 04 ("Borg opvolgen"). Alle 4 kolommen NULL = nog niets vrijgegeven —
--   bestaande contracten/schermen blijven ongewijzigd werken, niemand leest deze kolommen vandaag.
-- =============================================

IF COL_LENGTH('dbo.Contract', 'GuaranteeReleasedVOAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [GuaranteeReleasedVOAt] DATE NULL;
    PRINT 'Kolom Contract.GuaranteeReleasedVOAt toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.GuaranteeReleasedVOAt bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Contract', 'GuaranteeReleasedVOAmount') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [GuaranteeReleasedVOAmount] DECIMAL(18,2) NULL;
    PRINT 'Kolom Contract.GuaranteeReleasedVOAmount toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.GuaranteeReleasedVOAmount bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Contract', 'GuaranteeReleasedDOAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [GuaranteeReleasedDOAt] DATE NULL;
    PRINT 'Kolom Contract.GuaranteeReleasedDOAt toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.GuaranteeReleasedDOAt bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Contract', 'GuaranteeReleasedDOAmount') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [GuaranteeReleasedDOAmount] DECIMAL(18,2) NULL;
    PRINT 'Kolom Contract.GuaranteeReleasedDOAmount toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.GuaranteeReleasedDOAmount bestaat al, overgeslagen.';
