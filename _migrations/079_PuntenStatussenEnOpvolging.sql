-- =============================================
-- Migratie: 079_PuntenStatussenEnOpvolging
-- Datum: 2026-10-08
-- Omschrijving: Punten (design-handoff punt 40/41): nieuwe statusreeks + "In de wacht" + puntnummer per project.
--   ConstructionIssue.PuntNr          : P-0xx per project (bestaande rijen: volgorde van Id).
--   ConstructionIssue.OnHoldSince     : sinds wanneer het punt in de wacht staat (de deadline loopt dan niet door).
--   ConstructionIssue.OnHoldReason    : reden van de wacht.
--   ConstructionIssue.FollowUpDate    : opvolgdatum (herinnering) tijdens de wacht.
--   ConstructionIssueHistory.IsInternal : bericht enkel voor intern gebruik (niet zichtbaar voor de aannemer).
--   Statuswaarden (ConstructionIssueStatus): nieuw 8 Concept, 9 Ter goedkeuring, 10 Doorgestuurd, 11 Gemeld uitgevoerd,
--   12 In de wacht. Bestaande rijen worden eenmalig omgezet: Open/Toegewezen/Gepland/Heropend -> Doorgestuurd (ze waren al
--   zichtbaar voor de aannemer), Klaar voor controle -> Gemeld uitgevoerd, Opgelost -> Afgesloten. 5 en 6 blijven.
--   Strikt additief (kolommen) + idempotent: de statusomzetting loopt enkel mee met het toevoegen van PuntNr.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConstructionIssue') AND name = 'PuntNr')
BEGIN
    ALTER TABLE [dbo].[ConstructionIssue] ADD [PuntNr] INT NULL, [OnHoldSince] DATETIME2(0) NULL, [OnHoldReason] NVARCHAR(300) NULL, [FollowUpDate] DATE NULL;
    PRINT 'Kolommen PuntNr, OnHoldSince, OnHoldReason, FollowUpDate toegevoegd aan ConstructionIssue.';
END
ELSE PRINT 'Kolommen op ConstructionIssue bestaan al, overgeslagen.';
GO
IF EXISTS (SELECT 1 FROM dbo.ConstructionIssue WHERE PuntNr IS NULL)
BEGIN
    ;WITH n AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY ProjectId ORDER BY Id) AS rn FROM dbo.ConstructionIssue)
    UPDATE i SET PuntNr = n.rn FROM dbo.ConstructionIssue i JOIN n ON n.Id = i.Id WHERE i.PuntNr IS NULL;
    PRINT 'PuntNr ingevuld.';
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ConstructionIssueHistory') AND name = 'IsInternal')
BEGIN
    ALTER TABLE [dbo].[ConstructionIssueHistory] ADD [IsInternal] BIT NOT NULL CONSTRAINT DF_ConstructionIssueHistory_IsInternal DEFAULT 0;
    PRINT 'Kolom IsInternal toegevoegd aan ConstructionIssueHistory.';

    -- Eenmalige statusomzetting (enkel in deze run, zolang er nog geen nieuwe statussen bestaan).
    IF NOT EXISTS (SELECT 1 FROM dbo.ConstructionIssue WHERE Status >= 8)
    BEGIN
        UPDATE dbo.ConstructionIssue SET Status = 10 WHERE Status IN (0, 1, 2, 7);
        UPDATE dbo.ConstructionIssue SET Status = 11 WHERE Status = 3;
        UPDATE dbo.ConstructionIssue SET Status = 5  WHERE Status = 4;
        PRINT 'Bestaande punten naar de nieuwe statusreeks omgezet.';
    END
END
ELSE PRINT 'Kolom IsInternal bestaat al, overgeslagen.';
GO
