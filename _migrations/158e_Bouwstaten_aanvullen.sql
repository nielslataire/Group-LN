-- =============================================
-- 158e (24/11/2026): extra bouwstaten uit de eerste doorloop met Niels. Idempotent: enkel wat nog niet bestaat (op code, bedrijfsbreed).
--   Bereik 0 = gebouw (alle eenheden), 1 = per eenheid. PerBouwlaag = komt voor elke bouwlaag terug.
--   Naam, groep, volgorde, bereik en "per bouwlaag" kan je daarna in Instellingen › Bouwstaten aanpassen; enkel de code ligt vast.
--   Optioneel (gevelbepleistering, houten en aluminium gevelbekleding): de bouwstaat bestaat altijd, of een taak ervoor in de planning komt is een keuze per project.
--   Aanname: gipsblokken zijn niet per bouwlaag; zet het aan in de pagina als jullie er per bouwlaag een schijf op hebben.
-- =============================================

DECLARE @nieuw TABLE (Code NVARCHAR(60), Naam NVARCHAR(200), Groep NVARCHAR(100), Volgorde INT, Bereik TINYINT, PerBouwlaag BIT);
INSERT INTO @nieuw (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag) VALUES
    (N'RIOLERING',               N'Rioleringen geplaatst',                   N'Ruwbouw onderbouw', 34, 0, 0),
    (N'DORPELS',                 N'Dorpels geplaatst',                       N'Ruwbouw bovenbouw', 47, 0, 0),
    (N'GIPSBLOKKEN',             N'Gipsblokken geplaatst',                   N'Ruwbouw bovenbouw', 48, 0, 0),
    (N'GEVELBEPLEISTERING',      N'Gevelbepleistering klaar (optioneel)',    N'Ruwbouw bovenbouw', 49, 0, 0),
    (N'GEVELBEKLEDING_HOUT',     N'Houten gevelbekleding klaar (optioneel)', N'Ruwbouw bovenbouw', 50, 0, 0),
    (N'GEVELBEKLEDING_ALU',      N'Aluminium gevelbekleding klaar (optioneel)', N'Ruwbouw bovenbouw', 51, 0, 0),
    (N'HELLINGSSCHAPE',          N'Hellingsschape plat dak klaar',           N'Dak', 53, 0, 0),
    (N'OMGEVINGSWERKEN',         N'Omgevingswerken klaar',                   N'Omgeving', 95, 0, 0);

INSERT INTO [dbo].[Bouwstaat] (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag)
SELECT n.Code, n.Naam, n.Groep, n.Volgorde, n.Bereik, n.PerBouwlaag
FROM @nieuw n
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Bouwstaat] b WHERE b.Code = n.Code AND b.ProjectId IS NULL);

SELECT Groep, Volgorde, Code, Naam, Bereik, PerBouwlaag FROM [dbo].[Bouwstaat] WHERE ProjectId IS NULL ORDER BY Volgorde, Naam;
GO
