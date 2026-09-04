-- =============================================
-- Migratie: IssuerCompanyExterneStandaard
-- Datum: 2026-09-04
-- Omschrijving:
--   - IssuerCompany.IsExternalCoordinationDefault : markeert één facturatiebedrijf als
--     verzamelbak voor leveranciers van contracten waarvan de opdrachtgever geen eigen
--     facturatiebedrijf is (bv. coördinatiecontracten voor een externe bouwheer).
-- =============================================

IF COL_LENGTH('dbo.IssuerCompany', 'IsExternalCoordinationDefault') IS NULL
BEGIN
    ALTER TABLE [dbo].[IssuerCompany] ADD [IsExternalCoordinationDefault] BIT NOT NULL
        CONSTRAINT [DF_IssuerCompany_IsExternalCoordinationDefault] DEFAULT (0);
    PRINT 'Kolom IssuerCompany.IsExternalCoordinationDefault toegevoegd.';
END
ELSE
    PRINT 'Kolom IssuerCompany.IsExternalCoordinationDefault bestaat al, overgeslagen.';
