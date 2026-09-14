-- =============================================
-- Migratie: 040_TrajectPerfIndexen
-- Datum: 2026-09-13
-- Omschrijving: Increment 7 ("Mijn mijlpalen"/"Mijn taken" op alle 5 rol-dashboards + de
--   Deadlines-pagina) draait op elke pagina-load een portfolio-brede MijlpaalService.SearchPortfolio-
--   scan die filtert op VerantwoordelijkeUserId en VerantwoordelijkeRol (zie _MijnKeypointsWidget.cshtml
--   en MijnTakenController.BepaalRollen) — die kolommen hadden nog geen index (enkel
--   ProjecttrajectId/Status/ProjecttrajectFaseId/Doeldatum uit migratie 029).
--   Strikt additief + idempotent: enkel CREATE INDEX.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Mijlpaal_VerantwoordelijkeUserId' AND object_id = OBJECT_ID(N'[dbo].[Mijlpaal]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Mijlpaal_VerantwoordelijkeUserId]
        ON [dbo].[Mijlpaal] ([VerantwoordelijkeUserId])
        WHERE [VerantwoordelijkeUserId] IS NOT NULL;
    PRINT 'Index IX_Mijlpaal_VerantwoordelijkeUserId aangemaakt.';
END
ELSE
    PRINT 'Index IX_Mijlpaal_VerantwoordelijkeUserId bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Mijlpaal_VerantwoordelijkeRol' AND object_id = OBJECT_ID(N'[dbo].[Mijlpaal]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Mijlpaal_VerantwoordelijkeRol]
        ON [dbo].[Mijlpaal] ([VerantwoordelijkeRol])
        WHERE [VerantwoordelijkeRol] IS NOT NULL;
    PRINT 'Index IX_Mijlpaal_VerantwoordelijkeRol aangemaakt.';
END
ELSE
    PRINT 'Index IX_Mijlpaal_VerantwoordelijkeRol bestaat al, overgeslagen.';

PRINT 'Migratie 040_TrajectPerfIndexen voltooid.';
