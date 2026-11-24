-- =============================================
-- 159 (24/11/2026): Project.ProjectStartDate = de startdatum van het HELE project (grondoptie, vergunning, ...), los van de
--   aanvangsdatum van de bouwwerken (Project.StartDateConstruction). Het traject rekent zijn streefdatums vanaf de projectstart;
--   de planning en de uitvoeringstermijn vanaf de aanvangsdatum van de bouwwerken.
--   Additief en idempotent. Bestaande projecten blijven leeg (dan rekent het traject vanaf de dag waarop het traject startte).
-- =============================================
IF COL_LENGTH('dbo.Project', 'ProjectStartDate') IS NULL
    ALTER TABLE [dbo].[Project] ADD [ProjectStartDate] DATE NULL;
GO
SELECT COL_LENGTH('dbo.Project', 'ProjectStartDate') AS KolomBestaat;
GO
