-- =============================================
-- 166 (26/11/2026): aktefacturen per facturatiebedrijf instelbaar.
--   AkteGrondVatTypeId / AkteGrondPaymentTermId     : btw-code en betaaltermijn van de grondfactuur
--   AkteKostenVatTypeId / AkteKostenPaymentTermId   : btw-code en betaaltermijn van de kostenfactuur
--   AkteWerkenPaymentTermId                         : betaaltermijn van de werkenfactuur die bij de akte klaargezet wordt
-- Alles NULL = oud gedrag (grond 0 %, kosten 21 %, standaardtermijn van het bedrijf). Additief en idempotent.
-- =============================================
SET NOCOUNT ON;
IF COL_LENGTH('dbo.IssuerCompany', 'AkteGrondVatTypeId') IS NULL ALTER TABLE dbo.IssuerCompany ADD AkteGrondVatTypeId INT NULL;
IF COL_LENGTH('dbo.IssuerCompany', 'AkteGrondPaymentTermId') IS NULL ALTER TABLE dbo.IssuerCompany ADD AkteGrondPaymentTermId INT NULL;
IF COL_LENGTH('dbo.IssuerCompany', 'AkteKostenVatTypeId') IS NULL ALTER TABLE dbo.IssuerCompany ADD AkteKostenVatTypeId INT NULL;
IF COL_LENGTH('dbo.IssuerCompany', 'AkteKostenPaymentTermId') IS NULL ALTER TABLE dbo.IssuerCompany ADD AkteKostenPaymentTermId INT NULL;
IF COL_LENGTH('dbo.IssuerCompany', 'AkteWerkenPaymentTermId') IS NULL ALTER TABLE dbo.IssuerCompany ADD AkteWerkenPaymentTermId INT NULL;
PRINT N'KLAAR: 5 kolommen op IssuerCompany.';
