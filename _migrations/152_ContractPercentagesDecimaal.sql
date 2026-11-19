-- =============================================
-- Migratie: 152_ContractPercentagesDecimaal
-- Omschrijving: Contract.CashDiscountPercentage, GuaranteePercentage en VatPercentage waren numeric(18,0):
--   SQL Server rondde 2,5 % bij het opslaan af naar 3. Worden decimal(9,4) zodat decimalen bewaard blijven.
--   Betaaltermijnen (dagen) blijven numeric(18,0) - dat zijn hele getallen.
--   Verbreden van de schaal verliest geen bestaande waarden. Idempotent (enkel kolommen met schaal 0 worden aangepast).
--   Draai vóór app-start.
-- =============================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Contract]') AND name = N'CashDiscountPercentage' AND scale = 0)
    ALTER TABLE [dbo].[Contract] ALTER COLUMN [CashDiscountPercentage] DECIMAL(9,4) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Contract]') AND name = N'GuaranteePercentage' AND scale = 0)
    ALTER TABLE [dbo].[Contract] ALTER COLUMN [GuaranteePercentage] DECIMAL(9,4) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Contract]') AND name = N'VatPercentage' AND scale = 0)
    ALTER TABLE [dbo].[Contract] ALTER COLUMN [VatPercentage] DECIMAL(9,4) NULL;
GO
