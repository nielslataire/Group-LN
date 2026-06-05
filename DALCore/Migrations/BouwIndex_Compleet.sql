-- =============================================
-- BouwIndex uitbreiding: S-index, I2021, ABEX
-- Sprint 8 - Bouwindexen beheer
-- =============================================

-- 1. Verwijder bestaande CHECK constraint op IndexType (als die bestaat)
DECLARE @chk NVARCHAR(200)
SELECT @chk = cc.name
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
WHERE t.name = 'BouwIndex' AND cc.definition LIKE '%IndexType%'
IF @chk IS NOT NULL
    EXEC('ALTER TABLE BouwIndex DROP CONSTRAINT [' + @chk + ']')

-- 2. Verwijder bestaande UNIQUE constraint (als die bestaat)
DECLARE @uq NVARCHAR(200)
SELECT @uq = kc.name
FROM sys.key_constraints kc
INNER JOIN sys.tables t ON kc.parent_object_id = t.object_id
WHERE t.name = 'BouwIndex' AND kc.type = 'UQ'
IF @uq IS NOT NULL
    EXEC('ALTER TABLE BouwIndex DROP CONSTRAINT [' + @uq + ']')

-- 3. Vergroot IndexType kolom naar NVARCHAR(10) voor 'S', 'I', 'ABEX'
ALTER TABLE BouwIndex ALTER COLUMN IndexType NVARCHAR(10) NOT NULL;

-- 4. Maak Jaar nullable (ABEX-rijen kunnen een expliciete jaar hebben, maar nieuwe types ook)
ALTER TABLE BouwIndex ALTER COLUMN Jaar INT NULL;

-- 5. Voeg nieuwe kolommen toe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='BouwIndex' AND COLUMN_NAME='GeldigVanaf')
    ALTER TABLE BouwIndex ADD GeldigVanaf DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='BouwIndex' AND COLUMN_NAME='Categorie')
    ALTER TABLE BouwIndex ADD Categorie NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='BouwIndex' AND COLUMN_NAME='SubCategorie')
    ALTER TABLE BouwIndex ADD SubCategorie NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='BouwIndex' AND COLUMN_NAME='Bron')
    ALTER TABLE BouwIndex ADD Bron NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='BouwIndex' AND COLUMN_NAME='Opmerking')
    ALTER TABLE BouwIndex ADD Opmerking NVARCHAR(500) NULL;

-- 6. Seed ABEX data via dynamic SQL zodat de parser niet klaagt over nieuwe kolommen
IF NOT EXISTS (SELECT * FROM BouwIndex WHERE IndexType = 'ABEX')
BEGIN
    EXEC('
        INSERT INTO BouwIndex (IndexType, Jaar, Maand, IndexWaarde, IsActief, Bron) VALUES
        (''ABEX'', 2018, NULL, 735.00, 0, ''ABEX handmatig''),
        (''ABEX'', 2019, NULL, 748.00, 0, ''ABEX handmatig''),
        (''ABEX'', 2020, NULL, 762.00, 0, ''ABEX handmatig''),
        (''ABEX'', 2021, NULL, 800.00, 0, ''ABEX handmatig''),
        (''ABEX'', 2022, NULL, 870.00, 0, ''ABEX handmatig''),
        (''ABEX'', 2023, NULL, 910.00, 0, ''ABEX handmatig''),
        (''ABEX'', 2024, NULL, 940.00, 0, ''ABEX handmatig''),
        (''ABEX'', 2025, NULL, 963.00, 1, ''ABEX handmatig'')
    ')
END

-- 7. Voeg een gewone (niet-unieke) index toe voor snelle filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_BouwIndex_Type_Jaar_Maand' AND object_id = OBJECT_ID('BouwIndex'))
    CREATE INDEX IX_BouwIndex_Type_Jaar_Maand ON BouwIndex (IndexType, Jaar, Maand);
