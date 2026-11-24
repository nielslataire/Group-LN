-- =============================================
-- TEST_sjablonen_verwijderen.sql  (GEEN genummerde migratie: enkel om met een schone lei te beginnen)
-- Verwijdert ALLE sjablonen van de gekozen soorten: projectsjablonen, planningssjablonen, schijvensjablonen en/of trajectsjablonen,
-- met alles wat eraan hangt (blokken, taken, voorgangers, schijven, mijlpalen, fasen, triggers).
--
-- WAT BLIJFT STAAN: projecten, hun plannings, trajecten en betalingsgroepen. Een verwijzing vanuit een project naar een sjabloon wordt leeggemaakt.
-- VEILIGHEID: het script telt vóór en na het aantal trajecten, mijlpalen, planningtaken, planningfases en betalingsschijven van ALLE projecten.
-- Verandert een van die aantallen, dan stopt het script en wordt alles teruggedraaid (er zou projectdata verdwijnen).
-- Bouwstaten en activiteiten zijn GEEN sjablonen en blijven altijd staan.
--
-- HOE: 1. Kies hieronder welke soorten weg moeten.  2. Draai zoals het is: PROEFRIT (alles wordt teruggedraaid, je ziet de namen en aantallen).
--      3. Klopt het? Zet @Uitvoeren op 1 en draai opnieuw.
-- =============================================

SET NOCOUNT ON;
GO

-- Hulpprocedure (tijdelijk, verdwijnt met de verbinding): verwijdert rijen en ruimt alles op wat eraan hangt.
-- Een verwijzing die leeg mag zijn wordt leeggemaakt (de rij blijft bestaan); een verplichte verwijzing betekent dat de rij eraan hangt en mee verdwijnt.
IF OBJECT_ID('tempdb..#Ontkoppel') IS NOT NULL DROP PROCEDURE #Ontkoppel;
GO
CREATE PROCEDURE #Ontkoppel @tabel NVARCHAR(300), @where NVARCHAR(MAX), @diepte INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF @diepte > 20 THROW 50001, N'Te veel niveaus diep; het script stopt veilig.', 1;

    DECLARE @child NVARCHAR(300), @ccol NVARCHAR(200), @pcol NVARCHAR(200), @nullable BIT, @aantal INT, @fk NVARCHAR(200), @sub NVARCHAR(MAX), @sql NVARCHAR(MAX), @n INT, @volgende INT;
    DECLARE c CURSOR LOCAL FAST_FORWARD FOR
        SELECT fk.name,
               QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)),
               QUOTENAME(cc.name), QUOTENAME(pc.name), CASE WHEN cc.is_computed = 1 THEN CAST(0 AS BIT) ELSE cc.is_nullable END, -- een berekende kolom kan niet leeggemaakt worden: de rij hangt eraan en verdwijnt mee
               (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id = fk.object_id)
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        JOIN sys.columns cc ON cc.object_id = fkc.parent_object_id AND cc.column_id = fkc.parent_column_id
        JOIN sys.columns pc ON pc.object_id = fkc.referenced_object_id AND pc.column_id = fkc.referenced_column_id
        WHERE fk.referenced_object_id = OBJECT_ID(@tabel);
    OPEN c;
    FETCH NEXT FROM c INTO @fk, @child, @ccol, @pcol, @nullable, @aantal;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @aantal > 1 THROW 50002, N'Een verwijzing met meerdere kolommen werd gevonden; het script stopt veilig.', 1;
        SET @sub = @ccol + N' IN (SELECT ' + @pcol + N' FROM ' + @tabel + N' WHERE ' + @where + N')';
        -- staat de kolom in een CHECK-constraint van de kindtabel (bv. "een van deze eigenaars moet ingevuld zijn"), dan kan ze niet leeggemaakt worden: de rij hangt eraan en verdwijnt mee
        IF @nullable = 1 AND EXISTS (SELECT 1 FROM sys.check_constraints k WHERE k.parent_object_id = OBJECT_ID(@child) AND CHARINDEX(@ccol, k.definition) > 0) SET @nullable = 0;
        IF @nullable = 1
        BEGIN
            SET @sql = N'UPDATE ' + @child + N' SET ' + @ccol + N' = NULL WHERE ' + @sub;
            EXEC sp_executesql @sql;
            SET @n = @@ROWCOUNT;
            IF @n > 0 PRINT N'   ontkoppeld  ' + RIGHT(N'      ' + CAST(@n AS NVARCHAR(20)), 6) + N'  ' + @child + N'.' + @ccol;
        END
        ELSE IF @child <> @tabel
        BEGIN
            SET @volgende = @diepte + 1;
            EXEC #Ontkoppel @child, @sub, @volgende;
        END
        FETCH NEXT FROM c INTO @fk, @child, @ccol, @pcol, @nullable, @aantal;
    END
    CLOSE c; DEALLOCATE c;

    SET @sql = N'DELETE FROM ' + @tabel + N' WHERE ' + @where;
    EXEC sp_executesql @sql;
    SET @n = @@ROWCOUNT;
    IF @n > 0 PRINT N'   verwijderd  ' + RIGHT(N'      ' + CAST(@n AS NVARCHAR(20)), 6) + N'  ' + @tabel;
END
GO

SET XACT_ABORT ON;
DECLARE @Uitvoeren BIT = 0;       -- <<< 0 = proefrit (alles terugdraaien), 1 = echt bewaren
DECLARE @Project BIT = 1;         -- <<< projectsjablonen verwijderen
DECLARE @Planning BIT = 1;        -- <<< planningssjablonen verwijderen
DECLARE @Schijven BIT = 1;        -- <<< schijvensjablonen verwijderen
DECLARE @Traject BIT = 0;         -- <<< trajectsjablonen verwijderen (nu 0: ze blijven staan als bron voor de nieuwe sjablonen)

PRINT CASE WHEN @Uitvoeren = 1 THEN N'MODUS: UITVOEREN (wordt bewaard)' ELSE N'MODUS: PROEFRIT (wordt teruggedraaid)' END;
PRINT N'';
PRINT N'Wat er nu staat:';
SELECT N'Projectsjablonen' AS Soort, Id, Naam FROM ProjectSjabloon
UNION ALL SELECT N'Planningssjablonen', Id, Naam FROM PlanningSjabloon
UNION ALL SELECT N'Schijvensjablonen', Id, Naam FROM SchijfSjabloon
UNION ALL SELECT N'Trajectsjablonen', Id, Naam FROM TrajectSjabloon
ORDER BY 1, 3;

DECLARE @vTraject INT = (SELECT COUNT(*) FROM Projecttraject), @vMijlpaal INT = (SELECT COUNT(*) FROM Mijlpaal),
        @vTaak INT = (SELECT COUNT(*) FROM PlanningTaak), @vFase INT = (SELECT COUNT(*) FROM PlanningSectie),
        @vSchijf INT = (SELECT COUNT(*) FROM InvoicingPaymentStages), @vGroep INT = (SELECT COUNT(*) FROM InvoicingPaymentGroup);

BEGIN TRAN;

-- Eerst de projectsjablonen (die verwijzen naar de andere drie)
IF @Project = 1   BEGIN PRINT N'-- Projectsjablonen';  EXEC #Ontkoppel N'[dbo].[ProjectSjabloon]', N'1 = 1'; END
IF @Planning = 1  BEGIN PRINT N'-- Planningssjablonen'; EXEC #Ontkoppel N'[dbo].[PlanningSjabloon]', N'1 = 1'; END
IF @Schijven = 1  BEGIN PRINT N'-- Schijvensjablonen'; EXEC #Ontkoppel N'[dbo].[SchijfSjabloon]', N'1 = 1'; END
IF @Traject = 1   BEGIN PRINT N'-- Trajectsjablonen';  EXEC #Ontkoppel N'[dbo].[TrajectSjabloon]', N'1 = 1'; END

-- Veiligheidscontrole: geen projectdata mag verdwenen zijn
IF @vTraject <> (SELECT COUNT(*) FROM Projecttraject) OR @vMijlpaal <> (SELECT COUNT(*) FROM Mijlpaal)
   OR @vTaak <> (SELECT COUNT(*) FROM PlanningTaak) OR @vFase <> (SELECT COUNT(*) FROM PlanningSectie)
   OR @vSchijf <> (SELECT COUNT(*) FROM InvoicingPaymentStages) OR @vGroep <> (SELECT COUNT(*) FROM InvoicingPaymentGroup)
    THROW 50020, N'Er zou projectdata verdwijnen (trajecten, mijlpalen, planningtaken, fases of schijven). Alles is teruggedraaid; niets is verwijderd.', 1;

PRINT N'';
PRINT N'Wat er na afloop staat (projectdata blijft ongewijzigd):';
SELECT (SELECT COUNT(*) FROM ProjectSjabloon) AS Projectsjablonen, (SELECT COUNT(*) FROM PlanningSjabloon) AS Planningssjablonen,
       (SELECT COUNT(*) FROM SchijfSjabloon) AS Schijvensjablonen, (SELECT COUNT(*) FROM TrajectSjabloon) AS Trajectsjablonen,
       (SELECT COUNT(*) FROM Bouwstaat) AS BouwstatenBlijven;

IF @Uitvoeren = 1
BEGIN
    COMMIT;
    PRINT N'KLAAR: bewaard.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT N'PROEFRIT afgelopen: alles is teruggedraaid. Zet @Uitvoeren op 1 om het echt te doen.';
END
GO
