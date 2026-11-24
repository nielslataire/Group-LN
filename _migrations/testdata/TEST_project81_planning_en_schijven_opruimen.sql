-- =============================================
-- TEST_project81_planning_en_schijven_opruimen.sql  (GEEN genummerde migratie: enkel voor het testproject)
-- Maakt de planning en de schijven van één testproject (TP-...) leeg, zodat je het met je echte sjablonen opnieuw kan opbouwen.
--
-- WAT VERDWIJNT:
--   * alle plannings (hoofdplanning en extra), hun fasen, taken, voorgangers, eenheden en schijfkoppelingen
--   * alle schijven van alle betalingsgroepen (ook de factureerbare), met de "bereikt"-markeringen van de eenheden
--   * het traject met fasen en mijlpalen (als er een is)
--   * de systeemtaken "Schijf X is bereikt" in Mijn taken
-- WAT BLIJFT STAAN:
--   * het project, de eenheden (met waarden, verkoop en klanten), de BETALINGSGROEPEN zelf en welke eenheid in welke groep zit, contracten, budget
--   * facturen (hun regels verliezen enkel de koppeling met de schijf)
--   * alle sjablonen en instellingen
--
-- HOE: 1. @ProjectId hieronder juist zetten (nu 81).  2. Draai zoals het is: PROEFRIT, alles wordt teruggedraaid en je ziet per tabel hoeveel
-- rijen verdwijnen.  3. Klopt het? Zet @Uitvoeren op 1 en draai opnieuw.
-- Veiligheid: de projectcode moet met "TP-" beginnen (testprojecten uit TESTPROJECT_Aanmaken.sql), anders stopt het script.
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

DECLARE @ProjectId INT = 81;     -- <<< het testproject
DECLARE @Uitvoeren BIT = 0;      -- <<< 0 = proefrit (alles terugdraaien), 1 = echt bewaren

DECLARE @naam NVARCHAR(100) = (SELECT ProjectName FROM Project WHERE ProjectID = @ProjectId);
DECLARE @pcode NVARCHAR(30) = (SELECT ProjectCode FROM Project WHERE ProjectID = @ProjectId);
IF @naam IS NULL THROW 50010, N'Project niet gevonden.', 1;
IF @pcode IS NULL OR @pcode NOT LIKE N'TP-%' THROW 50011, N'De projectcode begint niet met TP-: het script stopt uit voorzorg.', 1;

PRINT N'Project ' + CAST(@ProjectId AS NVARCHAR(20)) + N': ' + @naam + N' (' + @pcode + N')';
PRINT CASE WHEN @Uitvoeren = 1 THEN N'MODUS: UITVOEREN (wordt bewaard)' ELSE N'MODUS: PROEFRIT (wordt teruggedraaid)' END;
PRINT N'';
PRINT N'Betalingsgroepen en aantal schijven (de groepen blijven, de schijven verdwijnen):';
SELECT g.Id, g.Name, (SELECT COUNT(*) FROM InvoicingPaymentStages s WHERE s.GroupId = g.Id) AS Schijven,
       (SELECT COUNT(*) FROM InvoicingPaymentStages s WHERE s.GroupId = g.Id AND s.Invoicable = 1) AS Factureerbaar
FROM InvoicingPaymentGroup g WHERE g.ProjectId = @ProjectId ORDER BY g.Id;

SET XACT_ABORT ON;
BEGIN TRAN;

PRINT N'';
PRINT N'-- Systeemtaken "schijf bereikt"';
DELETE FROM ProjectTaak WHERE ProjectId = @ProjectId AND BronSleutel LIKE N'schijf:%';
PRINT N'   verwijderd  ' + RIGHT(N'      ' + CAST(@@ROWCOUNT AS NVARCHAR(20)), 6) + N'  ProjectTaak (schijf)';

PRINT N'-- Schijfkoppelingen van de planning';
DELETE FROM PlanningSchijfKoppeling WHERE BouwPlanningId IN (SELECT Id FROM BouwPlanning WHERE ProjectId = @ProjectId);
PRINT N'   verwijderd  ' + RIGHT(N'      ' + CAST(@@ROWCOUNT AS NVARCHAR(20)), 6) + N'  PlanningSchijfKoppeling';

PRINT N'-- Planning (fasen en taken, ook van de hoofdplanning)';
DECLARE @w NVARCHAR(200) = N'[ProjectId] = ' + CAST(@ProjectId AS NVARCHAR(20));
EXEC #Ontkoppel N'[dbo].[PlanningSectie]', @w;
EXEC #Ontkoppel N'[dbo].[BouwPlanning]', @w;

PRINT N'-- Traject';
EXEC #Ontkoppel N'[dbo].[Projecttraject]', @w;

PRINT N'-- Schijven van de betalingsgroepen (de groepen blijven)';
DECLARE @ws NVARCHAR(400) = N'[GroupId] IN (SELECT [Id] FROM [dbo].[InvoicingPaymentGroup] WHERE [ProjectId] = ' + CAST(@ProjectId AS NVARCHAR(20)) + N')';
EXEC #Ontkoppel N'[dbo].[InvoicingPaymentStages]', @ws;

PRINT N'';
PRINT N'Controle (schijven, planning en traject moeten 0 zijn; groepen en eenheden blijven):';
SELECT
    (SELECT COUNT(*) FROM PlanningSectie WHERE ProjectId = @ProjectId) AS PlanningFasen,
    (SELECT COUNT(*) FROM BouwPlanning WHERE ProjectId = @ProjectId) AS Plannings,
    (SELECT COUNT(*) FROM Projecttraject WHERE ProjectId = @ProjectId) AS Trajecten,
    (SELECT COUNT(*) FROM InvoicingPaymentStages s JOIN InvoicingPaymentGroup g ON g.Id = s.GroupId WHERE g.ProjectId = @ProjectId) AS Schijven,
    (SELECT COUNT(*) FROM InvoicingPaymentGroup WHERE ProjectId = @ProjectId) AS GroepenDieBlijven,
    (SELECT COUNT(*) FROM Units WHERE ProjectId = @ProjectId) AS EenhedenDieBlijven;

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
