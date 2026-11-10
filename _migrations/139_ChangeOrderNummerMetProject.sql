-- =============================================
-- Migratie: 139_ChangeOrderNummerMetProject
-- Omschrijving: Offertes en wijzigingsopdrachten krijgen één doorlopende nummering per project en jaar
--   (in volgorde van aanmaken) en het nummer toont het project: "OF-RZ-2026-014" / "WO-RZ-2026-014-v2".
--   - Nieuwe kolom ChangeOrder.NumberProjectTag: afkorting van de projectnaam, vastgelegd bij het nummeren
--     (wijzigt dus niet meer als het project hernoemd wordt).
--   - EENMALIGE herschikking van alle bestaande nummers op Id-volgorde (= volgorde van aanmaken), over
--     offertes én wijzigingsopdrachten heen. Dezelfde regels als DALCore/ChangeOrderNumbering:
--       * nieuwe versie (SourceKind 2): zelfde jaar/teller als het document, VersionNo + 1;
--       * omgezette offerte (SourceKind 3): de wijzigingsopdracht krijgt het nummer van de offerte (OF -> WO),
--         tenzij dat nummer al een WO heeft; dan een nieuw nummer;
--       * anders: volgende vrije teller van het project in het jaar van ChangeOrder.Date.
--   WAARSCHUWING: nummers die al op verzonden PDF's, mails of facturen staan veranderen hierdoor.
--   De herschikking draait alleen als er nog geen enkele rij een projectafkorting heeft (dus één keer).
--   Afkorting: eerste letter van de eerste 4 woorden van de projectnaam (lidwoorden/voorzetsels als
--   "de", "het", "van" tellen niet mee); bij één woord de eerste 3 letters; leeg -> "P" + projectId.
-- =============================================

IF COL_LENGTH('dbo.ChangeOrder', 'NumberProjectTag') IS NULL
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD [NumberProjectTag] NVARCHAR(20) NULL;
    PRINT 'Kolom ChangeOrder.NumberProjectTag toegevoegd.';
END
ELSE PRINT 'Kolom ChangeOrder.NumberProjectTag bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[ChangeOrder] WHERE [NumberProjectTag] IS NOT NULL)
   AND EXISTS (SELECT 1 FROM [dbo].[ChangeOrder])
BEGIN
    SET NOCOUNT ON;

    -- 1. Alles ontnummeren (de unieke index negeert rijen zonder NumberSeq) en het project vastzetten.
    UPDATE [dbo].[ChangeOrder]
    SET NumberYear = NULL, NumberSeq = NULL, RootChangeOrderId = NULL, VersionNo = 1,
        NumberProjectId = COALESCE(NULLIF(ProjectId, 0), NumberProjectId);

    -- 2. Afkorting per project.
    CREATE TABLE #tag (ProjectId INT PRIMARY KEY, Tag NVARCHAR(20) NOT NULL);
    DECLARE @pid INT, @name NVARCHAR(400), @i INT, @len INT, @c NCHAR(1), @word NVARCHAR(100),
            @tag NVARCHAR(20), @words INT, @first NVARCHAR(100);
    DECLARE pc CURSOR LOCAL FAST_FORWARD FOR
        SELECT p.ProjectId, ISNULL(p.ProjectName, N'')
        FROM [dbo].[Project] p
        WHERE p.ProjectId IN (SELECT DISTINCT NumberProjectId FROM [dbo].[ChangeOrder] WHERE NumberProjectId IS NOT NULL);
    OPEN pc;
    FETCH NEXT FROM pc INTO @pid, @name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @tag = N'', @words = 0, @word = N'', @first = N'', @i = 1;
        SET @name = @name + N' ';
        SET @len = LEN(@name + N'x') - 1;
        WHILE @i <= @len
        BEGIN
            SET @c = SUBSTRING(@name, @i, 1);
            IF @c COLLATE Latin1_General_BIN LIKE N'[A-Za-z0-9]'
                SET @word = @word + @c;
            ELSE IF @word <> N''
            BEGIN
                IF LOWER(@word) NOT IN (N'de', N'het', N'een', N'van', N'der', N'den', N'en', N'te', N't', N'la', N'le')
                BEGIN
                    SET @words = @words + 1;
                    IF @words = 1 SET @first = @word;
                    IF @words <= 4 SET @tag = @tag + UPPER(LEFT(@word, 1));
                END
                SET @word = N'';
            END
            SET @i = @i + 1;
        END
        IF @words = 1 SET @tag = UPPER(LEFT(@first, 3));
        IF @tag = N'' SET @tag = N'P' + CAST(@pid AS NVARCHAR(20));
        INSERT INTO #tag (ProjectId, Tag) VALUES (@pid, @tag);
        FETCH NEXT FROM pc INTO @pid, @name;
    END
    CLOSE pc; DEALLOCATE pc;

    -- 3. Hernummeren op Id-volgorde.
    CREATE TABLE #seq (ProjectId INT NOT NULL, Yr INT NOT NULL, Mx INT NOT NULL, PRIMARY KEY (ProjectId, Yr));
    DECLARE @id INT, @proj INT, @isq BIT, @date DATE, @sk TINYINT, @src INT, @done BIT,
            @sy INT, @ss INT, @sroot INT, @root INT, @ver INT, @yr INT, @mx INT;
    DECLARE oc CURSOR LOCAL FAST_FORWARD FOR
        SELECT Id, NumberProjectId, IsQuote, [Date], ISNULL(SourceKind, 0), SourceChangeOrderId
        FROM [dbo].[ChangeOrder]
        ORDER BY Id;
    OPEN oc;
    FETCH NEXT FROM oc INTO @id, @proj, @isq, @date, @sk, @src;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @done = CASE WHEN @proj IS NULL THEN 1 ELSE 0 END;

        -- nieuwe versie van een document
        IF @done = 0 AND @sk = 2 AND @src IS NOT NULL
        BEGIN
            SELECT @sy = NumberYear, @ss = NumberSeq, @sroot = RootChangeOrderId FROM [dbo].[ChangeOrder] WHERE Id = @src;
            IF @ss IS NOT NULL
            BEGIN
                SET @root = ISNULL(@sroot, @src);
                SELECT @ver = ISNULL(MAX(VersionNo), 1) + 1 FROM [dbo].[ChangeOrder]
                WHERE (Id = @root OR RootChangeOrderId = @root) AND NumberSeq IS NOT NULL;
                UPDATE [dbo].[ChangeOrder]
                SET NumberYear = @sy, NumberSeq = @ss, VersionNo = @ver, RootChangeOrderId = @root
                WHERE Id = @id;
                SET @done = 1;
            END
        END

        -- offerte omgezet naar WO: zelfde nummer
        IF @done = 0 AND @sk = 3 AND @src IS NOT NULL
        BEGIN
            SELECT @sy = NumberYear, @ss = NumberSeq FROM [dbo].[ChangeOrder] WHERE Id = @src;
            IF @ss IS NOT NULL AND NOT EXISTS (
                SELECT 1 FROM [dbo].[ChangeOrder]
                WHERE NumberProjectId = @proj AND IsQuote = 0 AND NumberYear = @sy AND NumberSeq = @ss AND VersionNo = 1)
            BEGIN
                UPDATE [dbo].[ChangeOrder] SET NumberYear = @sy, NumberSeq = @ss, VersionNo = 1, RootChangeOrderId = NULL WHERE Id = @id;
                SET @done = 1;
            END
        END

        -- nieuw nummer uit de gedeelde teller
        IF @done = 0
        BEGIN
            SET @yr = YEAR(ISNULL(@date, GETDATE()));
            SET @mx = NULL;
            SELECT @mx = Mx FROM #seq WHERE ProjectId = @proj AND Yr = @yr;
            IF @mx IS NULL
            BEGIN
                SET @mx = 1;
                INSERT INTO #seq (ProjectId, Yr, Mx) VALUES (@proj, @yr, 1);
            END
            ELSE
            BEGIN
                SET @mx = @mx + 1;
                UPDATE #seq SET Mx = @mx WHERE ProjectId = @proj AND Yr = @yr;
            END
            UPDATE [dbo].[ChangeOrder] SET NumberYear = @yr, NumberSeq = @mx, VersionNo = 1, RootChangeOrderId = NULL WHERE Id = @id;
        END

        FETCH NEXT FROM oc INTO @id, @proj, @isq, @date, @sk, @src;
    END
    CLOSE oc; DEALLOCATE oc;

    -- 4. Afkorting vastleggen.
    UPDATE co SET co.NumberProjectTag = t.Tag
    FROM [dbo].[ChangeOrder] co JOIN #tag t ON t.ProjectId = co.NumberProjectId;

    DROP TABLE #seq;
    DROP TABLE #tag;
    PRINT 'Bestaande offertes/wijzigingsopdrachten hernummerd op volgorde van aanmaken, met projectafkorting.';
END
ELSE PRINT 'Hernummering al uitgevoerd (of geen rijen), overgeslagen.';
GO
