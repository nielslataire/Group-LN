-- =============================================
-- Migratie: 064_ChangeOrderOffertesEnFacturatieplan
-- Datum: 2026-09-30
-- Omschrijving: Schema voor "Offertes & wijzigingen" gl-v2 (design-handoff 20b/20c/20d,
--   design-handoff/Flow Facturatie en wijzigingen.md §5). Een offerte krijgt bewust GEEN eigen tabel
--   (beslissing Niels 2026-09-30) — ze is een ChangeOrder-rij in de vroege fase (IsQuote=1), die bij
--   "Omzetten" in-place overgaat naar een normale WO (IsQuote=0). Facturatieplan (voorschot/
--   tussentijds/saldo per WO) is wel nieuw schema: ChangeOrderPaymentTerm, met InvoicesDetails
--   uitgebreid met een FK ernaar zodat "al gefactureerd" op dezelfde manier afgeleid wordt als overal
--   elders in het project (geen aparte "reached"-tabel). Strikt additief + idempotent.
-- =============================================

-- 1) ChangeOrder: offerte-fase-velden ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'IsQuote')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [IsQuote] BIT NOT NULL CONSTRAINT DF_ChangeOrder_IsQuote DEFAULT (0);
    PRINT 'Kolom IsQuote toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom IsQuote bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'QuoteSupplierReference')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [QuoteSupplierReference] NVARCHAR(100) NULL;
    PRINT 'Kolom QuoteSupplierReference toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom QuoteSupplierReference bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'QuoteVatPercentage')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [QuoteVatPercentage] DECIMAL(5,2) NULL;
    PRINT 'Kolom QuoteVatPercentage toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom QuoteVatPercentage bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'QuoteConvertedAt')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [QuoteConvertedAt] DATETIME NULL;
    PRINT 'Kolom QuoteConvertedAt toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Kolom QuoteConvertedAt bestaat al, overgeslagen.';

-- 2) ChangeOrderDetail: OCR-controleervlag + bijgesneden foto --------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrderDetail') AND name = 'NeedsReview')
BEGIN
    ALTER TABLE [dbo].[ChangeOrderDetail] ADD [NeedsReview] BIT NOT NULL CONSTRAINT DF_ChangeOrderDetail_NeedsReview DEFAULT (0);
    PRINT 'Kolom NeedsReview toegevoegd aan ChangeOrderDetail.';
END
ELSE PRINT 'Kolom NeedsReview bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrderDetail') AND name = 'SourceImagePath')
BEGIN
    ALTER TABLE [dbo].[ChangeOrderDetail] ADD [SourceImagePath] NVARCHAR(300) NULL;
    PRINT 'Kolom SourceImagePath toegevoegd aan ChangeOrderDetail.';
END
ELSE PRINT 'Kolom SourceImagePath bestaat al, overgeslagen.';

-- 3) ChangeOrderPaymentTerm: het facturatieplan (§5) -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ChangeOrderPaymentTerm')
BEGIN
    CREATE TABLE [dbo].[ChangeOrderPaymentTerm] (
        [Id]             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ChangeOrderId]  INT NOT NULL,
        [Kind]           TINYINT NOT NULL,   -- 1=Voorschot, 2=Tussentijds, 3=Saldo
        [Percentage]     DECIMAL(5,2) NULL,
        [FixedAmount]    DECIMAL(18,2) NULL,
        [TriggerType]    TINYINT NOT NULL,   -- 1=NaOndertekening, 2=BijSchijf, 3=Manueel
        [TriggerStageId] INT NULL,
        [ReleasedAt]     DATETIME NULL,
        [SortOrder]      INT NOT NULL CONSTRAINT DF_ChangeOrderPaymentTerm_SortOrder DEFAULT (0),
        [CreatedAt]      DATETIME NOT NULL CONSTRAINT DF_ChangeOrderPaymentTerm_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT [FK_ChangeOrderPaymentTerm_ChangeOrder] FOREIGN KEY ([ChangeOrderId]) REFERENCES [dbo].[ChangeOrder]([Id]),
        CONSTRAINT [FK_ChangeOrderPaymentTerm_Stage] FOREIGN KEY ([TriggerStageId]) REFERENCES [dbo].[InvoicingPaymentStages]([Id])
    );
    PRINT 'Tabel ChangeOrderPaymentTerm aangemaakt.';
END
ELSE PRINT 'Tabel ChangeOrderPaymentTerm bestaat al, overgeslagen.';

-- 4) InvoicesDetails: link naar de termijn die gefactureerd werd ----------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoicesDetails') AND name = 'ChangeOrderPaymentTermId')
BEGIN
    ALTER TABLE [dbo].[InvoicesDetails] ADD [ChangeOrderPaymentTermId] INT NULL;
    ALTER TABLE [dbo].[InvoicesDetails]
        ADD CONSTRAINT [FK_InvoicesDetails_ChangeOrderPaymentTerm] FOREIGN KEY ([ChangeOrderPaymentTermId]) REFERENCES [dbo].[ChangeOrderPaymentTerm]([Id]);
    PRINT 'Kolom ChangeOrderPaymentTermId toegevoegd aan InvoicesDetails.';
END
ELSE PRINT 'Kolom ChangeOrderPaymentTermId bestaat al, overgeslagen.';

PRINT 'Migratie 064_ChangeOrderOffertesEnFacturatieplan voltooid.';
