-- =============================================
-- Migratie: 046_UnitsKoppelmechanismenCheck
-- Datum: 2026-09-25
-- Omschrijving: Vier check-constraints op dbo.Units die de twee koppelmechanismen uit elkaar
--   houden. Achtergrond en meetresultaten in DESIGN.md, "De twee koppelmechanismen op Units".
--
--   A  Units.AttachedUnitId  → deze eenheid hangt ONDER een andere eenheid (een berging onder een
--                              lot). De eenheid houdt haar eigen naam, prijs, aandeel en status.
--                              Dit is het mechanisme waarop de eenhedenboom van
--                              Projecten/DetailUnitsV2 en Projecten/DetailV2 gebouwd is.
--   B  Units.IsLink = 1       → een EXTRA Units-rij die staat voor "deze eenheden worden samen
--      + Units.LinkedUnitId     verkocht"; de leden krijgen LinkedUnitId = die rij.
--
--   Eén eenheid in BEIDE mechanismen tegelijk laat haar prijs en aandeel dubbel meetellen (één keer
--   in het lot waaronder ze hangt, één keer in de samengestelde pseudo-eenheid). De toepassing
--   sluit dat sinds 25/09/2026 af aan beide kanten (GetUnitsForLinkSelect voor de koppeldialoog,
--   een blokkade in de EditUnit-POST voor de dropdown "Gekoppelde eenheid"), maar dat is een
--   applicatieregel — deze constraints maken er een databaseregel van, zodat ook een rechtstreekse
--   UPDATE of een toekomstig scherm de combinatie niet meer kan maken.
--
--   WAT BEWUST NIET GEBLOKKEERD WORDT: een KOPPELING-rij die zelf onder een lot hangt
--   (IsLink = 1 én AttachedUnitId gevuld). Dat is geen fout maar het normale gebruik — 7 van de 11
--   bestaande koppelingen zijn een parkeerpaar dat samen met een appartement verkocht wordt.
--
--   Nulmeting op db_ab5fbb_testdb (25/09/2026): alle vier de regels gaven 0 overtredingen, en er
--   stond nog geen enkele check-constraint op dbo.Units. Het script controleert dat hieronder
--   nogmaals per constraint en slaat een constraint over (met een PRINT) wanneer de doeldatabase
--   wél overtredingen bevat — zo kan deze migratie nooit halverwege stukvallen op een andere
--   omgeving. Idempotent via sys.check_constraints, zelfde conventie als de rest van deze map.
-- =============================================

-- ---- 1. Niet beide koppelmechanismen op dezelfde eenheid ----
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Units_NietBeideKoppelmechanismen')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Units WHERE AttachedUnitId IS NOT NULL AND LinkedUnitId IS NOT NULL)
        PRINT 'OVERGESLAGEN: CK_Units_NietBeideKoppelmechanismen — er zijn eenheden met zowel AttachedUnitId als LinkedUnitId. Draai eerst blok 4 van _migrations/INVENTARIS_Koppelingen_ReadOnly.sql en ruim die op.';
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Units] WITH CHECK
            ADD CONSTRAINT [CK_Units_NietBeideKoppelmechanismen]
            CHECK ([AttachedUnitId] IS NULL OR [LinkedUnitId] IS NULL);
        PRINT 'Constraint CK_Units_NietBeideKoppelmechanismen toegevoegd.';
    END
END
ELSE
    PRINT 'Constraint CK_Units_NietBeideKoppelmechanismen bestaat al, overgeslagen.';
GO

-- ---- 2. Een KOPPELING kan zelf geen lid zijn van een andere KOPPELING ----
-- Mogelijk omdat de pseudo-rij het TypeId van haar eerste lid overneemt en zo in de keuzelijst van
-- de oude dialoog opdook. De translator herschrijft bij elke save de naam en het aandeel van een
-- koppeling uit haar leden; nesten laat die herschrijving over zichzelf heen lopen.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Units_KoppelingNietGenest')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Units WHERE IsLink = 1 AND LinkedUnitId IS NOT NULL)
        PRINT 'OVERGESLAGEN: CK_Units_KoppelingNietGenest — er zijn geneste koppelingen. Zie blok 5 van het inventarisscript.';
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Units] WITH CHECK
            ADD CONSTRAINT [CK_Units_KoppelingNietGenest]
            CHECK ([IsLink] = 0 OR [LinkedUnitId] IS NULL);
        PRINT 'Constraint CK_Units_KoppelingNietGenest toegevoegd.';
    END
END
ELSE
    PRINT 'Constraint CK_Units_KoppelingNietGenest bestaat al, overgeslagen.';
GO

-- ---- 3 & 4. Geen eenheid die naar zichzelf verwijst ----
-- Puur een vangnet: een zelfverwijzing zou de 3-diepe boomwalk van
-- GetUnitsWithAttachedByProjectId in een cirkel duwen.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Units_AttachedNietZichzelf')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Units WHERE AttachedUnitId = Id)
        PRINT 'OVERGESLAGEN: CK_Units_AttachedNietZichzelf — er zijn eenheden die onder zichzelf hangen.';
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Units] WITH CHECK
            ADD CONSTRAINT [CK_Units_AttachedNietZichzelf]
            CHECK ([AttachedUnitId] IS NULL OR [AttachedUnitId] <> [Id]);
        PRINT 'Constraint CK_Units_AttachedNietZichzelf toegevoegd.';
    END
END
ELSE
    PRINT 'Constraint CK_Units_AttachedNietZichzelf bestaat al, overgeslagen.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Units_LinkedNietZichzelf')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Units WHERE LinkedUnitId = Id)
        PRINT 'OVERGESLAGEN: CK_Units_LinkedNietZichzelf — er zijn eenheden die lid zijn van zichzelf.';
    ELSE
    BEGIN
        ALTER TABLE [dbo].[Units] WITH CHECK
            ADD CONSTRAINT [CK_Units_LinkedNietZichzelf]
            CHECK ([LinkedUnitId] IS NULL OR [LinkedUnitId] <> [Id]);
        PRINT 'Constraint CK_Units_LinkedNietZichzelf toegevoegd.';
    END
END
ELSE
    PRINT 'Constraint CK_Units_LinkedNietZichzelf bestaat al, overgeslagen.';
GO

-- ---- Controle ----
SELECT name AS Constraint_, definition AS Definitie, is_disabled AS Uitgeschakeld, is_not_trusted AS NietVertrouwd
FROM   sys.check_constraints
WHERE  parent_object_id = OBJECT_ID('dbo.Units')
ORDER BY name;
GO

-- ---- Terugdraaien, indien ooit nodig ----
-- ALTER TABLE [dbo].[Units] DROP CONSTRAINT [CK_Units_NietBeideKoppelmechanismen];
-- ALTER TABLE [dbo].[Units] DROP CONSTRAINT [CK_Units_KoppelingNietGenest];
-- ALTER TABLE [dbo].[Units] DROP CONSTRAINT [CK_Units_AttachedNietZichzelf];
-- ALTER TABLE [dbo].[Units] DROP CONSTRAINT [CK_Units_LinkedNietZichzelf];
