-- =============================================
-- Migratie: 158_Bouwstaten
-- Omschrijving: stap A van het herziene planningsontwerp (PLANNING_ONTWERP_HERZIEN.md, PLANNING_BOUWSTATEN_VOORSTEL.md).
--   * Nieuwe tabel Bouwstaat: bedrijfsbrede standaardlijst met vaste codes (ProjectId leeg), uitbreidbaar per project.
--   * Nieuwe nullable kolommen:
--       PlanningSjabloonTaak.BouwstaatId, .ActivityId     PlanningTaak.BouwstaatId, .ActivityId
--       PlanningSectie.BouwstaatId                        SchijfSjabloonSchijf.BouwstaatId, .BouwstaatRegel
--       InvoicingPaymentStages.BouwstaatId, .BouwstaatRegel   (0 = alle leverende taken voltooid, 1 = alle gestart)
--   * De standaardlijst wordt ingevuld en bestaande codes worden eenmalig gekoppeld (enkel waar de nieuwe kolom nog leeg is).
--   * Disciplines = activiteiten (Activity): geen aparte tabel. Taken krijgen hun activiteit op naam.
--   * De typfout "Binnnenbepleistering" wordt rechtgezet (ook zichtbaar in contracten en budgetten).
--   Additief en idempotent. Draai vóór app-start. Het gedrag van de planning verandert niet: ze leest de nieuwe kolommen nog niet.
-- =============================================

IF OBJECT_ID(N'[dbo].[Bouwstaat]') IS NULL
BEGIN
    CREATE TABLE [dbo].[Bouwstaat] (
        [Id]          INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Bouwstaat] PRIMARY KEY,
        [Code]        NVARCHAR(60)  NOT NULL,
        [Naam]        NVARCHAR(200) NOT NULL,
        [Groep]       NVARCHAR(100) NULL,
        [Volgorde]    INT NOT NULL CONSTRAINT [DF_Bouwstaat_Volgorde] DEFAULT 0,
        [Bereik]      TINYINT NOT NULL CONSTRAINT [DF_Bouwstaat_Bereik] DEFAULT 0,      -- 0 = gebouw (alle eenheden), 1 = per eenheid
        [PerBouwlaag] BIT NOT NULL CONSTRAINT [DF_Bouwstaat_PerBouwlaag] DEFAULT 0,
        [IsActief]    BIT NOT NULL CONSTRAINT [DF_Bouwstaat_IsActief] DEFAULT 1,
        [ProjectId]   INT NULL
    );
    CREATE INDEX [IX_Bouwstaat_Code] ON [dbo].[Bouwstaat]([Code]);
    PRINT 'Tabel Bouwstaat aangemaakt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSjabloonTaak]') AND name = N'BouwstaatId')
    ALTER TABLE [dbo].[PlanningSjabloonTaak] ADD [BouwstaatId] INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSjabloonTaak]') AND name = N'ActivityId')
    ALTER TABLE [dbo].[PlanningSjabloonTaak] ADD [ActivityId] INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningTaak]') AND name = N'BouwstaatId')
    ALTER TABLE [dbo].[PlanningTaak] ADD [BouwstaatId] INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningTaak]') AND name = N'ActivityId')
    ALTER TABLE [dbo].[PlanningTaak] ADD [ActivityId] INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PlanningSectie]') AND name = N'BouwstaatId')
    ALTER TABLE [dbo].[PlanningSectie] ADD [BouwstaatId] INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SchijfSjabloonSchijf]') AND name = N'BouwstaatId')
    ALTER TABLE [dbo].[SchijfSjabloonSchijf] ADD [BouwstaatId] INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SchijfSjabloonSchijf]') AND name = N'BouwstaatRegel')
    ALTER TABLE [dbo].[SchijfSjabloonSchijf] ADD [BouwstaatRegel] TINYINT NOT NULL CONSTRAINT [DF_SchijfSjabloonSchijf_BouwstaatRegel] DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InvoicingPaymentStages]') AND name = N'BouwstaatId')
    ALTER TABLE [dbo].[InvoicingPaymentStages] ADD [BouwstaatId] INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InvoicingPaymentStages]') AND name = N'BouwstaatRegel')
    ALTER TABLE [dbo].[InvoicingPaymentStages] ADD [BouwstaatRegel] TINYINT NOT NULL CONSTRAINT [DF_InvoicingPaymentStages_BouwstaatRegel] DEFAULT 0;
GO

-- Standaardlijst (alleen wat nog niet bestaat; bedrijfsbreed)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'VOORBEREIDING_KLAAR' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'VOORBEREIDING_KLAAR', N'Voorbereidende werken klaar', N'Voorbereiding', 10, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'WERF_UITGEZET' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'WERF_UITGEZET', N'Werf ingericht en uitgezet', N'Voorbereiding', 20, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'KELDER_UITGEGRAVEN' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'KELDER_UITGEGRAVEN', N'Kelder uitgegraven', N'Ruwbouw onderbouw', 30, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'FUNDERING_KLAAR' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'FUNDERING_KLAAR', N'Fundering klaar', N'Ruwbouw onderbouw', 31, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'VLOERPLAAT_KELDER' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'VLOERPLAAT_KELDER', N'Vloerplaat kelder gegoten', N'Ruwbouw onderbouw', 32, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'WANDEN_KELDER' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'WANDEN_KELDER', N'Kelderwanden klaar', N'Ruwbouw onderbouw', 33, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'VLOERPLAAT_GLVL' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'VLOERPLAAT_GLVL', N'Vloerplaat gelijkvloers gegoten (zonder kelder)', N'Ruwbouw bovenbouw', 40, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'PREDALLEN' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'PREDALLEN', N'Predallen geplaatst', N'Ruwbouw bovenbouw', 41, 0, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'OPSTORT' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'OPSTORT', N'Opstort gegoten', N'Ruwbouw bovenbouw', 42, 0, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'METSELWERK_RUWBOUW' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'METSELWERK_RUWBOUW', N'Metselwerk ruwbouw klaar', N'Ruwbouw bovenbouw', 43, 0, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'TERRAS_LUIFEL' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'TERRAS_LUIFEL', N'Terrassen en luifels klaar', N'Ruwbouw bovenbouw', 44, 0, 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'DAKPLAAT' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'DAKPLAAT', N'Dakplaat gegoten (plat dak)', N'Ruwbouw bovenbouw', 45, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'GEVELMETSELWERK' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'GEVELMETSELWERK', N'Gevelmetselwerk klaar', N'Ruwbouw bovenbouw', 46, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'DAK_PLAT_DICHT' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'DAK_PLAT_DICHT', N'Plat dak waterdicht', N'Dak', 50, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'DAK_HELLEND_DICHT' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'DAK_HELLEND_DICHT', N'Hellend dak gedekt', N'Dak', 51, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'DAKOPSTANDEN' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'DAKOPSTANDEN', N'Dak- en liftopstanden klaar', N'Dak', 52, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'BUITENSCHRIJNWERK' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'BUITENSCHRIJNWERK', N'Buitenschrijnwerk en glas geplaatst', N'Gesloten gebouw', 60, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'TECHNIEKEN_RUW' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'TECHNIEKEN_RUW', N'Leidingen technieken geplaatst', N'Technieken', 70, 0, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'PLEISTER' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'PLEISTER', N'Pleisterwerk klaar', N'Afwerking', 80, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'ISOLATIE_VLOER' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'ISOLATIE_VLOER', N'Vloerisolatie geplaatst', N'Afwerking', 81, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'CHAPE' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'CHAPE', N'Chape geplaatst', N'Afwerking', 82, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'TEGELWERK' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'TEGELWERK', N'Tegelwerk klaar', N'Afwerking', 83, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'VLOEREN_PARKET' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'VLOEREN_PARKET', N'Parket en vloeren klaar', N'Afwerking', 84, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'KEUKEN' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'KEUKEN', N'Keuken geplaatst', N'Afwerking', 85, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'BINNENSCHRIJNWERK' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'BINNENSCHRIJNWERK', N'Binnenschrijnwerk klaar', N'Afwerking', 86, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'OPKUIS' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'OPKUIS', N'Opgekuist', N'Oplevering', 90, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'VOORLOPIGE_OPLEVERING' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'VOORLOPIGE_OPLEVERING', N'Voorlopige oplevering (sleutels)', N'Oplevering', 91, 1, 0);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] WHERE Code = N'DEFINITIEVE_OPLEVERING' AND ProjectId IS NULL)
    INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES (N'DEFINITIEVE_OPLEVERING', N'Definitieve oplevering (einde waarborg)', N'Oplevering', 92, 1, 0);
GO

-- Typfout rechtzetten
UPDATE [dbo].[Activity] SET Omschrijving = N'Binnenbepleistering' WHERE Omschrijving = N'Binnnenbepleistering';
GO

-- Bestaande codes koppelen aan een bouwstaat en een activiteit (enkel waar de kolom nog leeg is)
DECLARE @map TABLE (Code NVARCHAR(100), Bouwstaat NVARCHAR(60) NULL, Activiteit NVARCHAR(200) NULL);
INSERT INTO @map (Code, Bouwstaat, Activiteit) VALUES
    (N'AFBRAAK', N'VOORBEREIDING_KLAAR', N'Afbraakwerken'),
    (N'WERF_UITZETTEN', N'WERF_UITGEZET', N'Werfinrichting'),
    (N'UITZETTEN', N'WERF_UITGEZET', N'Werfinrichting'),
    (N'UITGRAVEN_KELDER', N'KELDER_UITGEGRAVEN', N'Grondwerken'),
    (N'FUNDERINGEN', N'FUNDERING_KLAAR', N'Ruwbouw'),
    (N'VLOERPLAAT_KELDER', N'VLOERPLAAT_KELDER', N'Ondergrondse kelder'),
    (N'WANDEN_KELDER', N'WANDEN_KELDER', N'Ondergrondse kelder'),
    (N'AFDEK_KELDER', NULL, N'Ondergrondse kelder'),
    (N'VLOERPLAAT_GLVL', N'VLOERPLAAT_GLVL', N'Ruwbouw'),
    (N'VLOERPLAAT_V1', N'OPSTORT', N'Ruwbouw'),
    (N'PREDALLEN', N'PREDALLEN', N'Ruwbouw'),
    (N'OPSTORT', N'OPSTORT', N'Ruwbouw'),
    (N'METSELWERK_RUWBOUW', N'METSELWERK_RUWBOUW', N'Ruwbouw'),
    (N'TERRAS_LUIFEL', N'TERRAS_LUIFEL', N'Ruwbouw'),
    (N'PREDALLEN_DAK', N'DAKPLAAT', N'Ruwbouw'),
    (N'OPSTORT_DAK', N'DAKPLAAT', N'Ruwbouw'),
    (N'DAKPLAAT', N'DAKPLAAT', N'Ruwbouw'),
    (N'GEVELMETSELWERK', N'GEVELMETSELWERK', N'Ruwbouw'),
    (N'DAKOPSTANDEN', N'DAKOPSTANDEN', N'Dakbedekking - Plat dak - EPDM'),
    (N'DAKBEDEKKING_PLAT', N'DAK_PLAT_DICHT', N'Dakbedekking - Plat dak - EPDM'),
    (N'DAKBEDEKKING', N'DAK_PLAT_DICHT', N'Dakbedekking - Plat dak - EPDM'),
    (N'DAKSTRUCTUUR', NULL, N'Daktimmerwerken'),
    (N'DAKBEDEKKING_HELLEND', N'DAK_HELLEND_DICHT', N'Dakbedekking - Hellend Dak'),
    (N'RAMEN', N'BUITENSCHRIJNWERK', N'Buitenschrijnwerk - Aluminium'),
    (N'LEIDINGEN', N'TECHNIEKEN_RUW', NULL),
    (N'PLEISTER', N'PLEISTER', N'Binnenbepleistering'),
    (N'ISOLATIE', N'ISOLATIE_VLOER', N'Vloer - Isolatie - PUR'),
    (N'CHAPE', N'CHAPE', N'Vloer - Dekvloer'),
    (N'TEGELWERK', N'TEGELWERK', N'Vloer - Keramisch'),
    (N'PARKET', N'VLOEREN_PARKET', N'Vloer - Hout'),
    (N'KEUKEN', N'KEUKEN', N'Vast Meubilair - Keukens'),
    (N'BINNENSCHRIJNWERK', N'BINNENSCHRIJNWERK', N'Binnendeuren'),
    (N'PLINTEN', N'BINNENSCHRIJNWERK', N'Binnendeuren'),
    (N'OPKUIS', N'OPKUIS', N'Schoonmaak'),
    (N'VOORLOPIGE_OPLEVERING', N'VOORLOPIGE_OPLEVERING', NULL);

-- Sjabloontaken
UPDATE t SET t.BouwstaatId = b.Id
FROM [dbo].[PlanningSjabloonTaak] t
JOIN @map m ON m.Code = t.Code
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE t.BouwstaatId IS NULL;

UPDATE t SET t.ActivityId = a.ActivityId
FROM [dbo].[PlanningSjabloonTaak] t
JOIN @map m ON m.Code = t.Code
JOIN [dbo].[Activity] a ON a.Omschrijving = m.Activiteit
WHERE t.ActivityId IS NULL;

-- Planningtaken
UPDATE t SET t.BouwstaatId = b.Id
FROM [dbo].[PlanningTaak] t
JOIN @map m ON m.Code = t.Code
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE t.BouwstaatId IS NULL;

UPDATE t SET t.ActivityId = a.ActivityId
FROM [dbo].[PlanningTaak] t
JOIN @map m ON m.Code = t.Code
JOIN [dbo].[Activity] a ON a.Omschrijving = m.Activiteit
WHERE t.ActivityId IS NULL;

-- Planningfases
UPDATE s SET s.BouwstaatId = b.Id
FROM [dbo].[PlanningSectie] s
JOIN @map m ON m.Code = s.Code
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE s.BouwstaatId IS NULL;

-- Schijven in sjablonen en betalingsgroepen (op de planningcode)
UPDATE s SET s.BouwstaatId = b.Id
FROM [dbo].[SchijfSjabloonSchijf] s
JOIN @map m ON m.Code = s.PlanningCode
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE s.BouwstaatId IS NULL;

UPDATE s SET s.BouwstaatId = b.Id
FROM [dbo].[InvoicingPaymentStages] s
JOIN @map m ON m.Code = s.PlanningCode
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE s.BouwstaatId IS NULL;
GO

-- Controle: wat nog niet gekoppeld is (mag leeg zijn). In SSMS onder Results.
SELECT 'Sjabloontaak zonder bouwstaat' AS Soort, t.Code, t.Naam FROM [dbo].[PlanningSjabloonTaak] t WHERE t.BouwstaatId IS NULL
UNION ALL
SELECT 'Sjabloonschijf met planningcode zonder bouwstaat', s.PlanningCode, s.Naam FROM [dbo].[SchijfSjabloonSchijf] s WHERE s.PlanningCode IS NOT NULL AND s.BouwstaatId IS NULL;
GO
