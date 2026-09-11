-- =============================================
-- Migratie: 039_ProjectTaak
-- Datum: 2026-09-11
-- Omschrijving: Persoonlijke/interne takenlaag van de trajectopvolging ("Mijn taken", increment 6).
--   - ProjectTaak: lichte taak, optioneel gekoppeld aan Project/Unit/Mijlpaal/ProjectDossier/
--     ConstructionIssue. Niet geabsorbeerd in ConstructionIssue ("Punten") — dat draagt eigen,
--     defect-specifiek gewicht (media, plan-pins, Werfportaal). Koppeling via nullable FK's.
--   Strikt additief + idempotent.
--
--   TaakStatus:     0=Open 1=Bezig 2=Wachtend 3=Afgerond 4=Geannuleerd
--   TaakPrioriteit: 0=Laag 1=Normaal 2=Hoog 3=Dringend
--   TaakHerkomst:   0=Handmatig 1=Trigger 2=Mijlpaal 3=Dossier 4=Systeem
--   InterneRol (ToegewezenAanRol): 0=Onbekend 1=Projectleider 2=Projectontwikkelaar 3=CeoCfo
--       4=Boekhouder 5=Verkoper 6=Architect 7=Extern
--
--   Cascade-opmerking: enkel ProjectId -> Project cascadet. Unit/Mijlpaal/ProjectDossier/
--   ConstructionIssue zijn allen (indirect) ook via Project bereikbaar, dus die FK's zijn NO ACTION
--   om SQL Server Msg 1785 (meerdere cascade-paden) te vermijden — zelfde patroon als
--   FK_ProjectDossierMijlpaal_Mijlpaal in migratie 037.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjectTaak]'))
BEGIN
    CREATE TABLE [dbo].[ProjectTaak] (
        [Id]                  INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectId]           INT             NULL,
        [UnitId]              INT             NULL,
        [MijlpaalId]          INT             NULL,
        [ProjectDossierId]    INT             NULL,
        [ConstructionIssueId] INT             NULL,
        [Titel]               NVARCHAR(200)   NOT NULL,
        [Omschrijving]        NVARCHAR(MAX)   NULL,
        [Status]              INT             NOT NULL DEFAULT 0,
        [Prioriteit]          INT             NOT NULL DEFAULT 1,
        [ToegewezenAanUserId] NVARCHAR(128)   NULL,
        [ToegewezenAanRol]    INT             NULL,
        [Vervaldatum]         DATE            NULL,
        [AfgewerktOp]         DATETIME2(7)    NULL,
        [Herkomst]            INT             NOT NULL DEFAULT 0,
        [CreatedByUserId]     NVARCHAR(128)   NULL,
        [CreatedDate]         DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedDate]        DATETIME2(7)    NULL,
        [ModifiedByUserId]    NVARCHAR(128)   NULL,

        CONSTRAINT [FK_ProjectTaak_Project]
            FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectTaak_Units]
            FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]),
        CONSTRAINT [FK_ProjectTaak_Mijlpaal]
            FOREIGN KEY ([MijlpaalId]) REFERENCES [dbo].[Mijlpaal]([Id]),
        CONSTRAINT [FK_ProjectTaak_ProjectDossier]
            FOREIGN KEY ([ProjectDossierId]) REFERENCES [dbo].[ProjectDossier]([Id]),
        CONSTRAINT [FK_ProjectTaak_ConstructionIssue]
            FOREIGN KEY ([ConstructionIssueId]) REFERENCES [dbo].[ConstructionIssue]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_ProjectTaak_ProjectId] ON [dbo].[ProjectTaak] ([ProjectId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectTaak_ToegewezenAanUserId] ON [dbo].[ProjectTaak] ([ToegewezenAanUserId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectTaak_Status] ON [dbo].[ProjectTaak] ([Status]);
    CREATE NONCLUSTERED INDEX [IX_ProjectTaak_Vervaldatum] ON [dbo].[ProjectTaak] ([Vervaldatum]);
    CREATE NONCLUSTERED INDEX [IX_ProjectTaak_MijlpaalId] ON [dbo].[ProjectTaak] ([MijlpaalId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectTaak_ProjectDossierId] ON [dbo].[ProjectTaak] ([ProjectDossierId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectTaak_ConstructionIssueId] ON [dbo].[ProjectTaak] ([ConstructionIssueId]);

    PRINT 'Tabel ProjectTaak aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjectTaak bestaat al, overgeslagen.';

PRINT 'Migratie 039_ProjectTaak voltooid.';
