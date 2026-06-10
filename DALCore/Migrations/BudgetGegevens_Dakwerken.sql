-- Voeg AantalVeluxen toe aan BudgetGegevens
ALTER TABLE [dbo].[BudgetGegevens]
    ADD [AantalVeluxen] INT NULL;

-- Voeg de 7 dakwerken formule-slots toe in KostprijsFormulaKoppeling
INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving])
VALUES
    ('dakwerken_platdak',              'Platdak (€/m²)'),
    ('dakwerken_groendak',             'Groendak (€/m²)'),
    ('dakwerken_daktimmerwerk',        'Daktimmerwerk (€/m²)'),
    ('dakwerken_dakoversteken',        'Dakoversteken (€/m²)'),
    ('dakwerken_onderkantdoorrit',     'Onderkant doorrit (€/m²)'),
    ('dakwerken_hellenddak_bedekking', 'Hellend dak bedekking (€/m²)'),
    ('dakwerken_veluxen',              'Veluxen (€/stuk)');
