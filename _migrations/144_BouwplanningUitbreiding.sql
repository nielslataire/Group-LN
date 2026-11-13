-- =============================================
-- Migratie: 144_BouwplanningUitbreiding
-- Omschrijving: Projecten › Planning (bouwplanning met duur in werkdagen, voorgangers en Gantt).
--   Breidt de bestaande PlanningSectie (= fase) en PlanningTaak (= taak, duur 0 = mijlpaal) uit (migratie 005)
--   en voegt PlanningAfhankelijkheid en ProjectPlanning (één per project) toe.
--   Startdatum/Einddatum van een taak zijn voortaan BEREKENDE waarden (cache); de invoer is duur,
--   voorgangers en beperking. De tabellen bevatten op productie geen rijen, dus geen datamigratie.
--   Een planningsfase telt enkel mee voor de fysieke voortgang van een lot als TeltMeeVoorVoortgang aanstaat.
--   Additief en idempotent.
-- =============================================

-- ── PlanningSectie: deelfases en opt-in voor de voortgang ────────────────────────────────────
IF COL_LENGTH('dbo.PlanningSectie', 'ParentSectieId') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlanningSectie] ADD [ParentSectieId] INT NULL;
    PRINT 'Kolom PlanningSectie.ParentSectieId toegevoegd.';
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PlanningSectie_Parent')
BEGIN
    ALTER TABLE [dbo].[PlanningSectie] ADD CONSTRAINT [FK_PlanningSectie_Parent]
        FOREIGN KEY ([ParentSectieId]) REFERENCES [dbo].[PlanningSectie]([Id]);
    PRINT 'FK_PlanningSectie_Parent toegevoegd.';
END
GO
IF COL_LENGTH('dbo.PlanningSectie', 'TeltMeeVoorVoortgang') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlanningSectie] ADD [TeltMeeVoorVoortgang] BIT NOT NULL CONSTRAINT [DF_PlanningSectie_TeltMee] DEFAULT 0;
    PRINT 'Kolom PlanningSectie.TeltMeeVoorVoortgang toegevoegd.';
END
GO

-- ── PlanningTaak: duur, beperking, deadline, werkelijke start, verantwoordelijke, basisplanning ──
IF COL_LENGTH('dbo.PlanningTaak', 'Duur') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlanningTaak] ADD [Duur] INT NOT NULL CONSTRAINT [DF_PlanningTaak_Duur] DEFAULT 1;
    PRINT 'Kolom PlanningTaak.Duur toegevoegd.';
END
GO
IF COL_LENGTH('dbo.PlanningTaak', 'BeperkingType') IS NULL
BEGIN
    -- 0 zo vroeg mogelijk · 1 moet beginnen op · 2 niet eerder beginnen dan · 3 niet later beginnen dan
    -- 4 niet eerder eindigen dan · 5 niet later eindigen dan · 6 moet eindigen op
    ALTER TABLE [dbo].[PlanningTaak] ADD [BeperkingType] TINYINT NOT NULL CONSTRAINT [DF_PlanningTaak_BeperkingType] DEFAULT 0;
    PRINT 'Kolom PlanningTaak.BeperkingType toegevoegd.';
END
GO
IF COL_LENGTH('dbo.PlanningTaak', 'BeperkingDatum') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlanningTaak] ADD [BeperkingDatum] DATE NULL;
    PRINT 'Kolom PlanningTaak.BeperkingDatum toegevoegd.';
END
GO
IF COL_LENGTH('dbo.PlanningTaak', 'Deadline') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlanningTaak] ADD [Deadline] DATE NULL;
    PRINT 'Kolom PlanningTaak.Deadline toegevoegd.';
END
GO
IF COL_LENGTH('dbo.PlanningTaak', 'WerkelijkStart') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlanningTaak] ADD [WerkelijkStart] DATE NULL;
    PRINT 'Kolom PlanningTaak.WerkelijkStart toegevoegd.';
END
GO
IF COL_LENGTH('dbo.PlanningTaak', 'VerantwoordelijkeUserId') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlanningTaak] ADD [VerantwoordelijkeUserId] NVARCHAR(450) NULL;
    PRINT 'Kolom PlanningTaak.VerantwoordelijkeUserId toegevoegd.';
END
GO
IF COL_LENGTH('dbo.PlanningTaak', 'BasisStart') IS NULL
BEGIN
    ALTER TABLE [dbo].[PlanningTaak] ADD [BasisStart] DATE NULL;
    ALTER TABLE [dbo].[PlanningTaak] ADD [BasisEinde] DATE NULL;
    PRINT 'Kolommen PlanningTaak.BasisStart/BasisEinde toegevoegd.';
END
GO

-- ── PlanningAfhankelijkheid: voorganger → opvolger ───────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[PlanningAfhankelijkheid]'))
BEGIN
    CREATE TABLE [dbo].[PlanningAfhankelijkheid] (
        [Id]                INT      NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [VoorgangerTaakId]  INT      NOT NULL,
        [OpvolgerTaakId]    INT      NOT NULL,
        -- 0 FS (einde → begin) · 1 SS (begin → begin) · 2 FF (einde → einde) · 3 SF (begin → einde)
        [Type]              TINYINT  NOT NULL CONSTRAINT [DF_PlanningAfh_Type] DEFAULT 0,
        -- Wachttijd in werkdagen, ook negatief (overlap).
        [WachtDagen]        INT      NOT NULL CONSTRAINT [DF_PlanningAfh_Wacht] DEFAULT 0,
        CONSTRAINT [FK_PlanningAfh_Voorganger] FOREIGN KEY ([VoorgangerTaakId]) REFERENCES [dbo].[PlanningTaak]([Id]),
        CONSTRAINT [FK_PlanningAfh_Opvolger]   FOREIGN KEY ([OpvolgerTaakId])   REFERENCES [dbo].[PlanningTaak]([Id]),
        CONSTRAINT [UQ_PlanningAfh_Paar] UNIQUE ([VoorgangerTaakId], [OpvolgerTaakId])
    );
    CREATE INDEX [IX_PlanningAfh_Opvolger] ON [dbo].[PlanningAfhankelijkheid] ([OpvolgerTaakId]);
    PRINT 'Tabel PlanningAfhankelijkheid aangemaakt.';
END
ELSE PRINT 'Tabel PlanningAfhankelijkheid bestaat al, overgeslagen.';
GO

-- ── ProjectPlanning: kopgegevens, één rij per project ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ProjectPlanning]'))
BEGIN
    CREATE TABLE [dbo].[ProjectPlanning] (
        [ProjectId]          INT           NOT NULL PRIMARY KEY,
        -- Telt op bij elke wijziging; wie met een oudere versie opslaat krijgt een conflictmelding.
        [Versie]             INT           NOT NULL CONSTRAINT [DF_ProjectPlanning_Versie] DEFAULT 1,
        [BasisVastgelegdOp]  DATETIME2     NULL,
        -- Uit = simulatie zonder verlet.
        [VerletMeetellen]    BIT           NOT NULL CONSTRAINT [DF_ProjectPlanning_Verlet] DEFAULT 1,
        [CreatedDate]        DATETIME2     NOT NULL CONSTRAINT [DF_ProjectPlanning_Created] DEFAULT SYSUTCDATETIME(),
        [ModifiedDate]       DATETIME2     NULL,
        [ModifiedByUserId]   NVARCHAR(450) NULL,
        CONSTRAINT [FK_ProjectPlanning_Project] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Project]([ProjectID])
    );
    PRINT 'Tabel ProjectPlanning aangemaakt.';
END
ELSE PRINT 'Tabel ProjectPlanning bestaat al, overgeslagen.';
GO
