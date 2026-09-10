-- =============================================
-- Migratie: 031_MijlpaalPerEenheid
-- Datum: 2026-09-10
-- Omschrijving: Mijlpalen kunnen per eenheid (Units) lopen.
--   - Mijlpaal.UnitId               : NULL = projectniveau, gezet = die ene eenheid
--   - TrajectSjabloonMijlpaal.Scope : 0 = Project, 1 = PerEenheid
--   Bij instantiatie / sync maakt de app voor elke PerEenheid-sjabloonmijlpaal
--   één Mijlpaal per unit van het project.
--   Strikt additief + idempotent. Geen GO-batchscheiders: statements die naar de
--   nieuwe kolommen verwijzen draaien via EXEC (parse pas bij uitvoering).
--
--   Voegt ook per-eenheid standaardmijlpalen toe aan het Woonproject-sjabloon.
-- =============================================

------------------------------------------------------------
-- 1. Mijlpaal.UnitId + FK + index
------------------------------------------------------------
IF COL_LENGTH('dbo.Mijlpaal', 'UnitId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Mijlpaal] ADD [UnitId] INT NULL;
    PRINT 'Kolom Mijlpaal.UnitId toegevoegd.';
END
ELSE
    PRINT 'Kolom Mijlpaal.UnitId bestaat al, overgeslagen.';

IF OBJECT_ID(N'[dbo].[FK_Mijlpaal_Units]', 'F') IS NULL
   AND COL_LENGTH('dbo.Mijlpaal', 'UnitId') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[Units]') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Mijlpaal] WITH CHECK
        ADD CONSTRAINT [FK_Mijlpaal_Units]
        FOREIGN KEY ([UnitId]) REFERENCES [dbo].[Units]([Id]);');
    PRINT 'FK Mijlpaal.UnitId -> Units toegevoegd.';
END
ELSE
    PRINT 'FK Mijlpaal.UnitId bestaat al of tabellen ontbreken, overgeslagen.';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Mijlpaal_Projecttraject_Unit' AND object_id = OBJECT_ID(N'[dbo].[Mijlpaal]'))
BEGIN
    EXEC(N'CREATE NONCLUSTERED INDEX [IX_Mijlpaal_Projecttraject_Unit]
        ON [dbo].[Mijlpaal] ([ProjecttrajectId], [UnitId]);');
    PRINT 'Index IX_Mijlpaal_Projecttraject_Unit aangemaakt.';
END
ELSE
    PRINT 'Index IX_Mijlpaal_Projecttraject_Unit bestaat al, overgeslagen.';

------------------------------------------------------------
-- 2. TrajectSjabloonMijlpaal.Scope
------------------------------------------------------------
IF COL_LENGTH('dbo.TrajectSjabloonMijlpaal', 'Scope') IS NULL
BEGIN
    ALTER TABLE [dbo].[TrajectSjabloonMijlpaal]
        ADD [Scope] INT NOT NULL CONSTRAINT [DF_TrajectSjabloonMijlpaal_Scope] DEFAULT 0;
    PRINT 'Kolom TrajectSjabloonMijlpaal.Scope toegevoegd.';
END
ELSE
    PRINT 'Kolom TrajectSjabloonMijlpaal.Scope bestaat al, overgeslagen.';

------------------------------------------------------------
-- 3. Per-eenheid standaardmijlpalen voor het Woonproject-sjabloon
------------------------------------------------------------
IF EXISTS (SELECT 1 FROM [dbo].[TrajectSjabloon] WHERE [IsStandaard] = 1 AND [ProjectType] = 1)
BEGIN
    EXEC(N'
    DECLARE @wp INT = (SELECT MIN([Id]) FROM [dbo].[TrajectSjabloon] WHERE [IsStandaard] = 1 AND [ProjectType] = 1);

    ;WITH f AS (SELECT [Id], [Code] FROM [dbo].[TrajectSjabloonFase] WHERE [TrajectSjabloonId] = @wp)
    INSERT INTO [dbo].[TrajectSjabloonMijlpaal]
        ([TrajectSjabloonFaseId], [Naam], [Code], [Volgorde], [MijlpaalType], [VerantwoordelijkeRol],
         [DoeldatumAnkerCode], [DoeldatumOffsetDagen], [IsVerplicht], [Scope])
    SELECT f.[Id], v.[Naam], v.[Code], v.[Volgorde], v.[MijlpaalType], v.[Rol], v.[Anker], v.[Offset], v.[Verplicht], 1
    FROM f
    JOIN (VALUES
        (N''VOORVERKOOP'', N''Reservatie eenheid'',            N''UNIT_RESERVATIE'',     110, 4, 5, N''VERKOOP_GESTART'',      30,  1),
        (N''VOORVERKOOP'', N''Compromis eenheid getekend'',    N''UNIT_COMPROMIS'',      120, 4, 5, N''VERKOOP_GESTART'',      60,  1),
        (N''VOORVERKOOP'', N''Akte eenheid verleden'',         N''UNIT_AKTE'',           130, 1, 5, N''VERKOOP_GESTART'',      180, 1),
        (N''OPLEVERING'',  N''Voorlopige oplevering eenheid'', N''UNIT_OPLEVERING_VL'',  110, 7, 1, N''AFWERKING_AF'',         14,  1),
        (N''OPLEVERING'',  N''Definitieve oplevering eenheid'',N''UNIT_OPLEVERING_DEF'', 120, 7, 1, N''OPLEVERING_VOORLOPIG'', 365, 1)
    ) AS v([FaseCode], [Naam], [Code], [Volgorde], [MijlpaalType], [Rol], [Anker], [Offset], [Verplicht])
      ON v.[FaseCode] = f.[Code]
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[TrajectSjabloonMijlpaal] m
        WHERE m.[TrajectSjabloonFaseId] = f.[Id] AND m.[Code] = v.[Code]
    );');

    PRINT 'Per-eenheid standaardmijlpalen Woonproject toegevoegd (voor zover nog niet aanwezig).';
END
ELSE
    PRINT 'Woonproject-sjabloon niet gevonden, per-eenheid mijlpalen overgeslagen.';

PRINT 'Migratie 031_MijlpaalPerEenheid voltooid.';
