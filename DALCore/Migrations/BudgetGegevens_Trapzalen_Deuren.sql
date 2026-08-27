-- ============================================================
-- Trapzalen + deuren-formule (activiteit 230)
-- 1. AantalTrapzalen kolom op BudgetGegevens
-- 2. Formule-slots voor deur-materialen in KostprijsFormulaKoppeling
--    (koppel daarna de materialen in Instellingen → Kostprijsmaterialen
--     → Formule koppelingen; ze verschijnen als @mat_deuren_* parameters)
-- 3. Seed-formule voor activiteit 230 (binnen- en buitendeuren)
-- Gebruik IF NOT EXISTS zodat het script opnieuw uitvoerbaar is.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.BudgetGegevens') AND name = N'AantalTrapzalen')
    ALTER TABLE [dbo].[BudgetGegevens]
        ADD [AantalTrapzalen] INT NULL;

GO

-- ── Formule-slots deuren ─────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM [dbo].[KostprijsFormulaKoppeling] WHERE [Sleutel] = 'deuren_buitendeur_app')
    INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving]) VALUES
        ('deuren_buitendeur_app',         'Buitendeur appartement (€/stuk)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[KostprijsFormulaKoppeling] WHERE [Sleutel] = 'deuren_binnendeuren_app')
    INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving]) VALUES
        ('deuren_binnendeuren_app',       'Binnendeuren appartement (€/app.)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[KostprijsFormulaKoppeling] WHERE [Sleutel] = 'deuren_buitendeur_ondergronds')
    INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving]) VALUES
        ('deuren_buitendeur_ondergronds', 'Buitendeur ondergronds (€/stuk)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[KostprijsFormulaKoppeling] WHERE [Sleutel] = 'deuren_binnendeuren_woning')
    INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving]) VALUES
        ('deuren_binnendeuren_woning',    'Binnendeuren woning (€/woning)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[KostprijsFormulaKoppeling] WHERE [Sleutel] = 'deuren_bergingdeur')
    INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving]) VALUES
        ('deuren_bergingdeur',            'Bergingdeur (€/stuk)');

IF NOT EXISTS (SELECT 1 FROM [dbo].[KostprijsFormulaKoppeling] WHERE [Sleutel] = 'deuren_trapzaaldeur')
    INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving]) VALUES
        ('deuren_trapzaaldeur',           'Deur trapzaal (€/stuk)');

GO

-- ── Seed-formule activiteit 230 ──────────────────────────────────────────────
-- Trapzaaldeuren tellen enkel mee vanaf meer dan 4 bovengrondse verdiepingen.

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 230)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (230, N'Deuren', N'@aantal_appartementen * @mat_deuren_buitendeur_app + @aantal_appartementen * @mat_deuren_binnendeuren_app + @aantal_trapzalen * @verdiepingen_ondergronds * @mat_deuren_buitendeur_ondergronds + @aantal_woningen * @mat_deuren_binnendeuren_woning + @aantal_bergingen * @mat_deuren_bergingdeur + ALS(@verdiepingen_bovengronds > 4; @verdiepingen_bovengronds * @aantal_trapzalen * @mat_deuren_trapzaaldeur; 0)');

GO
