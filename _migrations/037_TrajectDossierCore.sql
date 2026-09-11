-- =============================================
-- Migratie: 037_TrajectDossierCore
-- Datum: 2026-09-11
-- Omschrijving: Generieke dossier-/workstreamlaag van de trajectopvolging (increment 4).
--   - ProjectDossier            : generiek dossier (DossierKind + kleine statuslevenscyclus + DataJson)
--   - ProjectDossierGebeurtenis : opvolgingstijdlijn
--   - ProjectDossierDocument    : koppeling naar ProjectDocs (of losse Storage-FileId)
--   - ProjectDossierMijlpaal    : M:N-koppeling dossier <-> mijlpalen die het stuurt
--   - ProjectNutsAansluiting    : getypeerde companion voor het lanceringstype (nutsaansluiting)
--   - FK Mijlpaal.DossierId -> ProjectDossier (kolom bestaat al sinds 029, kreeg nog geen FK)
--   Strikt additief + idempotent. Geen GO-batchscheiders: de FK op Mijlpaal.DossierId draait
--   via EXEC zodat hij pas geparsed wordt nadat ProjectDossier zeker bestaat.
--
--   DossierKind: 0=Vrij 1=NutsAansluiting 2=Omgevingsvergunning 3=Grondverwerving 4=Akte
--       5=Verzekering 6=VerkoopDossier 7=FacturatieMijlpaal 8=Nutsafrekening
--   DossierStatus: 0=Nieuw 1=Aangevraagd 2=InBehandeling 3=Afgehandeld 4=Geannuleerd
--   NutsType: 0=Elektriciteit 1=Gas 2=Water 3=Riolering 4=Telecom 5=Overig
-- =============================================

------------------------------------------------------------
-- 1. ProjectDossier
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjectDossier]'))
BEGIN
    CREATE TABLE [dbo].[ProjectDossier] (
        [Id]                          INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectId]                   INT             NOT NULL,
        [UnitId]                      INT             NULL,
        [DossierKind]                 INT             NOT NULL DEFAULT 0,
        [Titel]                       NVARCHAR(200)   NOT NULL,
        [Referentie]                  NVARCHAR(100)   NULL,
        [Status]                      INT             NOT NULL DEFAULT 0,
        [VerantwoordelijkePartijType] INT             NULL,
        [VerantwoordelijkePartijId]   INT             NULL,
        [VerantwoordelijkeUserId]     NVARCHAR(128)   NULL,
        [ExterneContactNaam]          NVARCHAR(150)   NULL,
        [ExterneContactEmail]         NVARCHAR(254)   NULL,
        [AanvraagDatum]               DATE            NULL,
        [VerwachteAfhandelingDatum]   DATE            NULL,
        [AfgehandeldDatum]            DATE            NULL,
        [Bedrag]                      DECIMAL(19,4)   NULL,
        [Omschrijving]                NVARCHAR(MAX)   NULL,
        [DataJson]                    NVARCHAR(MAX)   NULL,
        [CreatedDate]                 DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId]             NVARCHAR(128)   NULL,
        [ModifiedDate]                DATETIME2(7)    NULL,
        [ModifiedByUserId]            NVARCHAR(128)   NULL,

        CONSTRAINT [FK_ProjectDossier_Project]
            FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectDossier_Units]
            FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_ProjectDossier_ProjectId]
        ON [dbo].[ProjectDossier] ([ProjectId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectDossier_Project_Kind]
        ON [dbo].[ProjectDossier] ([ProjectId], [DossierKind]);
    CREATE NONCLUSTERED INDEX [IX_ProjectDossier_UnitId]
        ON [dbo].[ProjectDossier] ([UnitId]);

    PRINT 'Tabel ProjectDossier aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjectDossier bestaat al, overgeslagen.';

------------------------------------------------------------
-- 2. ProjectDossierGebeurtenis
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjectDossierGebeurtenis]'))
BEGIN
    CREATE TABLE [dbo].[ProjectDossierGebeurtenis] (
        [Id]               INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectDossierId] INT             NOT NULL,
        [Datum]            DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [Type]             INT             NOT NULL DEFAULT 0,
        [Titel]            NVARCHAR(200)   NULL,
        [Tekst]            NVARCHAR(MAX)   NULL,
        [UserId]           NVARCHAR(128)   NULL,
        [CreatedDate]      DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [FK_ProjectDossierGebeurtenis_Dossier]
            FOREIGN KEY ([ProjectDossierId]) REFERENCES [dbo].[ProjectDossier]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_ProjectDossierGebeurtenis_DossierId]
        ON [dbo].[ProjectDossierGebeurtenis] ([ProjectDossierId]);

    PRINT 'Tabel ProjectDossierGebeurtenis aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjectDossierGebeurtenis bestaat al, overgeslagen.';

------------------------------------------------------------
-- 3. ProjectDossierDocument
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjectDossierDocument]'))
BEGIN
    CREATE TABLE [dbo].[ProjectDossierDocument] (
        [Id]               INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectDossierId] INT             NOT NULL,
        [ProjectDocId]     INT             NULL,
        [FileId]           NVARCHAR(200)   NULL,
        [Naam]             NVARCHAR(200)   NULL,
        [DocType]          INT             NULL,
        [CreatedDate]      DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId]  NVARCHAR(128)   NULL,

        CONSTRAINT [FK_ProjectDossierDocument_Dossier]
            FOREIGN KEY ([ProjectDossierId]) REFERENCES [dbo].[ProjectDossier]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectDossierDocument_ProjectDoc]
            FOREIGN KEY ([ProjectDocId]) REFERENCES [dbo].[ProjectDocs]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_ProjectDossierDocument_DossierId]
        ON [dbo].[ProjectDossierDocument] ([ProjectDossierId]);

    PRINT 'Tabel ProjectDossierDocument aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjectDossierDocument bestaat al, overgeslagen.';

------------------------------------------------------------
-- 4. ProjectDossierMijlpaal
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjectDossierMijlpaal]'))
BEGIN
    CREATE TABLE [dbo].[ProjectDossierMijlpaal] (
        [Id]               INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectDossierId] INT NOT NULL,
        [MijlpaalId]       INT NOT NULL,

        CONSTRAINT [FK_ProjectDossierMijlpaal_Dossier]
            FOREIGN KEY ([ProjectDossierId]) REFERENCES [dbo].[ProjectDossier]([Id]) ON DELETE CASCADE,
        -- NO ACTION: Mijlpaal cascadet al via Projecttraject -> Project; een tweede CASCADE-pad
        -- hierheen (Project -> ProjectDossier -> ProjectDossierMijlpaal) geeft Msg 1785.
        CONSTRAINT [FK_ProjectDossierMijlpaal_Mijlpaal]
            FOREIGN KEY ([MijlpaalId]) REFERENCES [dbo].[Mijlpaal]([Id]),
        CONSTRAINT [UX_ProjectDossierMijlpaal] UNIQUE ([ProjectDossierId], [MijlpaalId])
    );

    PRINT 'Tabel ProjectDossierMijlpaal aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjectDossierMijlpaal bestaat al, overgeslagen.';

------------------------------------------------------------
-- 5. ProjectNutsAansluiting
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjectNutsAansluiting]'))
BEGIN
    CREATE TABLE [dbo].[ProjectNutsAansluiting] (
        [Id]                     INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [ProjectDossierId]       INT             NOT NULL,
        [UnitId]                 INT             NULL,
        [NutsType]               INT             NOT NULL DEFAULT 0,
        [NetbeheerderCompanyId]  INT             NULL,
        [Ean]                    NVARCHAR(50)    NULL,
        [Meternummer]            NVARCHAR(50)    NULL,
        [GevraagdVermogen]       NVARCHAR(50)    NULL,
        [AanvraagVerstuurdOp]    DATE            NULL,
        [KeuringDocId]           INT             NULL,
        [AansluitkostRaming]     DECIMAL(19,4)   NULL,
        [AansluitkostDefinitief] DECIMAL(19,4)   NULL,
        [AfrekeningLink]         INT             NULL,

        CONSTRAINT [FK_ProjectNutsAansluiting_Dossier]
            FOREIGN KEY ([ProjectDossierId]) REFERENCES [dbo].[ProjectDossier]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UX_ProjectNutsAansluiting_Dossier] UNIQUE ([ProjectDossierId]),
        CONSTRAINT [FK_ProjectNutsAansluiting_Units]
            FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]),
        CONSTRAINT [FK_ProjectNutsAansluiting_Netbeheerder]
            FOREIGN KEY ([NetbeheerderCompanyId]) REFERENCES [dbo].[CompanyInfo]([CompanyId]),
        CONSTRAINT [FK_ProjectNutsAansluiting_KeuringDoc]
            FOREIGN KEY ([KeuringDocId]) REFERENCES [dbo].[ProjectDocs]([Id]),
        CONSTRAINT [FK_ProjectNutsAansluiting_Afrekening]
            FOREIGN KEY ([AfrekeningLink]) REFERENCES [dbo].[ConnectionSettlement]([Id])
    );

    PRINT 'Tabel ProjectNutsAansluiting aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjectNutsAansluiting bestaat al, overgeslagen.';

------------------------------------------------------------
-- 6. FK Mijlpaal.DossierId -> ProjectDossier (kolom bestaat al sinds 029)
------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[FK_Mijlpaal_ProjectDossier]', 'F') IS NULL
   AND COL_LENGTH('dbo.Mijlpaal', 'DossierId') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[ProjectDossier]') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Mijlpaal] WITH CHECK
        ADD CONSTRAINT [FK_Mijlpaal_ProjectDossier]
        FOREIGN KEY ([DossierId]) REFERENCES [dbo].[ProjectDossier]([Id]);');
    PRINT 'FK Mijlpaal.DossierId -> ProjectDossier toegevoegd.';
END
ELSE
    PRINT 'FK Mijlpaal.DossierId bestaat al of tabellen ontbreken, overgeslagen.';

PRINT 'Migratie 037_TrajectDossierCore voltooid.';
