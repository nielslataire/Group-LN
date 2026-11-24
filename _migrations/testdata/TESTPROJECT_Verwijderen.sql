-- ============================================================================================
-- TESTPROJECT_Verwijderen.sql  (GEEN genummerde migratie: enkel testgegevens)
-- Verwijdert testprojecten VOLLEDIG: het project zelf en alles wat eraan hangt (eenheden, planning, traject, schijven, contracten,
-- facturen, verzekeringen, documenten, taken, ...), plus de klanten die enkel bij dat project hoorden en de fictieve partijen
-- die TESTPROJECT_Aanmaken.sql voor dat project maakte.
--
-- WIE WORDT VERWIJDERD (zie @Codes en @Ook79 hieronder):
--   * elk project waarvan de ProjectCode in @Codes staat (standaard alle "TP-%": de twee testprojecten van TESTPROJECT_Aanmaken.sql)
--   * project 79 ("TEST ...") als @Ook79 = 1. Voorzorg: de naam moet met "TEST" beginnen, anders stopt het script.
--   Andere projecten kunnen NOOIT verwijderd worden met dit script.
--
-- HOE: eerst @Uitvoeren = 0 (proefrit: alles wordt uitgevoerd en teruggedraaid, je ziet per tabel hoeveel rijen verdwijnen),
-- daarna @Uitvoeren = 1. Maak voor de zekerheid altijd eerst een back-up van de database.
--
-- WERKING: alle verwijzingen naar het project worden gevolgd via de foreign keys. Een verplichte verwijzing betekent "hangt eraan" en
-- verdwijnt mee; een optionele verwijzing wordt leeggemaakt, BEHALVE een kolom met de naam ProjectId op de projecttabel zelf: die
-- hoort altijd bij het project en verdwijnt ook (bv. foto's, verzekeringen).
-- NIET opgeruimd: bestanden in de opslag (geüploade foto's/documenten op schijf of in Storage); die verwijs je apart.
-- ============================================================================================
SET NOCOUNT ON;
GO

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
        -- een optionele ProjectId-kolom rechtstreeks naar het project hoort bij het project: meeverwijderen, niet leegmaken
        IF @nullable = 1 AND @tabel = N'[dbo].[Project]' AND @ccol = N'[ProjectId]' SET @nullable = 0;
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
DECLARE @Uitvoeren BIT = 0;                  -- <<< 0 = proefrit (alles terugdraaien), 1 = echt bewaren
DECLARE @Ook79 BIT = 0;                      -- <<< 1 = verwijder ook het oude testproject 79 (naam moet met TEST beginnen)
DECLARE @CodePatroon NVARCHAR(30) = N'TP-%'; -- <<< bv. N'TP-APP-01' om enkel het appartementsproject te verwijderen

-- Welke projecten?
DECLARE @doel TABLE (ProjectId INT PRIMARY KEY, Naam NVARCHAR(200), Code NVARCHAR(30) NULL);
INSERT INTO @doel (ProjectId, Naam, Code)
SELECT ProjectId, ProjectName, ProjectCode FROM dbo.Project WHERE ProjectCode LIKE @CodePatroon AND ProjectCode LIKE N'TP-%';
IF @Ook79 = 1
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Project WHERE ProjectId = 79 AND ProjectName LIKE N'TEST%')
        THROW 50011, N'Project 79 bestaat niet of de naam begint niet met TEST: het script stopt uit voorzorg.', 1;
    IF NOT EXISTS (SELECT 1 FROM @doel WHERE ProjectId = 79)
        INSERT INTO @doel (ProjectId, Naam, Code) SELECT ProjectId, ProjectName, ProjectCode FROM dbo.Project WHERE ProjectId = 79;
END
IF NOT EXISTS (SELECT 1 FROM @doel) BEGIN PRINT N'Geen testprojecten gevonden, niets gedaan.'; RETURN; END

PRINT CASE WHEN @Uitvoeren = 1 THEN N'MODUS: UITVOEREN (wordt bewaard)' ELSE N'MODUS: PROEFRIT (wordt teruggedraaid)' END;
PRINT N'Te verwijderen projecten:';
SELECT ProjectId, Naam, Code FROM @doel;

BEGIN TRAN;

-- Het ondertekeningslogboek (SigningEvent) is append-only: een trigger weigert wijzigen en verwijderen. Enkel voor project 79 (door Niels op 24/11/2026
-- uitdrukkelijk toegestaan) zetten we ze tijdelijk uit. Dat gebeurt binnen deze transactie (een proefrit zet ze dus vanzelf terug) en het script
-- zet ze zelf weer aan vóór het bewaren. Voor de TP-testprojecten gebeurt dit NIET.
DECLARE @triggerUit NVARCHAR(300) = NULL;
IF @Ook79 = 1 AND EXISTS (SELECT 1 FROM @doel WHERE ProjectId = 79)
    SELECT @triggerUit = QUOTENAME(OBJECT_SCHEMA_NAME(t.parent_id)) + N'.' + QUOTENAME(OBJECT_NAME(t.parent_id)) FROM sys.triggers t WHERE t.name = N'TR_SigningEvent_AppendOnly' AND t.is_disabled = 0;
IF @triggerUit IS NOT NULL EXEC (N'DISABLE TRIGGER [TR_SigningEvent_AppendOnly] ON ' + @triggerUit);

-- Klanten die enkel bij deze projecten horen (vóór het verwijderen van de eenheden vastleggen)
DECLARE @klanten TABLE (Id INT PRIMARY KEY);
INSERT INTO @klanten (Id)
SELECT DISTINCT u.ClientAccountId FROM dbo.Units u
WHERE u.ProjectId IN (SELECT ProjectId FROM @doel) AND u.ClientAccountId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Units o WHERE o.ClientAccountId = u.ClientAccountId AND o.ProjectId NOT IN (SELECT ProjectId FROM @doel));

-- Fictieve partijen van de testprojecten (alleen via de marker, nooit echte bedrijven)
DECLARE @partijen TABLE (Id INT PRIMARY KEY);
INSERT INTO @partijen (Id)
SELECT c.CompanyId FROM dbo.CompanyInfo c JOIN @doel d ON c.Opmerkingen = N'TESTPROJECT-DATA ' + d.Code WHERE d.Code IS NOT NULL;

-- Projecten (en alles wat eraan hangt), één per keer
DECLARE @pid INT, @w NVARCHAR(200);
DECLARE cp CURSOR LOCAL FAST_FORWARD FOR SELECT ProjectId FROM @doel ORDER BY ProjectId;
OPEN cp; FETCH NEXT FROM cp INTO @pid;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT N'-- project ' + CAST(@pid AS NVARCHAR(10));
    SET @w = N'[ProjectID] = ' + CAST(@pid AS NVARCHAR(20));
    EXEC #Ontkoppel N'[dbo].[Project]', @w;
    FETCH NEXT FROM cp INTO @pid;
END
CLOSE cp; DEALLOCATE cp;

-- Klanten die nergens anders meer voorkomen
PRINT N'-- klanten van het project';
DECLARE @kid INT;
DECLARE ck CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM @klanten;
OPEN ck; FETCH NEXT FROM ck INTO @kid;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @w = N'[Id] = ' + CAST(@kid AS NVARCHAR(20));
    EXEC #Ontkoppel N'[dbo].[ClientAccount]', @w;
    FETCH NEXT FROM ck INTO @kid;
END
CLOSE ck; DEALLOCATE ck;

-- Fictieve partijen
PRINT N'-- fictieve partijen';
DECLARE @bid INT;
DECLARE cb CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM @partijen;
OPEN cb; FETCH NEXT FROM cb INTO @bid;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @w = N'[CompanyId] = ' + CAST(@bid AS NVARCHAR(20));
    EXEC #Ontkoppel N'[dbo].[CompanyInfo]', @w;
    FETCH NEXT FROM cb INTO @bid;
END
CLOSE cb; DEALLOCATE cb;

IF @triggerUit IS NOT NULL EXEC (N'ENABLE TRIGGER [TR_SigningEvent_AppendOnly] ON ' + @triggerUit);

PRINT N'';
PRINT N'Controle (moet overal 0 zijn):';
SELECT (SELECT COUNT(*) FROM dbo.Project WHERE ProjectId IN (SELECT ProjectId FROM @doel)) AS Projecten,
       (SELECT COUNT(*) FROM dbo.Units WHERE ProjectId IN (SELECT ProjectId FROM @doel)) AS Eenheden,
       (SELECT COUNT(*) FROM dbo.ClientAccount WHERE Id IN (SELECT Id FROM @klanten)) AS Klanten,
       (SELECT COUNT(*) FROM dbo.CompanyInfo WHERE CompanyId IN (SELECT Id FROM @partijen)) AS Partijen;

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
