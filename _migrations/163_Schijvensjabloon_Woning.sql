-- =============================================
-- 163 (25/11/2026): schijvensjabloon "Woning" (gelijkvloers + 1 verdieping), volgens de verdeling van Niels: 75 % tot gesloten ruwbouw, 25 % afwerking en oplevering.
--   5 % uitgezet · 20 % vloerplaat gelijkvloers · 15 % vloerplaat verdieping · 15 % dakplaat (plat dak) of dakstructuur (hellend dak)
--   · 10 % dakbedekking · 10 % buitenschrijnwerk · 5 % technieken · 5 % pleister · 5 % chape · 5 % keuken · 5 % voorlopige oplevering = 100 %.
--   Grond en kosten staan hier NIET in: die komen op de aktefacturen (grondfactuur, kostenfactuur), niet als schijf.
--   Aannames om te bevestigen: (1) de "dakplaat" bij een hellend dak is de dakstructuur (daktimmerwerken); (2) één verdieping: bij meer bouwlagen komt de schijf "vloerplaat verdieping"
--   per verdieping terug en wordt het totaal meer dan 100 % (het toepassen weigert dat dan met een melding).
--   Vereist 158, 158e, 158h en 161/162 niet nodig. Idempotent: bestaat het sjabloon al, dan gebeurt er niets.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @naam NVARCHAR(200) = N'Woning';
IF EXISTS (SELECT 1 FROM dbo.SchijfSjabloon WHERE Naam = @naam)
BEGIN PRINT N'Het schijvensjabloon "' + @naam + N'" bestaat al: niets gedaan.'; RETURN; END

BEGIN TRAN;

INSERT INTO dbo.SchijfSjabloon (Naam, Beschrijving, MinimumPercentage, IsActief, CreatedDate)
VALUES (@naam, N'Woning: uitzetten, vloerplaten, dak, buitenschrijnwerk, technieken, pleister, chape, keuken en voorlopige oplevering (75 % tot gesloten ruwbouw, 25 % afwerking).', 5, 1, SYSUTCDATETIME());
DECLARE @s INT = SCOPE_IDENTITY();

DECLARE @sch TABLE (Nr INT IDENTITY(1,1), Naam NVARCHAR(250), Bouwstaat NVARCHAR(60), Regel TINYINT, Vast DECIMAL(9,4), PerVerd BIT, VarNaam NVARCHAR(20), VarWaarde NVARCHAR(20));
INSERT INTO @sch (Naam, Bouwstaat, Regel, Vast, PerVerd, VarNaam, VarWaarde) VALUES
 (N'Na het uitzetten van de woning',                              N'WERF_UITGEZET',         0, 5,  0, NULL, NULL),
 (N'Na het gieten van de vloerplaat gelijkvloers',                N'VLOERPLAAT_GLVL',       0, 20, 0, NULL, NULL),
 (N'Na het gieten van de vloerplaat {verdieping}',                N'OPSTORT',               0, 15, 1, N'laag', N'boven'),
 (N'Na het gieten van de dakplaat',                               N'DAKPLAAT',              0, 15, 0, N'dak', N'plat'),
 (N'Na het plaatsen van de dakstructuur',                         N'DAKSTRUCTUUR',          0, 15, 0, N'dak', N'hellend'),
 (N'Na het plaatsen van het dak',                                 N'DAK_PLAT_DICHT',        0, 10, 0, N'dak', N'plat'),
 (N'Na het plaatsen van het dak',                                 N'DAK_HELLEND_DICHT',     0, 10, 0, N'dak', N'hellend'),
 (N'Na het plaatsen van de ramen',                                N'BUITENSCHRIJNWERK',     0, 10, 0, NULL, NULL),
 (N'Na het plaatsen van de leidingen en technieken',              N'TECHNIEKEN_RUW',        0, 5,  0, NULL, NULL),
 (N'Na het plaatsen van het pleisterwerk',                        N'PLEISTER',              0, 5,  0, NULL, NULL),
 (N'Na het plaatsen van de chape',                                N'CHAPE',                 0, 5,  0, NULL, NULL),
 (N'Na het plaatsen van de keuken',                               N'KEUKEN',                0, 5,  0, NULL, NULL),
 (N'Bij de voorlopige oplevering (ontvangst van de sleutels)',    N'VOORLOPIGE_OPLEVERING', 0, 5,  0, NULL, NULL);

INSERT INTO dbo.SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde, BouwstaatId, BouwstaatRegel)
SELECT @s, c.Nr * 10, c.Naam, NULL, c.Vast, 1, c.PerVerd, c.VarNaam, c.VarWaarde,
       (SELECT TOP 1 b.Id FROM dbo.Bouwstaat b WHERE b.Code = c.Bouwstaat AND b.ProjectId IS NULL), c.Regel
FROM @sch c ORDER BY c.Nr;

SELECT 'Schijf zonder bouwstaat (moet leeg zijn; draai anders 158 t/m 158h)' AS Controle, c.Naam FROM dbo.SchijfSjabloonSchijf c WHERE c.SjabloonId = @s AND c.BouwstaatId IS NULL;
SELECT c.Volgorde, c.Naam, b.Code AS Bouwstaat, c.VastPercentage AS Vast, c.PerVerdieping, c.VoorwaardeVariabele AS Var, c.VoorwaardeWaarde AS Waarde
FROM dbo.SchijfSjabloonSchijf c LEFT JOIN dbo.Bouwstaat b ON b.Id = c.BouwstaatId WHERE c.SjabloonId = @s ORDER BY c.Volgorde;

COMMIT;
PRINT N'KLAAR: bewaard. Open het sjabloon in Instellingen > Sjablonen > Schijven om het voorbeeld te bekijken.';
