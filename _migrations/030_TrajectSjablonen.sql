-- =============================================
-- Migratie: 030_TrajectSjablonen
-- Datum: 2026-09-10
-- Omschrijving: Sjabloon-laag van de trajectopvolging + FK vanaf
--   Projecttraject.TrajectSjabloonId. Seedt twee standaardsjablonen
--   (Woonproject / Commercieel) met fases + representatieve mijlpalen.
--   Strikt additief + idempotent. Bindings/triggers volgen in latere
--   increments (kolommen BronBinding/BronParam blijven hier NULL).
--
--   Rol-codes (InterneRol): 1=Projectleider 2=Projectontwikkelaar
--       3=CeoCfo 4=Boekhouder 5=Verkoper 6=Architect 7=Extern
--   MijlpaalType: 0=Algemeen 1=Administratief 2=Vergunning 3=Werf
--       4=Verkoop 5=Financieel 6=Keuring 7=Oplevering
-- =============================================

------------------------------------------------------------
-- 1. TrajectSjabloon
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[TrajectSjabloon]'))
BEGIN
    CREATE TABLE [dbo].[TrajectSjabloon] (
        [Id]               INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [Naam]             NVARCHAR(200)   NOT NULL,
        [ProjectType]      INT             NULL,
        [IsStandaard]      BIT             NOT NULL DEFAULT 0,
        [IsActief]         BIT             NOT NULL DEFAULT 1,
        [Omschrijving]     NVARCHAR(MAX)   NULL,
        [CreatedDate]      DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId]  NVARCHAR(128)   NULL,
        [ModifiedDate]     DATETIME2(7)    NULL,
        [ModifiedByUserId] NVARCHAR(128)   NULL
    );
    PRINT 'Tabel TrajectSjabloon aangemaakt.';
END
ELSE
    PRINT 'Tabel TrajectSjabloon bestaat al, overgeslagen.';

------------------------------------------------------------
-- 2. TrajectSjabloonFase
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[TrajectSjabloonFase]'))
BEGIN
    CREATE TABLE [dbo].[TrajectSjabloonFase] (
        [Id]                       INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [TrajectSjabloonId]        INT             NOT NULL,
        [Naam]                     NVARCHAR(200)   NOT NULL,
        [Code]                     NVARCHAR(50)    NOT NULL,
        [Volgorde]                 INT             NOT NULL DEFAULT 0,
        [KleurCode]                NVARCHAR(20)    NULL,
        [StandaardProjectStatusId] INT            NULL,

        CONSTRAINT [FK_TrajectSjabloonFase_Sjabloon]
            FOREIGN KEY ([TrajectSjabloonId]) REFERENCES [dbo].[TrajectSjabloon]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_TrajectSjabloonFase_SjabloonId]
        ON [dbo].[TrajectSjabloonFase] ([TrajectSjabloonId]);

    PRINT 'Tabel TrajectSjabloonFase aangemaakt.';
END
ELSE
    PRINT 'Tabel TrajectSjabloonFase bestaat al, overgeslagen.';

------------------------------------------------------------
-- 3. TrajectSjabloonMijlpaal
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[TrajectSjabloonMijlpaal]'))
BEGIN
    CREATE TABLE [dbo].[TrajectSjabloonMijlpaal] (
        [Id]                    INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [TrajectSjabloonFaseId] INT             NOT NULL,
        [Naam]                  NVARCHAR(200)   NOT NULL,
        [Code]                  NVARCHAR(50)    NOT NULL,
        [Volgorde]              INT             NOT NULL DEFAULT 0,
        [MijlpaalType]          INT             NOT NULL DEFAULT 0,
        [VerantwoordelijkeRol]  INT             NULL,
        [DoeldatumAnkerCode]    NVARCHAR(50)    NULL,
        [DoeldatumOffsetDagen]  INT             NULL,
        [IsVerplicht]           BIT             NOT NULL DEFAULT 1,
        [BronBinding]           INT             NULL,
        [BronParam]             NVARCHAR(100)   NULL,
        [DossierKind]           INT             NULL,
        [Omschrijving]          NVARCHAR(MAX)   NULL,

        CONSTRAINT [FK_TrajectSjabloonMijlpaal_Fase]
            FOREIGN KEY ([TrajectSjabloonFaseId]) REFERENCES [dbo].[TrajectSjabloonFase]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_TrajectSjabloonMijlpaal_FaseId]
        ON [dbo].[TrajectSjabloonMijlpaal] ([TrajectSjabloonFaseId]);

    PRINT 'Tabel TrajectSjabloonMijlpaal aangemaakt.';
END
ELSE
    PRINT 'Tabel TrajectSjabloonMijlpaal bestaat al, overgeslagen.';

------------------------------------------------------------
-- 4. TrajectSjabloonMijlpaalAfhankelijkheid
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[TrajectSjabloonMijlpaalAfhankelijkheid]'))
BEGIN
    CREATE TABLE [dbo].[TrajectSjabloonMijlpaalAfhankelijkheid] (
        [Id]                INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [MijlpaalId]        INT NOT NULL,
        [VereistMijlpaalId] INT NOT NULL,
        [Type]              INT NOT NULL DEFAULT 0,

        CONSTRAINT [FK_SjabloonMijlpaalAfh_Mijlpaal]
            FOREIGN KEY ([MijlpaalId]) REFERENCES [dbo].[TrajectSjabloonMijlpaal]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SjabloonMijlpaalAfh_Vereist]
            FOREIGN KEY ([VereistMijlpaalId]) REFERENCES [dbo].[TrajectSjabloonMijlpaal]([Id]),
        CONSTRAINT [UX_SjabloonMijlpaalAfh] UNIQUE ([MijlpaalId], [VereistMijlpaalId])
    );

    PRINT 'Tabel TrajectSjabloonMijlpaalAfhankelijkheid aangemaakt.';
END
ELSE
    PRINT 'Tabel TrajectSjabloonMijlpaalAfhankelijkheid bestaat al, overgeslagen.';

------------------------------------------------------------
-- 5. FK Projecttraject.TrajectSjabloonId -> TrajectSjabloon(Id)
------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[FK_Projecttraject_Sjabloon]', 'F') IS NULL
   AND OBJECT_ID(N'[dbo].[Projecttraject]') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[TrajectSjabloon]') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Projecttraject] WITH CHECK
        ADD CONSTRAINT [FK_Projecttraject_Sjabloon]
        FOREIGN KEY ([TrajectSjabloonId]) REFERENCES [dbo].[TrajectSjabloon]([Id]);
    PRINT 'FK Projecttraject.TrajectSjabloonId toegevoegd.';
END
ELSE
    PRINT 'FK Projecttraject.TrajectSjabloonId bestaat al of tabellen ontbreken, overgeslagen.';

------------------------------------------------------------
-- 6. Seed: standaardsjabloon "Woonproject"
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloon] WHERE [IsStandaard] = 1 AND [ProjectType] = 1)
BEGIN
    DECLARE @wp INT;

    INSERT INTO [dbo].[TrajectSjabloon] ([Naam], [ProjectType], [IsStandaard], [IsActief], [Omschrijving])
    VALUES (N'Standaardtraject Woonproject', 1, 1, 1,
            N'Volledige levenscyclus van grondaankoop tot nazorg voor een residentieel ontwikkelingsproject.');
    SET @wp = SCOPE_IDENTITY();

    INSERT INTO [dbo].[TrajectSjabloonFase] ([TrajectSjabloonId], [Naam], [Code], [Volgorde], [StandaardProjectStatusId])
    VALUES
        (@wp, N'Aankoop grond',        N'AANKOOP',     10, NULL),
        (@wp, N'Ontwerp',              N'ONTWERP',     20, 3),
        (@wp, N'Omgevingsvergunning',  N'VERGUNNING',  30, 4),
        (@wp, N'Voorverkoop',          N'VOORVERKOOP', 40, 5),
        (@wp, N'Uitvoering werf',      N'UITVOERING',  50, 2),
        (@wp, N'Oplevering',           N'OPLEVERING',  60, NULL),
        (@wp, N'Nazorg & afsluiting',  N'NAZORG',      70, 1);

    ;WITH f AS (SELECT [Id], [Code] FROM [dbo].[TrajectSjabloonFase] WHERE [TrajectSjabloonId] = @wp)
    INSERT INTO [dbo].[TrajectSjabloonMijlpaal]
        ([TrajectSjabloonFaseId], [Naam], [Code], [Volgorde], [MijlpaalType], [VerantwoordelijkeRol], [DoeldatumAnkerCode], [DoeldatumOffsetDagen], [IsVerplicht])
    SELECT f.[Id], v.[Naam], v.[Code], v.[Volgorde], v.[MijlpaalType], v.[Rol], v.[Anker], v.[Offset], v.[Verplicht]
    FROM f
    JOIN (VALUES
        (N'AANKOOP',     N'Compromis grond getekend',                      N'AANKOOP_COMPROMIS',        10, 1, 2, N'PROJECT_CREATED',        0,    1),
        (N'AANKOOP',     N'Verlijden akte grond',                          N'AANKOOP_AKTE',             20, 1, 2, N'AANKOOP_COMPROMIS',      120,  1),
        (N'ONTWERP',     N'Architect aangesteld',                          N'ARCHITECT_AANGESTELD',     10, 1, 2, N'AANKOOP_AKTE',          30,   1),
        (N'ONTWERP',     N'Voorontwerp goedgekeurd',                       N'VOORONTWERP_GOEDGEKEURD',  20, 0, 2, N'ARCHITECT_AANGESTELD',  90,   1),
        (N'ONTWERP',     N'Aanvraagdossier vergunning klaar',              N'VERGUNNING_DOSSIER_KLAAR', 30, 1, 6, N'VOORONTWERP_GOEDGEKEURD',60,  1),
        (N'VERGUNNING',  N'Omgevingsvergunning ingediend',                 N'VERGUNNING_INGEDIEND',     10, 2, 2, N'VERGUNNING_DOSSIER_KLAAR',7,  1),
        (N'VERGUNNING',  N'Dossier volledig en ontvankelijk verklaard',    N'VERGUNNING_VOLLEDIG',      20, 2, 7, N'VERGUNNING_INGEDIEND',  30,   1),
        (N'VERGUNNING',  N'Openbaar onderzoek afgerond',                   N'OPENBAAR_ONDERZOEK',       30, 2, 7, N'VERGUNNING_VOLLEDIG',   30,   1),
        (N'VERGUNNING',  N'Vergunning verleend (college van B&W)',         N'VERGUNNING_VERLEEND',      40, 2, 2, N'OPENBAAR_ONDERZOEK',    45,   1),
        (N'VERGUNNING',  N'Vergunning definitief (beroepstermijn voorbij)',N'VERGUNNING_DEFINITIEF',    50, 2, 2, N'VERGUNNING_VERLEEND',   35,   1),
        (N'VOORVERKOOP', N'Verkoop opgestart',                             N'VERKOOP_GESTART',          10, 4, 5, N'VERGUNNING_VERLEEND',   0,    1),
        (N'VOORVERKOOP', N'Voorverkoopdrempel financiering behaald',       N'VOORVERKOOP_DREMPEL',      20, 5, 3, N'VERKOOP_GESTART',       120,  1),
        (N'UITVOERING',  N'Werfmelding ingediend',                         N'WERFMELDING',              10, 3, 1, N'VERGUNNING_DEFINITIEF', 14,   1),
        (N'UITVOERING',  N'Start der werken',                              N'WERF_START',               20, 3, 1, N'WERFMELDING',           14,   1),
        (N'UITVOERING',  N'Ruwbouw wind- en waterdicht',                   N'WIND_WATERDICHT',          30, 3, 1, N'WERF_START',            120,  1),
        (N'UITVOERING',  N'Technieken afgewerkt',                          N'TECHNIEKEN_AF',            40, 3, 1, N'WIND_WATERDICHT',       90,   1),
        (N'UITVOERING',  N'Afwerking voltooid',                            N'AFWERKING_AF',             50, 3, 1, N'TECHNIEKEN_AF',         60,   1),
        (N'OPLEVERING',  N'Keuring elektriciteit',                         N'KEURING_ELEK',             10, 6, 1, N'AFWERKING_AF',          0,    1),
        (N'OPLEVERING',  N'Keuring water',                                 N'KEURING_WATER',            20, 6, 1, N'AFWERKING_AF',          0,    1),
        (N'OPLEVERING',  N'Keuring gas',                                   N'KEURING_GAS',              30, 6, 1, N'AFWERKING_AF',          0,    0),
        (N'OPLEVERING',  N'Keuring riolering',                             N'KEURING_RIOOL',            40, 6, 1, N'AFWERKING_AF',          0,    1),
        (N'OPLEVERING',  N'Voorlopige oplevering',                         N'OPLEVERING_VOORLOPIG',     50, 7, 1, N'AFWERKING_AF',          14,   1),
        (N'OPLEVERING',  N'Definitieve oplevering',                        N'OPLEVERING_DEFINITIEF',    60, 7, 1, N'OPLEVERING_VOORLOPIG',  365,  1),
        (N'NAZORG',      N'Afgifte postinterventiedossier (PID)',          N'PID_AFGIFTE',              10, 1, 1, N'OPLEVERING_VOORLOPIG',  30,   1),
        (N'NAZORG',      N'Eindafrekening nutsvoorzieningen',              N'NUTS_AFREKENING',          20, 5, 4, N'OPLEVERING_VOORLOPIG',  90,   1),
        (N'NAZORG',      N'Vrijgave bankwaarborgen aannemers',             N'WAARBORG_VRIJGAVE',        30, 5, 4, N'OPLEVERING_DEFINITIEF', 0,    1),
        (N'NAZORG',      N'Project administratief afgesloten',             N'PROJECT_AFGESLOTEN',       40, 0, 2, N'OPLEVERING_DEFINITIEF', 60,   1)
    ) AS v([FaseCode], [Naam], [Code], [Volgorde], [MijlpaalType], [Rol], [Anker], [Offset], [Verplicht])
      ON v.[FaseCode] = f.[Code];

    PRINT 'Seed standaardsjabloon Woonproject aangemaakt.';
END
ELSE
    PRINT 'Standaardsjabloon Woonproject bestaat al, overgeslagen.';

------------------------------------------------------------
-- 7. Seed: standaardsjabloon "Commercieel"
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloon] WHERE [IsStandaard] = 1 AND [ProjectType] = 2)
BEGIN
    DECLARE @co INT;

    INSERT INTO [dbo].[TrajectSjabloon] ([Naam], [ProjectType], [IsStandaard], [IsActief], [Omschrijving])
    VALUES (N'Standaardtraject Commercieel', 2, 1, 1,
            N'Levenscyclus voor een commercieel ontwikkelingsproject.');
    SET @co = SCOPE_IDENTITY();

    INSERT INTO [dbo].[TrajectSjabloonFase] ([TrajectSjabloonId], [Naam], [Code], [Volgorde], [StandaardProjectStatusId])
    VALUES
        (@co, N'Aankoop',             N'AANKOOP',    10, NULL),
        (@co, N'Ontwerp',             N'ONTWERP',    20, 3),
        (@co, N'Omgevingsvergunning', N'VERGUNNING', 30, 4),
        (@co, N'Uitvoering werf',     N'UITVOERING', 40, 2),
        (@co, N'Oplevering',          N'OPLEVERING', 50, NULL);

    ;WITH f AS (SELECT [Id], [Code] FROM [dbo].[TrajectSjabloonFase] WHERE [TrajectSjabloonId] = @co)
    INSERT INTO [dbo].[TrajectSjabloonMijlpaal]
        ([TrajectSjabloonFaseId], [Naam], [Code], [Volgorde], [MijlpaalType], [VerantwoordelijkeRol], [DoeldatumAnkerCode], [DoeldatumOffsetDagen], [IsVerplicht])
    SELECT f.[Id], v.[Naam], v.[Code], v.[Volgorde], v.[MijlpaalType], v.[Rol], v.[Anker], v.[Offset], v.[Verplicht]
    FROM f
    JOIN (VALUES
        (N'AANKOOP',    N'Verlijden akte grond',                          N'AANKOOP_AKTE',          10, 1, 2, N'PROJECT_CREATED',       90,  1),
        (N'ONTWERP',    N'Aanvraagdossier vergunning klaar',              N'VERGUNNING_DOSSIER_KLAAR',20,1, 6, N'AANKOOP_AKTE',          120, 1),
        (N'VERGUNNING', N'Omgevingsvergunning ingediend',                 N'VERGUNNING_INGEDIEND',   10, 2, 2, N'VERGUNNING_DOSSIER_KLAAR',7, 1),
        (N'VERGUNNING', N'Vergunning verleend',                           N'VERGUNNING_VERLEEND',    20, 2, 2, N'VERGUNNING_INGEDIEND',   105, 1),
        (N'VERGUNNING', N'Vergunning definitief',                         N'VERGUNNING_DEFINITIEF',  30, 2, 2, N'VERGUNNING_VERLEEND',    35,  1),
        (N'UITVOERING', N'Werfmelding ingediend',                         N'WERFMELDING',            10, 3, 1, N'VERGUNNING_DEFINITIEF',  14,  1),
        (N'UITVOERING', N'Start der werken',                              N'WERF_START',             20, 3, 1, N'WERFMELDING',            14,  1),
        (N'UITVOERING', N'Ruwbouw wind- en waterdicht',                   N'WIND_WATERDICHT',        30, 3, 1, N'WERF_START',             150, 1),
        (N'OPLEVERING', N'Voorlopige oplevering',                         N'OPLEVERING_VOORLOPIG',   10, 7, 1, N'WIND_WATERDICHT',        120, 1),
        (N'OPLEVERING', N'Definitieve oplevering',                        N'OPLEVERING_DEFINITIEF',  20, 7, 1, N'OPLEVERING_VOORLOPIG',   365, 1)
    ) AS v([FaseCode], [Naam], [Code], [Volgorde], [MijlpaalType], [Rol], [Anker], [Offset], [Verplicht])
      ON v.[FaseCode] = f.[Code];

    PRINT 'Seed standaardsjabloon Commercieel aangemaakt.';
END
ELSE
    PRINT 'Standaardsjabloon Commercieel bestaat al, overgeslagen.';

PRINT 'Migratie 030_TrajectSjablonen voltooid.';
