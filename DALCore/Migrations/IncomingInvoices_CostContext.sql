-- ============================================================
-- Documentencentrum: CostContext + toewijzing
-- Voeg CostContextType, AssignedToUserId, AssignedToName toe
-- aan de IncommingInvoices-tabel.
-- ============================================================

ALTER TABLE [dbo].[IncommingInvoices]
    ADD
    -- Cost context classificatie
    [CostContextType]  NVARCHAR(30)   NOT NULL CONSTRAINT [DF_IncommingInvoices_CostContextType]  DEFAULT ('Unknown'),

    -- Toewijzing aan gebruiker (los van enrichment UserReviewedBy)
    [AssignedToUserId] NVARCHAR(450)  NULL,
    [AssignedToName]   NVARCHAR(200)  NULL;

GO

CREATE NONCLUSTERED INDEX [IX_IncommingInvoices_CostContext]
    ON [dbo].[IncommingInvoices] ([IssuerCompanyId], [CostContextType]);

GO
