-- =============================================
-- 158g (24/11/2026): startpunt voor het schijvensjabloon "Appartementsgebouw", uit de doorloop met Niels (bijgewerkt na zijn opmerkingen).
--   Schijven: bij aanvang van de werken (werf ingericht en uitgezet, regel "gestart") · vloerplaat kelder (met kelder) · vloerplaat gelijkvloers
--   (met kelder: opstort gelijkvloers, zonder kelder: vloerplaat gelijkvloers) · vloerplaat per verdieping (opstort van elke bouwlaag boven het gelijkvloers)
--   · daktimmerwerken (enkel hellend dak) · dakbedekking (plat of hellend) · buitenschrijnwerk · leidingen en technieken · pleisterwerken · chape · keuken
--   · voorlopige oplevering.
--   Minimum 5 % per schijf (stappen van 5 %). Na het buitenschrijnwerk (gesloten ruwbouw) is ALTIJD minstens 75 % bereikt: wat daarna komt
--   (technieken, pleisterwerken, chape, keuken en de oplevering) is dus telkens vast 5 % (samen 25 %). De overige 75 % gaat naar gewicht over aanvang,
--   vloerplaten, dak en buitenschrijnwerk.
--   Elke schijf wacht op een BOUWSTAAT (geen planningcode). Vereist 158, 158e en 158h (bouwstaat "Dakstructuur geplaatst").
--   Idempotent: bestaat er al een sjabloon met deze naam, dan gebeurt er niets (pas het dan aan in de editor, of verwijder het en draai dit opnieuw).
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @naam NVARCHAR(200) = N'Appartementsgebouw';
IF EXISTS (SELECT 1 FROM dbo.SchijfSjabloon WHERE Naam = @naam)
BEGIN PRINT N'Het schijvensjabloon "' + @naam + N'" bestaat al: niets gedaan.'; RETURN; END

BEGIN TRAN;

INSERT INTO dbo.SchijfSjabloon (Naam, Beschrijving, MinimumPercentage, IsActief, CreatedDate)
VALUES (@naam, N'Appartementsgebouw: aanvang, vloerplaten per bouwlaag, dak, buitenschrijnwerk, technieken, pleisterwerken, chape, keuken en voorlopige oplevering.', 5, 1, SYSUTCDATETIME());
DECLARE @s INT = SCOPE_IDENTITY();

-- Naam (mag {verdieping} bevatten), bouwstaat, regel (0 = alle taken voltooid, 1 = gestart), vast %, gewicht, per verdieping, voorwaarde
DECLARE @sch TABLE (Nr INT IDENTITY(1,1), Naam NVARCHAR(250), Bouwstaat NVARCHAR(60), Regel TINYINT, Vast DECIMAL(9,4), Gewicht DECIMAL(9,4), PerVerd BIT, VarNaam NVARCHAR(20), VarWaarde NVARCHAR(20));
INSERT INTO @sch (Naam, Bouwstaat, Regel, Vast, Gewicht, PerVerd, VarNaam, VarWaarde) VALUES
 (N'Bij aanvang van de werken',                                         N'WERF_UITGEZET',         1, NULL, 1, 0, NULL, NULL),
 (N'Bij het gieten vloerplaat kelder',                                  N'VLOERPLAAT_KELDER',     0, NULL, 1, 0, N'kelder', N'ja'),
 (N'Bij het gieten vloerplaat gelijkvloers',                            N'OPSTORT',               0, NULL, 1, 0, N'kelder', N'ja'),
 (N'Bij het gieten vloerplaat gelijkvloers',                            N'VLOERPLAAT_GLVL',       0, NULL, 1, 0, N'kelder', N'nee'),
 (N'Bij het gieten vloerplaat {verdieping}',                            N'OPSTORT',               0, NULL, 1, 1, N'laag',   N'boven'),
 (N'Bij het plaatsen van de daktimmerwerken',                           N'DAKSTRUCTUUR',          0, NULL, 1, 0, N'dak',    N'hellend'),
 (N'Bij het plaatsen van de dakbedekking',                              N'DAK_PLAT_DICHT',        0, NULL, 1, 0, N'dak',    N'plat'),
 (N'Bij het plaatsen van de dakbedekking',                              N'DAK_HELLEND_DICHT',     0, NULL, 1, 0, N'dak',    N'hellend'),
 (N'Bij het plaatsen van het buitenschrijnwerk',                        N'BUITENSCHRIJNWERK',     0, NULL, 1, 0, NULL, NULL),
 (N'Bij het plaatsen van leidingen en technieken',                      N'TECHNIEKEN_RUW',        0, 5,    1, 0, NULL, NULL),
 (N'Bij de pleisterwerken',                                             N'PLEISTER',              0, 5,    1, 0, NULL, NULL),
 (N'Bij het plaatsen van de chape',                                     N'CHAPE',                 0, 5,    1, 0, NULL, NULL),
 (N'Bij het plaatsen van de keuken',                                    N'KEUKEN',                0, 5,    1, 0, NULL, NULL),
 (N'Bij de voorlopige oplevering (ontvangst van de sleutels)',          N'VOORLOPIGE_OPLEVERING', 0, 5,    1, 0, NULL, NULL);

INSERT INTO dbo.SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde, BouwstaatId, BouwstaatRegel)
SELECT @s, c.Nr * 10, c.Naam, NULL, c.Vast, c.Gewicht, c.PerVerd, c.VarNaam, c.VarWaarde,
       (SELECT TOP 1 b.Id FROM dbo.Bouwstaat b WHERE b.Code = c.Bouwstaat AND b.ProjectId IS NULL), c.Regel
FROM @sch c ORDER BY c.Nr;

SELECT 'Schijf zonder bouwstaat (moet leeg zijn; draai anders 158h)' AS Controle, c.Naam
FROM dbo.SchijfSjabloonSchijf c WHERE c.SjabloonId = @s AND c.BouwstaatId IS NULL;
SELECT c.Volgorde, c.Naam, b.Code AS Bouwstaat, c.BouwstaatRegel AS Regel, c.VastPercentage AS Vast, c.Gewicht, c.PerVerdieping, c.VoorwaardeVariabele AS Var, c.VoorwaardeWaarde AS Waarde
FROM dbo.SchijfSjabloonSchijf c LEFT JOIN dbo.Bouwstaat b ON b.Id = c.BouwstaatId WHERE c.SjabloonId = @s ORDER BY c.Volgorde;

COMMIT;
PRINT N'KLAAR: bewaard. Open het sjabloon in Instellingen > Sjablonen > Schijven om percentages en voorbeeld te bekijken.';
