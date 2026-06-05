-- =============================================
-- Vervang ABEX door S/I bouwindex
-- =============================================

-- 1. Nieuwe BouwIndex tabel (historiek S en I indexen)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BouwIndex' AND xtype='U')
CREATE TABLE BouwIndex (
    Id          INT IDENTITY PRIMARY KEY,
    IndexType   NVARCHAR(1)    NOT NULL CHECK (IndexType IN ('S','I')),
    Jaar        INT            NOT NULL,
    Maand       INT            NULL,
    IndexWaarde DECIMAL(10,4)  NOT NULL,
    IsActief    BIT            NOT NULL DEFAULT 0,
    CONSTRAINT UQ_BouwIndex UNIQUE (IndexType, Jaar, Maand)
);

-- Seed startwaarden
IF NOT EXISTS (SELECT * FROM BouwIndex)
BEGIN
    INSERT INTO BouwIndex (IndexType, Jaar, Maand, IndexWaarde, IsActief) VALUES
    ('S', 2020, NULL, 100.0000, 0),
    ('S', 2021, NULL, 103.5000, 0),
    ('S', 2022, NULL, 108.2000, 0),
    ('S', 2023, NULL, 112.8000, 0),
    ('S', 2024, NULL, 116.4000, 0),
    ('S', 2025, NULL, 119.1000, 1),
    ('I', 2020, NULL, 100.0000, 0),
    ('I', 2021, NULL, 108.7000, 0),
    ('I', 2022, NULL, 124.3000, 0),
    ('I', 2023, NULL, 119.6000, 0),
    ('I', 2024, NULL, 121.8000, 0),
    ('I', 2025, NULL, 123.5000, 1);
END

-- 2. Verwijder ABEX kolommen uit BudgetGegevens
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_NAME='BudgetGegevens' AND COLUMN_NAME='ABEXBasisIndex')
    ALTER TABLE BudgetGegevens DROP COLUMN ABEXBasisIndex;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_NAME='BudgetGegevens' AND COLUMN_NAME='ABEXHuidigIndex')
    ALTER TABLE BudgetGegevens DROP COLUMN ABEXHuidigIndex;

-- 3. Voeg S/I index kolommen toe aan BudgetGegevens
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='BudgetGegevens' AND COLUMN_NAME='SIndexStart')
    ALTER TABLE BudgetGegevens ADD SIndexStart DECIMAL(10,4) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='BudgetGegevens' AND COLUMN_NAME='SIndexHuidig')
    ALTER TABLE BudgetGegevens ADD SIndexHuidig DECIMAL(10,4) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='BudgetGegevens' AND COLUMN_NAME='IIndexStart')
    ALTER TABLE BudgetGegevens ADD IIndexStart DECIMAL(10,4) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='BudgetGegevens' AND COLUMN_NAME='IIndexHuidig')
    ALTER TABLE BudgetGegevens ADD IIndexHuidig DECIMAL(10,4) NULL;

-- 4. Verwijder oude ABEXIndex tabel als die bestaat
IF EXISTS (SELECT * FROM sysobjects WHERE name='ABEXIndex' AND xtype='U')
    DROP TABLE ABEXIndex;
