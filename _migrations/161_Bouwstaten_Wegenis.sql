-- =============================================
-- 161 (25/11/2026): bouwstaten voor de wegenis van een verkaveling (stap 1 van de verkaveling met woningen).
--   Groep "Wegenis", bedrijfsbreed, bereik gebouw (0) = geldt voor het hele project, niet per eenheid, niet per bouwlaag.
--   Ze dienen als koppelpunt tussen: planningstaken van de wegenis -> schijven -> trajectmijlpalen, en als startvoorwaarde voor de woningen
--   ("woningen starten na: onmiddellijk / funderingen wegenis / nutsvoorzieningen / voorlopige oplevering wegenis").
--   Idempotent: enkel wat nog niet bestaat (op code). Naam, groep en volgorde kan je achteraf aanpassen in Instellingen > Bouwstaten; enkel de code ligt vast.
--   Bestaande bouwstaten die ook bij de wegenis passen worden hergebruikt: WERF_UITGEZET (loten uitgezet door de landmeter) en RIOLERING.
-- =============================================
SET NOCOUNT ON;

DECLARE @nieuw TABLE (Code NVARCHAR(60), Naam NVARCHAR(200), Groep NVARCHAR(100), Volgorde INT, Bereik TINYINT, PerBouwlaag BIT);
INSERT INTO @nieuw (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES
    (N'WEGENIS_GRONDWERKEN',        N'Grondwerken wegenis klaar (ontbossing, ophoging, uitgraven wegenbed)', N'Wegenis', 100, 0, 0),
    (N'WEGENIS_RIOLERING',          N'Riolering en regenwaterafvoer wegenis geplaatst',                     N'Wegenis', 101, 0, 0),
    (N'WEGENIS_FUNDERING',          N'Wegenfundering aangelegd',                                            N'Wegenis', 102, 0, 0),
    (N'WEGENIS_NUTSVOORZIENINGEN',  N'Nutsvoorzieningen aangelegd (water, elektriciteit, gas, telecom)',    N'Wegenis', 103, 0, 0),
    (N'WEGENIS_BOORDEN',            N'Boordstenen en kantstroken geplaatst',                                N'Wegenis', 104, 0, 0),
    (N'WEGENIS_VERHARDING',         N'Wegverharding aangebracht (asfalt of klinkers)',                      N'Wegenis', 105, 0, 0),
    (N'WEGENIS_GROENAANLEG',        N'Groenaanleg en openbare ruimte klaar',                                N'Wegenis', 106, 0, 0),
    (N'WEGENIS_VOORLOPIGE_OPLEVERING', N'Voorlopige oplevering wegenis (aan de gemeente)',                  N'Wegenis', 107, 0, 0),
    (N'WEGENIS_DEFINITIEVE_OPLEVERING', N'Definitieve oplevering wegenis (einde waarborg)',                 N'Wegenis', 108, 0, 0);

INSERT INTO dbo.Bouwstaat (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag)
SELECT n.Code, n.Naam, n.Groep, n.Volgorde, n.Bereik, n.PerBouwlaag
FROM @nieuw n
WHERE NOT EXISTS (SELECT 1 FROM dbo.Bouwstaat b WHERE b.Code = n.Code AND b.ProjectId IS NULL);
PRINT N'Toegevoegd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bouwstaat/bouwstaten (de rest bestond al).';

SELECT Groep, Volgorde, Code, Naam, Bereik, PerBouwlaag FROM dbo.Bouwstaat WHERE Groep = N'Wegenis' AND ProjectId IS NULL ORDER BY Volgorde;
