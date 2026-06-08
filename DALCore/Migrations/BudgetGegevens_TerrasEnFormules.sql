-- Voeg TerrasPrijsPerM2 toe aan BudgetGegevens
ALTER TABLE [dbo].[BudgetGegevens]
    ADD [TerrasPrijsPerM2] DECIMAL(8,2) NULL;

-- Voeg de 3 nieuwe formule-slots toe in KostprijsFormulaKoppeling
INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving])
VALUES
    ('bovenbouw_gevelmetselwerk', 'Gevelmetselwerk (€/m²)'),
    ('bovenbouw_gipsblokken',     'Gipsblokken (€/m²)'),
    ('bovenbouw_terras',          'Terras (€/m²)');
