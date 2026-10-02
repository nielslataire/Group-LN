-- =============================================
-- Migratie: 067_ChangeOrderDetailSortOrder
-- Datum: 2026-10-02
-- Omschrijving: "Offertes & wijzigingen" gl-v2, design-handoff punt 28a/28b — de regels van een
--   wijzigingsopdracht hebben een sleepgreep om ze te herschikken. Tot nu toe bestond er geen
--   volgordekolom: de regels stonden altijd in volgorde van aanmaak (Id). SortOrder bewaart de
--   volgorde zoals ze op het opmaakscherm staat; het document dat de klant tekent volgt dezelfde
--   volgorde. NULL = nog nooit herschikt (bestaande en via de oude schermen gemaakte regels): die
--   blijven op Id gesorteerd, achter de regels mét een volgorde. Strikt additief + idempotent.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrderDetail') AND name = 'SortOrder')
BEGIN
    ALTER TABLE [dbo].[ChangeOrderDetail] ADD [SortOrder] INT NULL;
    PRINT 'Kolom SortOrder toegevoegd aan ChangeOrderDetail.';
END
ELSE PRINT 'Kolom SortOrder bestaat al, overgeslagen.';
