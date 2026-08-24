-- Voeg formule-slots toe voor sectionaalpoort en kantelpoort
INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving])
VALUES
    ('poorten_sectionaalpoort', 'Sectionaalpoort (€/stuk)'),
    ('poorten_kantelpoort',     'Kantelpoort (€/stuk)');
