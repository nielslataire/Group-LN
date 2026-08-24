-- ============================================================
-- BudgetActivityFormule
-- Bewerkbare voorstel-formules per activiteit (BudgetActivityLijnen).
-- Een formule berekent het TOTAAL voor het project; de service deelt
-- daarna door @aantal_wooncomm voor de prijs per eenheid.
-- Gebruik IF NOT EXISTS zodat het script opnieuw uitvoerbaar is.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'BudgetActivityFormule' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE [dbo].[BudgetActivityFormule]
    (
        [Id]              INT             NOT NULL IDENTITY(1,1),
        [ActivityId]      INT             NOT NULL,
        [Formule]         NVARCHAR(MAX)   NOT NULL,
        [Omschrijving]    NVARCHAR(300)   NULL,
        [Actief]          BIT             NOT NULL CONSTRAINT [DF_BudgetActivityFormule_Actief] DEFAULT 1,
        [LaatstGewijzigd] DATETIME2       NOT NULL CONSTRAINT [DF_BudgetActivityFormule_Gewijzigd] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_BudgetActivityFormule]
            PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [UQ_BudgetActivityFormule_Activity]
            UNIQUE ([ActivityId]),

        CONSTRAINT [FK_BudgetActivityFormule_Activity]
            FOREIGN KEY ([ActivityId])
            REFERENCES [dbo].[Activity] ([ActivityId])
            ON DELETE CASCADE
    );
END

GO

-- ── Seed: bestaande hardgecodeerde voorstellen als bewerkbare formules ───────
-- (zelfde berekening als voorheen in BudgetActivityService)

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 186)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (186, N'Ruwbouw', N'@prijs_ruwbouw_basis * @opp_ruwbouw + @prijs_terras * @opp_terras_prefab + @prijs_gevelmetselwerk * @opp_gevels + @prijs_gipsblokken * @aantal_wooncomm');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 177)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (177, N'Berlinerwanden', N'@mat_onderbouw_berlinerwanden * @lm_berlinerwanden');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 178)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (178, N'Secanpalen', N'@mat_onderbouw_secanpalen * @lm_secanpalen');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 183)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (183, N'Funderingen', N'@mat_onderbouw_funderingen * @opp_funderingen');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 173)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (173, N'Grondwerken', N'@mat_onderbouw_grondwerken * @m3_grondwerken');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 179)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (179, N'Onderschoeiingen', N'@mat_onderbouw_onderschoeiingen * @m3_onderschoeiingen');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 197)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (197, N'Platdak', N'@mat_dakwerken_platdak * @opp_platdak');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 202)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (202, N'Groendak', N'@mat_dakwerken_groendak * @opp_groendak');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 194)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (194, N'Daktimmerwerk', N'@mat_dakwerken_daktimmerwerk * @opp_hellend_dak * 1.42 * 0.45 + @mat_dakwerken_dakoversteken * @opp_dakoversteken + @mat_dakwerken_onderkantdoorrit * @opp_onderkant_doorrit');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 195)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (195, N'Hellend dak bedekking', N'@mat_dakwerken_hellenddak_bedekking * @opp_hellend_dak * 1.42 + @mat_dakwerken_veluxen * @aantal_veluxen');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 181)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (181, N'Kelderruwbouw', N'@mat_ruwbouw_basis * @opp_garberg_ondergronds');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 205)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (205, N'Buitenschrijnwerk', N'@mat_gevelsluiting_buitenschrijnwerk * @opp_ramen');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 217)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (217, N'Gevelsluiting', N'@mat_gevelsluiting_ballustrades * @lm_ballustrades + @mat_gevelsluiting_zichtschermen * @lm_zichtschermen');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 215)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (215, N'Leien gevelbekleding', N'@mat_gevelsluiting_leien * @opp_leien');

GO
