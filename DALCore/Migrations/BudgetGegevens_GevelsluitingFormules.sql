-- Voeg formule-slots toe voor bovenbouw aanvullend en gevelsluiting
INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving])
VALUES
    ('gevelsluiting_buitenschrijnwerk','Buitenschrijnwerk (€/m²)'),
    ('gevelsluiting_ballustrades',     'Ballustrades (€/lm)'),
    ('gevelsluiting_zichtschermen',    'Zichtschermen (€/lm)'),
    ('gevelsluiting_leien',            'Leien gevelbekleding (€/m²)');
