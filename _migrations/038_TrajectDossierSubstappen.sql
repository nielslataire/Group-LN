-- =============================================
-- Migratie: 038_TrajectDossierSubstappen
-- Datum: 2026-09-11
-- Omschrijving: Generieke checklist-stappen binnen een dossier (increment 5 — vergunningsdossier),
--   herbruikbaar voor elk DossierKind dat een stappenplan nodig heeft.
--   - ProjectDossierSubstap : één stap (Code/Naam/Volgorde/Status/Datum) binnen een ProjectDossier.
--   Strikt additief + idempotent.
--
--   DossierSubstapStatus: 0=NietGestart 1=Bezig 2=Afgerond 3=NietVanToepassing
--   ComputedBinding: 12=DossierSubstap (nieuw — mijlpaal.BronParam = substap-Code)
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjectDossierSubstap]'))
BEGIN
    CREATE TABLE [dbo].[ProjectDossierSubstap] (
        [Id]               INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectDossierId] INT             NOT NULL,
        [Code]             NVARCHAR(50)    NOT NULL,
        [Naam]             NVARCHAR(200)   NOT NULL,
        [Volgorde]         INT             NOT NULL DEFAULT 0,
        [Status]           INT             NOT NULL DEFAULT 0,
        [Datum]            DATE            NULL,
        [Opmerking]        NVARCHAR(MAX)   NULL,
        [CreatedDate]      DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedDate]     DATETIME2(7)    NULL,
        [ModifiedByUserId] NVARCHAR(128)   NULL,

        CONSTRAINT [FK_ProjectDossierSubstap_Dossier]
            FOREIGN KEY ([ProjectDossierId]) REFERENCES [dbo].[ProjectDossier]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UX_ProjectDossierSubstap_Code] UNIQUE ([ProjectDossierId], [Code])
    );

    CREATE NONCLUSTERED INDEX [IX_ProjectDossierSubstap_DossierId]
        ON [dbo].[ProjectDossierSubstap] ([ProjectDossierId]);

    PRINT 'Tabel ProjectDossierSubstap aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjectDossierSubstap bestaat al, overgeslagen.';

PRINT 'Migratie 038_TrajectDossierSubstappen voltooid.';
