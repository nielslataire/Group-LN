-- Voeg M3Onderschoeiingen toe aan BudgetGegevens
ALTER TABLE [dbo].[BudgetGegevens]
    ADD [M3Onderschoeiingen] DECIMAL(10,2) NULL;

-- Voeg de 5 onderbouw formule-slots toe in KostprijsFormulaKoppeling
INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving])
VALUES
    ('onderbouw_berlinerwanden',   'Berlinerwanden (€/lm)'),
    ('onderbouw_secanpalen',       'Secanpalen (€/lm)'),
    ('onderbouw_funderingen',      'Funderingen (€/m²)'),
    ('onderbouw_grondwerken',      'Grondwerken (€/m³)'),
    ('onderbouw_onderschoeiingen', 'Onderschoeiingen (€/m³)');
