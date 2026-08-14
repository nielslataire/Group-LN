-- =============================================
-- Migratie: Vacature
-- Datum: 2026-08-14
-- Omschrijving: Voegt de Vacature-tabel toe voor de publieke
--               vacatures-pagina van de website. Bewust een vlak,
--               beperkt schema (geen content-blokken zoals bij Blog) —
--               kan later verder uitgebreid worden.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Vacature' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Vacature] (
        [Id]                INT            IDENTITY(1,1) NOT NULL,
        [Titel]             NVARCHAR(250)  NOT NULL,
        [Slug]              NVARCHAR(300)  NOT NULL,
        [Categorie]         NVARCHAR(100)  NULL,
        [Locatie]           NVARCHAR(100)  NULL,
        [Dienstverband]     NVARCHAR(100)  NULL,
        [KorteBeschrijving] NVARCHAR(500)  NULL,
        [Beschrijving]      NVARCHAR(MAX)  NULL,
        [IsGepubliceerd]    BIT            NOT NULL CONSTRAINT [DF_Vacature_IsGepubliceerd] DEFAULT (0),
        [SortOrder]         INT            NOT NULL CONSTRAINT [DF_Vacature_SortOrder] DEFAULT (0),
        [AangemaaktOp]      DATETIME2      NOT NULL CONSTRAINT [DF_Vacature_AangemaaktOp] DEFAULT (GETDATE()),
        [GewijzigdOp]       DATETIME2      NOT NULL CONSTRAINT [DF_Vacature_GewijzigdOp]  DEFAULT (GETDATE()),

        CONSTRAINT [PK_Vacature]
            PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX [UX_Vacature_Slug]
        ON [dbo].[Vacature] ([Slug]);

    PRINT 'Tabel Vacature aangemaakt.';
END
ELSE
    PRINT 'Tabel Vacature bestaat al, overgeslagen.';
