-- =============================================
-- Migratie: Projectvoortgang berekening
-- Datum: 2026-03-30
-- Omschrijving: Maakt tabel ProjectVoortgang aan
--               voor het opslaan van berekende
--               fysieke/financiële/contractuele
--               voortgang per project.
--               Fase: 0=Opstart, 1=InVoorbereiding,
--                     2=InUitvoering, 3=Eindfase,
--                     4=Afgewerkt
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE object_id = OBJECT_ID(N'[dbo].[ProjectVoortgang]')
)
BEGIN
    CREATE TABLE [dbo].[ProjectVoortgang] (
        [Id]                        INT             NOT NULL IDENTITY(1,1)   PRIMARY KEY,
        [ProjectId]                 INT             NOT NULL,
        [FysiekeVoortgangPct]       DECIMAL(7,2)    NOT NULL DEFAULT 0,
        [FinancieleVoortgangPct]    DECIMAL(7,2)    NOT NULL DEFAULT 0,
        [ContractueleVolwassenheidPct] DECIMAL(7,2)  NOT NULL DEFAULT 0,
        [Fase]                      INT             NOT NULL DEFAULT 0,
        [Warnings]                  NVARCHAR(500)   NULL,
        [BerekendOp]                DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [ManueelAfgesloten]         BIT             NOT NULL DEFAULT 0,

        CONSTRAINT [FK_ProjectVoortgang_Project]
            FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID])
    );

    CREATE UNIQUE INDEX [UX_ProjectVoortgang_ProjectId]
        ON [dbo].[ProjectVoortgang] ([ProjectId]);

    PRINT 'Tabel ProjectVoortgang aangemaakt.';
END
ELSE
    PRINT 'Tabel ProjectVoortgang bestaat al, overgeslagen.';
