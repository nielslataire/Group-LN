-- =============================================
-- Migratie: VacatureDetail
-- Datum: 2026-08-17
-- Omschrijving: Breidt het Vacature-schema uit met Opleiding/Start
--               en vier kindtabellen voor puntenlijsten
--               (takenpakket, vereisten, voordelen, sollicitatiestappen),
--               naar het patroon van BlogArtikelBlok/BlogArtikelFaq.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Vacature') AND name = 'Opleiding')
BEGIN
    ALTER TABLE [dbo].[Vacature] ADD [Opleiding] NVARCHAR(150) NULL;
    PRINT 'Kolom Vacature.Opleiding toegevoegd.';
END
ELSE
    PRINT 'Kolom Vacature.Opleiding bestaat al, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Vacature') AND name = 'Start')
BEGIN
    ALTER TABLE [dbo].[Vacature] ADD [Start] NVARCHAR(100) NULL;
    PRINT 'Kolom Vacature.Start toegevoegd.';
END
ELSE
    PRINT 'Kolom Vacature.Start bestaat al, overgeslagen.';

-- Takenpakket
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'VacatureTaak' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[VacatureTaak] (
        [Id]        INT           IDENTITY(1,1) NOT NULL,
        [VacatureId] INT          NOT NULL,
        [SortOrder] INT           NOT NULL CONSTRAINT [DF_VacatureTaak_SortOrder] DEFAULT (0),
        [Tekst]     NVARCHAR(500) NOT NULL,

        CONSTRAINT [PK_VacatureTaak] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_VacatureTaak_Vacature]
            FOREIGN KEY ([VacatureId]) REFERENCES [dbo].[Vacature] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_VacatureTaak_VacatureId] ON [dbo].[VacatureTaak] ([VacatureId]);
    PRINT 'Tabel VacatureTaak aangemaakt.';
END
ELSE
    PRINT 'Tabel VacatureTaak bestaat al, overgeslagen.';

-- Wie zoeken we (must-haves / mooi meegenomen)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'VacatureVereiste' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[VacatureVereiste] (
        [Id]        INT           IDENTITY(1,1) NOT NULL,
        [VacatureId] INT          NOT NULL,
        [SortOrder] INT           NOT NULL CONSTRAINT [DF_VacatureVereiste_SortOrder] DEFAULT (0),
        [Categorie] NVARCHAR(20)  NOT NULL CONSTRAINT [DF_VacatureVereiste_Categorie] DEFAULT ('MustHave'),
        [Tekst]     NVARCHAR(500) NOT NULL,

        CONSTRAINT [PK_VacatureVereiste] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_VacatureVereiste_Vacature]
            FOREIGN KEY ([VacatureId]) REFERENCES [dbo].[Vacature] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_VacatureVereiste_VacatureId] ON [dbo].[VacatureVereiste] ([VacatureId]);
    PRINT 'Tabel VacatureVereiste aangemaakt.';
END
ELSE
    PRINT 'Tabel VacatureVereiste bestaat al, overgeslagen.';

-- Wat bieden we
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'VacatureVoordeel' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[VacatureVoordeel] (
        [Id]        INT           IDENTITY(1,1) NOT NULL,
        [VacatureId] INT          NOT NULL,
        [SortOrder] INT           NOT NULL CONSTRAINT [DF_VacatureVoordeel_SortOrder] DEFAULT (0),
        [Tekst]     NVARCHAR(500) NOT NULL,

        CONSTRAINT [PK_VacatureVoordeel] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_VacatureVoordeel_Vacature]
            FOREIGN KEY ([VacatureId]) REFERENCES [dbo].[Vacature] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_VacatureVoordeel_VacatureId] ON [dbo].[VacatureVoordeel] ([VacatureId]);
    PRINT 'Tabel VacatureVoordeel aangemaakt.';
END
ELSE
    PRINT 'Tabel VacatureVoordeel bestaat al, overgeslagen.';

-- Stappenlijst sollicitatie
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'VacatureSollicitatieStap' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[VacatureSollicitatieStap] (
        [Id]        INT            IDENTITY(1,1) NOT NULL,
        [VacatureId] INT           NOT NULL,
        [SortOrder] INT            NOT NULL CONSTRAINT [DF_VacatureSollicitatieStap_SortOrder] DEFAULT (0),
        [Titel]     NVARCHAR(200)  NULL,
        [Tekst]     NVARCHAR(1000) NULL,

        CONSTRAINT [PK_VacatureSollicitatieStap] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_VacatureSollicitatieStap_Vacature]
            FOREIGN KEY ([VacatureId]) REFERENCES [dbo].[Vacature] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_VacatureSollicitatieStap_VacatureId] ON [dbo].[VacatureSollicitatieStap] ([VacatureId]);
    PRINT 'Tabel VacatureSollicitatieStap aangemaakt.';
END
ELSE
    PRINT 'Tabel VacatureSollicitatieStap bestaat al, overgeslagen.';
