-- =============================================
-- Migratie: 130_CommissionTweeDecimalen
-- Datum: 2026-11-05
-- Omschrijving: ChangeOrderDetail.Commission was decimal(18,0): een commissie van 7,5 % werd afgerond opgeslagen. Wordt decimal(9,2), zodat de commissie per rij
--   (en de standaardcommissie op Projecten/ChangeOrderDetailV2) met twee cijfers na de komma bewaard wordt. Bestaande waarden zijn al hele getallen en blijven gelijk.
--   Idempotent: tweemaal draaien doet niets extra.
-- =============================================
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ChangeOrderDetail') AND name = 'Commission' AND (scale <> 2 OR precision <> 9))
BEGIN
    ALTER TABLE [dbo].[ChangeOrderDetail] ALTER COLUMN [Commission] DECIMAL(9, 2) NOT NULL;
    PRINT 'ChangeOrderDetail.Commission is nu decimal(9,2).';
END
ELSE PRINT 'ChangeOrderDetail.Commission is al decimal(9,2), niets te doen.';
GO
