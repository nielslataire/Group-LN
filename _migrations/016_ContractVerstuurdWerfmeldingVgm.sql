-- =============================================
-- Migratie: ContractVerstuurdWerfmeldingVgm
-- Datum: 2026-09-01
-- Omschrijving: Voegt aan Contract drie extra opvolgvelden toe:
--               - ContractSentDate  : datum waarop het contract verstuurd is
--               - ContractSentNote  : vrije opmerking bij de verzending (bv. "Bestelbon")
--               - SiteNotification  : werfmelding gebeurd (ja/nee)
--               - VgmCharter        : VGM-charter ondertekend (ja/nee)
-- =============================================

IF COL_LENGTH('dbo.Contract', 'ContractSentDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [ContractSentDate] DATE NULL;
    PRINT 'Kolom Contract.ContractSentDate toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.ContractSentDate bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Contract', 'ContractSentNote') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [ContractSentNote] NVARCHAR(200) NULL;
    PRINT 'Kolom Contract.ContractSentNote toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.ContractSentNote bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Contract', 'SiteNotification') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [SiteNotification] BIT NOT NULL
        CONSTRAINT [DF_Contract_SiteNotification] DEFAULT (0);
    PRINT 'Kolom Contract.SiteNotification toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.SiteNotification bestaat al, overgeslagen.';

IF COL_LENGTH('dbo.Contract', 'VgmCharter') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [VgmCharter] BIT NOT NULL
        CONSTRAINT [DF_Contract_VgmCharter] DEFAULT (0);
    PRINT 'Kolom Contract.VgmCharter toegevoegd.';
END
ELSE
    PRINT 'Kolom Contract.VgmCharter bestaat al, overgeslagen.';
