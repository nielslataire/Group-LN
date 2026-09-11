-- =============================================
-- Migratie: 036_TrajectStandaardTriggers
-- Datum: 2026-09-11
-- Omschrijving: Voegt twee demonstratie-/standaardtriggers toe aan het
--   Woonproject-sjabloon, zodat de trigger-engine (increment 3) meteen iets
--   te evalueren heeft:
--   - "Vergunning definitief" bereikt -> volgende fase (Voorverkoop) ontgrendelen
--     + de projectleider verwittigen (rol-override naar Projectleider, want de
--     mijlpaal zelf staat op Projectontwikkelaar, die geen directe rol-koppeling heeft).
--   - "Voorlopige oplevering" (project) bij overschrijding -> herinnering.
--   Enkel toegevoegd waar nog geen trigger met dezelfde combinatie bestaat.
--   Strikt additief + idempotent. Geen schemawijziging.
--
--   TriggerEvent: 0=BijBereiken 1=BijOverschrijding
--   TriggerActie: 0=VerwittigRol 6=PlanHerinnering 7=DeblokkeerVolgendeFase
-- =============================================

IF EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloon] WHERE [IsStandaard] = 1 AND [ProjectType] = 1)
BEGIN
    EXEC(N'
    DECLARE @wp INT = (SELECT MIN([Id]) FROM [dbo].[TrajectSjabloon] WHERE [IsStandaard] = 1 AND [ProjectType] = 1);
    DECLARE @vergunningDefinitief INT = (
        SELECT m.[Id] FROM [dbo].[TrajectSjabloonMijlpaal] m
        JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
        WHERE f.[TrajectSjabloonId] = @wp AND m.[Code] = N''VERGUNNING_DEFINITIEF''
    );
    DECLARE @opleveringVoorlopig INT = (
        SELECT m.[Id] FROM [dbo].[TrajectSjabloonMijlpaal] m
        JOIN [dbo].[TrajectSjabloonFase] f ON f.[Id] = m.[TrajectSjabloonFaseId]
        WHERE f.[TrajectSjabloonId] = @wp AND m.[Code] = N''OPLEVERING_VOORLOPIG''
    );

    IF @vergunningDefinitief IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM [dbo].[TrajectSjabloonMijlpaalTrigger]
        WHERE [TrajectSjabloonMijlpaalId] = @vergunningDefinitief AND [TriggerActie] = 7
    )
        INSERT INTO [dbo].[TrajectSjabloonMijlpaalTrigger]
            ([TrajectSjabloonMijlpaalId], [TriggerEvent], [TriggerActie], [ActieParametersJson], [MagProjectWijzigen], [IsActief], [Omschrijving])
        VALUES
            (@vergunningDefinitief, 0, 7, NULL, 0, 1, N''Ontgrendelt de voorverkoopfase zodra de vergunning definitief is.'');

    IF @vergunningDefinitief IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM [dbo].[TrajectSjabloonMijlpaalTrigger]
        WHERE [TrajectSjabloonMijlpaalId] = @vergunningDefinitief AND [TriggerActie] = 0
    )
        INSERT INTO [dbo].[TrajectSjabloonMijlpaalTrigger]
            ([TrajectSjabloonMijlpaalId], [TriggerEvent], [TriggerActie], [ActieParametersJson], [MagProjectWijzigen], [IsActief], [Omschrijving])
        VALUES
            (@vergunningDefinitief, 0, 0, N''{"rol":1}'', 0, 1, N''Verwittigt de projectleider dat de vergunning definitief is.'');

    IF @opleveringVoorlopig IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM [dbo].[TrajectSjabloonMijlpaalTrigger]
        WHERE [TrajectSjabloonMijlpaalId] = @opleveringVoorlopig AND [TriggerEvent] = 1
    )
        INSERT INTO [dbo].[TrajectSjabloonMijlpaalTrigger]
            ([TrajectSjabloonMijlpaalId], [TriggerEvent], [TriggerActie], [ActieParametersJson], [MagProjectWijzigen], [IsActief], [Omschrijving])
        VALUES
            (@opleveringVoorlopig, 1, 6, NULL, 0, 1, N''Herinnering wanneer de voorlopige oplevering overschreden is.'');
    ');

    PRINT 'Standaardtriggers Woonproject toegevoegd (voor zover nog niet aanwezig).';
END
ELSE
    PRINT 'Woonproject-sjabloon niet gevonden, standaardtriggers overgeslagen.';

PRINT 'Migratie 036_TrajectStandaardTriggers voltooid.';
