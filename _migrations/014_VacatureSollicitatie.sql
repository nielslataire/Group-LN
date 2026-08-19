-- =============================================
-- Migratie: VacatureSollicitatie
-- Datum: 2026-08-18
-- Omschrijving: Tabel voor sollicitaties via het cv-uploadformulier op de
--               vacature-detailpagina. Het cv-bestand wordt als VARBINARY(MAX)
--               in de databank bewaard (geen gedeelde bestandsopslag tussen
--               WWWCOPRO (klassieke ADO.NET) en CPMCore (EF Core)).
--               VacatureId is NULLABLE met ON DELETE SET NULL zodat sollicitaties
--               bewaard blijven ook als de vacature later verwijderd wordt —
--               VacatureTitelSnapshot houdt de titel op moment van solliciteren vast.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'VacatureSollicitatie' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[VacatureSollicitatie] (
        [Id]                    INT            IDENTITY(1,1) NOT NULL,
        [VacatureId]            INT            NULL,
        [VacatureTitelSnapshot] NVARCHAR(250)  NOT NULL,
        [Voornaam]              NVARCHAR(100)  NOT NULL,
        [Achternaam]            NVARCHAR(100)  NOT NULL,
        [Email]                 NVARCHAR(200)  NOT NULL,
        [Telefoon]              NVARCHAR(50)   NULL,
        [Motivatie]             NVARCHAR(MAX)  NULL,
        [CvBestandsnaam]        NVARCHAR(260)  NOT NULL,
        [CvBestandType]         NVARCHAR(100)  NOT NULL,
        [CvBestand]             VARBINARY(MAX) NOT NULL,
        [IsGelezen]             BIT            NOT NULL CONSTRAINT [DF_VacatureSollicitatie_IsGelezen] DEFAULT (0),
        [AangemaaktOp]          DATETIME2      NOT NULL CONSTRAINT [DF_VacatureSollicitatie_AangemaaktOp] DEFAULT (GETDATE()),

        CONSTRAINT [PK_VacatureSollicitatie] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_VacatureSollicitatie_Vacature]
            FOREIGN KEY ([VacatureId]) REFERENCES [dbo].[Vacature] ([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_VacatureSollicitatie_VacatureId] ON [dbo].[VacatureSollicitatie] ([VacatureId]);

    PRINT 'Tabel VacatureSollicitatie aangemaakt.';
END
ELSE
    PRINT 'Tabel VacatureSollicitatie bestaat al, overgeslagen.';
