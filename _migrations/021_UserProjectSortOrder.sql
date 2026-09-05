-- =============================================
-- Migratie: UserProjectSortOrder
-- Datum: 2026-09-05
-- Omschrijving:
--   Laat een gebruiker een eigen, handmatige volgorde opslaan voor de
--   werf-kaarten op het projectleider-dashboard ("Mijn Werven" -> Rangschikken).
--   Eén rij per (UserId, ProjectId); SortOrder is de 0-gebaseerde positie.
--   Bij elke "Klaar" in de rangschik-modus wordt de volledige set voor die
--   gebruiker vervangen (delete + insert), dus geen extra status-kolom nodig.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserProjectSortOrder' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[UserProjectSortOrder] (
        [Id]        INT NOT NULL IDENTITY(1,1),
        [UserId]    INT NOT NULL,
        [ProjectId] INT NOT NULL,
        [SortOrder] INT NOT NULL,

        CONSTRAINT [PK_UserProjectSortOrder]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_UserProjectSortOrder_Users]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([ID])
            ON DELETE CASCADE,

        CONSTRAINT [FK_UserProjectSortOrder_Project]
            FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project] ([ProjectID])
            ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [UX_UserProjectSortOrder_User_Project]
        ON [dbo].[UserProjectSortOrder] ([UserId], [ProjectId]);

    CREATE INDEX [IX_UserProjectSortOrder_UserId]
        ON [dbo].[UserProjectSortOrder] ([UserId]);

    PRINT 'Tabel UserProjectSortOrder aangemaakt.';
END
ELSE
    PRINT 'Tabel UserProjectSortOrder bestaat al, overgeslagen.';
