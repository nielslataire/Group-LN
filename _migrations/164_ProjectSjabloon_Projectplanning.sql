-- =============================================
-- 164 (25/11/2026): projectsjabloon krijgt een planning voor het project zelf (hoofdplanning, bv. de wegenis van een verkaveling)
--   naast de planning voor de eenheden (woningen), en een standaardmoment waarop de woningen starten.
--   PlanningSjabloonProjectId : planningssjabloon dat in de (lege) hoofdplanning komt; leeg = geen.
--   WoningenStartNa           : bouwstaatcode waarna de woningen starten (bv. WEGENIS_VERHARDING); leeg = onmiddellijk (aanvangsdatum bouwwerken).
--   Additief en idempotent. Draai vóór app-start.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProjectSjabloon]') AND name = N'PlanningSjabloonProjectId')
    ALTER TABLE [dbo].[ProjectSjabloon] ADD [PlanningSjabloonProjectId] INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProjectSjabloon]') AND name = N'WoningenStartNa')
    ALTER TABLE [dbo].[ProjectSjabloon] ADD [WoningenStartNa] NVARCHAR(60) NULL;
PRINT N'KLAAR: kolommen PlanningSjabloonProjectId en WoningenStartNa aanwezig op ProjectSjabloon.';
