-- =============================================
-- Migratie: ContractAdditionalOrder (bijbestellingen op een contract)
-- Datum: 2026-09-09
-- Omschrijving:
--   Een bijbestelling hangt onder een bestaand lot (ContractActivity) van een
--   leverancierscontract, heeft een vrije omschrijving en een prijs. De prijs
--   telt mee in de totale contractprijs, in het gecontracteerde bedrag van de
--   nacalculatie/voortgang en in de facturatievergelijking.
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE object_id = OBJECT_ID(N'[dbo].[ContractAdditionalOrder]')
)
BEGIN
    CREATE TABLE [dbo].[ContractAdditionalOrder] (
        [Id]                 INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ContractActivityId] INT             NOT NULL,
        [Description]         NVARCHAR(200)   NULL,
        [Price]              DECIMAL(19, 4)  NOT NULL DEFAULT 0,
        [CreatedAt]          DATETIME2       NOT NULL DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [FK_ContractAdditionalOrder_ContractActivity]
            FOREIGN KEY ([ContractActivityId]) REFERENCES [dbo].[ContractActivity]([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_ContractAdditionalOrder_ContractActivityId]
        ON [dbo].[ContractAdditionalOrder] ([ContractActivityId]);

    PRINT 'Tabel ContractAdditionalOrder aangemaakt.';
END
ELSE
    PRINT 'Tabel ContractAdditionalOrder bestaat al, overgeslagen.';
