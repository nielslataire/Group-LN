-- ============================================================
-- BudgetActivityFormule — sectionaalpoort / kantelpoort
-- Aantal garages (tab Oppervlaktes) × prijs van het gekozen poorttype (tab Gegevens).
-- De niet-gekozen poorttype-teller staat op 0, dus die formule levert 0 op.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 211)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (211, N'Sectionaalpoort', N'@mat_poorten_sectionaalpoort * @aantal_poorten_sectionaal');

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 210)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (210, N'Kantelpoort', N'@mat_poorten_kantelpoort * @aantal_poorten_kantel');

GO
