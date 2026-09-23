-- =============================================
-- Migratie: 044_KlantPrimairContactEnEmailOptioneel
-- Datum: 2026-09-23
-- Omschrijving: Twee samenhangende wijzigingen voor het klantenaccount-e-mailadres:
--   1. ClientContacts.IsPrimaryContact (BIT, default 0) — vlag om één contactpersoon als
--      "primair" aan te duiden (Klanten Edit/Create/EditProject). Zelfde soort losse vlag als
--      het al bestaande IsCoOwner, GEEN unieke-index-afdwinging op DB-niveau: de applicatie
--      (ClientService, bij het opslaan van een klant) garandeert dat er maximaal één primair
--      contact per klant is, dezelfde "applicatie normaliseert, DB blijft simpel"-aanpak als
--      elders in dit schema.
--   2. ClientAccounts.Email en ClientContacts.Email worden NULL toegestaan indien ze dat nog
--      niet waren — het e-mailadres wordt voortaan optioneel in te vullen op alle drie de
--      klantformulieren (Edit/Create/EditProject). Leest het huidige kolomtype dynamisch uit
--      sys.columns i.p.v. een lengte te gokken (bv. NVARCHAR(100) vs (255)) — een verkeerd
--      gegokte lengte zou hier bestaande, langere waarden stilzwijgend kunnen afkappen.
--   Strikt additief + idempotent.
-- =============================================

IF COL_LENGTH('dbo.ClientContacts', 'IsPrimaryContact') IS NULL
BEGIN
    ALTER TABLE [dbo].[ClientContacts] ADD [IsPrimaryContact] BIT NOT NULL CONSTRAINT DF_ClientContacts_IsPrimaryContact DEFAULT (0);
    PRINT 'Kolom ClientContacts.IsPrimaryContact toegevoegd.';
END
ELSE
    PRINT 'Kolom ClientContacts.IsPrimaryContact bestaat al, overgeslagen.';

DECLARE @sql NVARCHAR(MAX);
DECLARE @table NVARCHAR(128);
DECLARE @tables TABLE (TableName NVARCHAR(128));
INSERT INTO @tables (TableName) VALUES ('ClientAccounts'), ('ClientContacts');

DECLARE tbl_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName FROM @tables;
OPEN tbl_cursor;
FETCH NEXT FROM tbl_cursor INTO @table;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        WHERE t.name = @table AND c.name = 'Email' AND c.is_nullable = 0
    )
    BEGIN
        SELECT @sql =
            N'ALTER TABLE [dbo].[' + @table + N'] ALTER COLUMN [Email] ' +
            TYPE_NAME(c.user_type_id) +
            CASE
                WHEN TYPE_NAME(c.user_type_id) IN ('nvarchar', 'nchar')
                    THEN N'(' + CASE WHEN c.max_length = -1 THEN N'MAX' ELSE CAST(c.max_length / 2 AS NVARCHAR(10)) END + N')'
                WHEN TYPE_NAME(c.user_type_id) IN ('varchar', 'char')
                    THEN N'(' + CASE WHEN c.max_length = -1 THEN N'MAX' ELSE CAST(c.max_length AS NVARCHAR(10)) END + N')'
                ELSE N''
            END + N' NULL'
        FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        WHERE t.name = @table AND c.name = 'Email';

        EXEC sp_executesql @sql;
        PRINT 'Kolom ' + @table + '.Email staat nu NULL toe.';
    END
    ELSE
        PRINT 'Kolom ' + @table + '.Email staat al NULL toe (of bestaat niet), overgeslagen.';

    FETCH NEXT FROM tbl_cursor INTO @table;
END
CLOSE tbl_cursor;
DEALLOCATE tbl_cursor;

PRINT 'Migratie 044_KlantPrimairContactEnEmailOptioneel voltooid.';
