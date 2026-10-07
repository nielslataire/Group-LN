-- =============================================
-- Migratie: 076_BudgetReferentieProjecten
-- Datum: 2026-10-07
-- Omschrijving: Nacalculatie in de budgetwizard (stap 6, zie BUDGET_VOORTGANG.md): referentieprojecten met hun werkelijke kost
--   per activiteit (uit de app of uit Excel), en per budgetversie welke referentieprojecten ze vergelijkt.
--   BudgetReferentieProject      : kop (naam, optioneel project, peildatum, eenheden, GBA, indexen op peildatum, bron).
--   BudgetReferentieProjectLijn  : bedrag per activiteit (excl. btw, niet geïndexeerd).
--   BudgetVersieNacalcReferentie : koppeltabel versie ↔ referentieproject.
--   Strikt additief + idempotent.
-- =============================================
IF OBJECT_ID('dbo.BudgetReferentieProject', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BudgetReferentieProject] (
        [Id]             INT IDENTITY(1,1) NOT NULL,
        [Naam]           NVARCHAR(200)  NOT NULL,
        [ProjectId]      INT            NULL,
        [Datum]          DATE           NULL,
        [AantalEenheden] INT            NOT NULL CONSTRAINT DF_BudgetRefProject_Eenheden DEFAULT 0,
        [OppervlakteGBA] DECIMAL(18,2)  NULL,
        [SIndex]         DECIMAL(18,4)  NULL,
        [IIndex]         DECIMAL(18,4)  NULL,
        [Bron]           NVARCHAR(50)   NOT NULL CONSTRAINT DF_BudgetRefProject_Bron DEFAULT N'Excel',
        [Opmerking]      NVARCHAR(500)  NULL,
        [CreatedAt]      DATETIME2(0)   NOT NULL CONSTRAINT DF_BudgetRefProject_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT PK_BudgetReferentieProject PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT FK_BudgetRefProject_Project FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID]) ON DELETE SET NULL
    );
    PRINT 'Tabel BudgetReferentieProject aangemaakt.';
END
ELSE PRINT 'Tabel BudgetReferentieProject bestaat al, overgeslagen.';
GO
IF OBJECT_ID('dbo.BudgetReferentieProjectLijn', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BudgetReferentieProjectLijn] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [ReferentieProjectId] INT           NOT NULL,
        [ActivityId]          INT           NOT NULL,
        [Bedrag]              DECIMAL(18,2) NOT NULL,
        [Opmerking]           NVARCHAR(200) NULL,
        CONSTRAINT PK_BudgetReferentieProjectLijn PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT UQ_BudgetRefProjectLijn_Activiteit UNIQUE ([ReferentieProjectId], [ActivityId]),
        CONSTRAINT FK_BudgetRefProjectLijn_RefProject FOREIGN KEY ([ReferentieProjectId]) REFERENCES [dbo].[BudgetReferentieProject]([Id]) ON DELETE CASCADE,
        CONSTRAINT FK_BudgetRefProjectLijn_Activity FOREIGN KEY ([ActivityId]) REFERENCES [dbo].[Activity]([ActivityID])
    );
    PRINT 'Tabel BudgetReferentieProjectLijn aangemaakt.';
END
ELSE PRINT 'Tabel BudgetReferentieProjectLijn bestaat al, overgeslagen.';
GO
IF OBJECT_ID('dbo.BudgetVersieNacalcReferentie', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BudgetVersieNacalcReferentie] (
        [BudgetVersieId]      INT NOT NULL,
        [ReferentieProjectId] INT NOT NULL,
        CONSTRAINT PK_BudgetVersieNacalcReferentie PRIMARY KEY CLUSTERED ([BudgetVersieId], [ReferentieProjectId]),
        CONSTRAINT FK_BudgetVersieNacalcRef_Versie FOREIGN KEY ([BudgetVersieId]) REFERENCES [dbo].[BudgetVersie]([Id]) ON DELETE CASCADE,
        CONSTRAINT FK_BudgetVersieNacalcRef_RefProject FOREIGN KEY ([ReferentieProjectId]) REFERENCES [dbo].[BudgetReferentieProject]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabel BudgetVersieNacalcReferentie aangemaakt.';
END
ELSE PRINT 'Tabel BudgetVersieNacalcReferentie bestaat al, overgeslagen.';
GO
