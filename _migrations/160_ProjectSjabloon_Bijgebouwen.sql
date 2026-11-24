-- =============================================
-- 160 (24/11/2026): ProjectSjabloon.SchijfSjabloonBijgebouwId = het schijvensjabloon voor betalingsgroepen met enkel parkings en bergingen.
--   Een groep waarvan alle eenheden een parking of berging zijn (type buiten de typegroepen woning en commercieel) krijgt bij het toepassen
--   van het projectsjabloon dit sjabloon; alle andere groepen krijgen het gewone schijvensjabloon. Leeg = alle groepen krijgen het gewone sjabloon.
--   Additief en idempotent. Vereist 156.
-- =============================================
IF COL_LENGTH('dbo.ProjectSjabloon', 'SchijfSjabloonBijgebouwId') IS NULL
    ALTER TABLE [dbo].[ProjectSjabloon] ADD [SchijfSjabloonBijgebouwId] INT NULL;
GO
SELECT COL_LENGTH('dbo.ProjectSjabloon', 'SchijfSjabloonBijgebouwId') AS KolomBestaat;
GO
