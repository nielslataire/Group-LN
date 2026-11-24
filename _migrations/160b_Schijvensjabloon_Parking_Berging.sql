-- =============================================
-- 160b (24/11/2026): schijvensjabloon "Parking of berging" (voor een parking of berging met eigen schijven) en koppeling aan het projectsjabloon "Appartementsgebouw".
--   30 % bij aanvang van de werken · 40 % bij het gieten van de vloerplaat (kelder, of gelijkvloers zonder kelder) · 30 % bij de voorlopige oplevering.
--   Een parking of berging die aan een appartement hangt, zet je in de betalingsgroep van dat appartement en volgt dan diens schijven.
--   Vereist 158 (bouwstaten), 158j (projectsjabloon) en 160 (kolom). Idempotent: bestaat het sjabloon al, dan wordt het enkel (opnieuw) gekoppeld.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @naam NVARCHAR(200) = N'Parking of berging';
BEGIN TRAN;

IF NOT EXISTS (SELECT 1 FROM dbo.SchijfSjabloon WHERE Naam = @naam)
BEGIN
    INSERT INTO dbo.SchijfSjabloon (Naam, Beschrijving, MinimumPercentage, IsActief, CreatedDate)
    VALUES (@naam, N'Parking of berging met eigen schijven: aanvang, vloerplaat en voorlopige oplevering.', 5, 1, SYSUTCDATETIME());
    DECLARE @s INT = SCOPE_IDENTITY();

    DECLARE @sch TABLE (Nr INT IDENTITY(1,1), Naam NVARCHAR(250), Bouwstaat NVARCHAR(60), Regel TINYINT, Vast DECIMAL(9,4), Gewicht DECIMAL(9,4), VarNaam NVARCHAR(20), VarWaarde NVARCHAR(20));
    INSERT INTO @sch (Naam, Bouwstaat, Regel, Vast, Gewicht, VarNaam, VarWaarde) VALUES
     (N'Bij aanvang van de werken',                                  N'WERF_UITGEZET',         1, 30,   1, NULL,     NULL),
     (N'Bij het gieten van de vloerplaat kelder',                    N'VLOERPLAAT_KELDER',     0, NULL, 1, N'kelder', N'ja'),
     (N'Bij het gieten van de vloerplaat gelijkvloers',              N'VLOERPLAAT_GLVL',       0, NULL, 1, N'kelder', N'nee'),
     (N'Bij de voorlopige oplevering (ontvangst van de sleutels)',   N'VOORLOPIGE_OPLEVERING', 0, 30,   1, NULL,     NULL);

    INSERT INTO dbo.SchijfSjabloonSchijf (SjabloonId, Volgorde, Naam, PlanningCode, VastPercentage, Gewicht, PerVerdieping, VoorwaardeVariabele, VoorwaardeWaarde, BouwstaatId, BouwstaatRegel)
    SELECT @s, c.Nr * 10, c.Naam, NULL, c.Vast, c.Gewicht, 0, c.VarNaam, c.VarWaarde,
           (SELECT TOP 1 b.Id FROM dbo.Bouwstaat b WHERE b.Code = c.Bouwstaat AND b.ProjectId IS NULL), c.Regel
    FROM @sch c ORDER BY c.Nr;

    SELECT 'Schijf zonder bouwstaat (moet leeg zijn; draai anders 158)' AS Controle, c.Naam FROM dbo.SchijfSjabloonSchijf c WHERE c.SjabloonId = @s AND c.BouwstaatId IS NULL;
END

DECLARE @id INT = (SELECT TOP 1 Id FROM dbo.SchijfSjabloon WHERE Naam = @naam ORDER BY Id);
UPDATE dbo.ProjectSjabloon SET SchijfSjabloonBijgebouwId = @id WHERE Naam = N'Appartementsgebouw' AND SchijfSjabloonBijgebouwId IS NULL;
PRINT CONCAT(N'Projectsjablonen gekoppeld: ', @@ROWCOUNT);

SELECT c.Volgorde, c.Naam, b.Code AS Bouwstaat, c.BouwstaatRegel AS Regel, c.VastPercentage AS Vast, c.Gewicht, c.VoorwaardeVariabele AS Var, c.VoorwaardeWaarde AS Waarde
FROM dbo.SchijfSjabloonSchijf c LEFT JOIN dbo.Bouwstaat b ON b.Id = c.BouwstaatId WHERE c.SjabloonId = @id ORDER BY c.Volgorde;
COMMIT;
