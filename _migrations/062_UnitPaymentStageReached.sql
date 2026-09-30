-- =============================================
-- Migratie: 062_UnitPaymentStageReached
-- Datum: 2026-09-30
-- Omschrijving: Nieuwe tabel voor "schijf bereikt aanduiden" per eenheid (design-handoff 21g/21h,
--   Projecten/PaymentStagesV2). InvoicingPaymentStages.Invoicable blijft ongewijzigd een groep-brede
--   vlag ("alle eenheden van deze groep mogen deze schijf factureren") — dat gedrag van bestaande
--   projecten mag niet stilzwijgend veranderen. Deze tabel komt er BOVENOP als fijnmaziger, per
--   eenheid alternatief: een eenheid×schijf-combinatie is factureerbaar zodra ÓF de groep-vlag
--   Invoicable=1 staat ÓF er hier een rij voor bestaat — zie GetInvoicableRows-achtige logica in
--   ProjectenController.PaymentStagesV2.cs/InvoicingV2.cs. Strikt additief + idempotent.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UnitPaymentStageReached')
BEGIN
    CREATE TABLE [dbo].[UnitPaymentStageReached] (
        [Id]               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UnitId]           INT NOT NULL,
        [PaymentStageId]   INT NOT NULL,
        [ReachedDate]      DATE NOT NULL,
        [ReachedByUserId]  INT NULL,
        [ProofDocId]       INT NULL,
        [CreatedAt]        DATETIME NOT NULL CONSTRAINT DF_UnitPaymentStageReached_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT [UQ_UnitPaymentStageReached_Unit_Stage] UNIQUE ([UnitId], [PaymentStageId]),
        CONSTRAINT [FK_UnitPaymentStageReached_Unit] FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]),
        CONSTRAINT [FK_UnitPaymentStageReached_Stage] FOREIGN KEY ([PaymentStageId]) REFERENCES [dbo].[InvoicingPaymentStages]([Id]),
        CONSTRAINT [FK_UnitPaymentStageReached_ProofDoc] FOREIGN KEY ([ProofDocId]) REFERENCES [dbo].[ProjectDocs]([Id])
    );
    PRINT 'Tabel UnitPaymentStageReached aangemaakt.';
END
ELSE
    PRINT 'Tabel UnitPaymentStageReached bestaat al, overgeslagen.';

PRINT 'Migratie 062_UnitPaymentStageReached voltooid.';
