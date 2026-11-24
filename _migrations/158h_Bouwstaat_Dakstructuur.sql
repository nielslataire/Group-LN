-- =============================================
-- 158h (24/11/2026): bouwstaat "Dakstructuur geplaatst" voor de schijf "Bij het plaatsen van de daktimmerwerken" (hellend dak).
--   * nieuwe bouwstaat DAKSTRUCTUUR (groep Dak, bereik gebouw), enkel als ze nog niet bestaat
--   * de taak DAKSTRUCTUUR van het planningssjabloon "Appartementsgebouw" levert die bouwstaat (enkel waar ze nog leeg is)
--   * de planningtaken met code DAKSTRUCTUUR (reeds toegepaste plannings) krijgen ze ook, enkel waar leeg
--   Idempotent. Vereist 158.
-- =============================================
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Bouwstaat WHERE Code = N'DAKSTRUCTUUR' AND ProjectId IS NULL)
    INSERT INTO dbo.Bouwstaat (Code, Naam, Groep, Volgorde, Bereik, PerBouwlaag)
    VALUES (N'DAKSTRUCTUUR', N'Dakstructuur geplaatst', N'Dak', 51, 0, 0);

DECLARE @bs INT = (SELECT TOP 1 Id FROM dbo.Bouwstaat WHERE Code = N'DAKSTRUCTUUR' AND ProjectId IS NULL);

UPDATE t SET t.BouwstaatId = @bs
FROM dbo.PlanningSjabloonTaak t
JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId
JOIN dbo.PlanningSjabloon s ON s.Id = b.SjabloonId AND s.Naam = N'Appartementsgebouw'
WHERE t.Code = N'DAKSTRUCTUUR' AND t.BouwstaatId IS NULL;

UPDATE dbo.PlanningTaak SET BouwstaatId = @bs WHERE Code = N'DAKSTRUCTUUR' AND BouwstaatId IS NULL;

SELECT b.Code, b.Naam, b.Groep, (SELECT COUNT(*) FROM dbo.PlanningSjabloonTaak t WHERE t.BouwstaatId = b.Id) AS SjabloontakenDieHetOpleveren
FROM dbo.Bouwstaat b WHERE b.Id = @bs;
