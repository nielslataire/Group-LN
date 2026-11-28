-- =============================================
-- 173 (28/11/2026): afwerking van de technieken in het planningssjabloon "Appartementsgebouw" (opmerking Niels):
--   Afwerking elektriciteit (schakelaars, stopcontacten, armaturen), Sanitaire toestellen en Verwarmingstoestellen kunnen tegelijkertijd lopen.
--   Drie nieuwe taken in het blok Afwerking, zonder onderlinge voorganger. Aannames (Niels mag corrigeren):
--     * ze starten na het schilderwerk (toestellen en schakelmateriaal komen op het afgewerkte oppervlak) en na de lift;
--     * duur 8 werkdagen elk (schatting);
--     * de omgevingswerken en de oplevering wachten erop via het blok Afwerking, dus ze eindigen nooit na de oplevering;
--     * activiteiten: Elektrische installatie, Sanitaire installatie en (indien gevonden) een activiteit met "verwarming"; anders leeg.
--   Het sjabloon "Woning" (estafette per woning) blijft ongewijzigd.
-- Idempotent: een taak met die code die al in het sjabloon staat, wordt overgeslagen.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @s INT, @blok INT, @vol INT;
SELECT @s = MIN(Id) FROM dbo.PlanningSjabloon WHERE Naam = N'Appartementsgebouw';
SELECT @blok = Id FROM dbo.PlanningSjabloonBlok WHERE SjabloonId = @s AND Code = N'B_AFWERKING';
IF @s IS NULL OR @blok IS NULL
BEGIN
    PRINT N'Sjabloon "Appartementsgebouw" of blok B_AFWERKING niet gevonden: niets gewijzigd.';
    ROLLBACK; RETURN;
END
SELECT @vol = ISNULL(MAX(Volgorde), 0) FROM dbo.PlanningSjabloonTaak WHERE BlokId = @blok;

DECLARE @t TABLE (Nr INT IDENTITY(1,1), Code NVARCHAR(60), Naam NVARCHAR(200), Duur INT, Activiteit INT);
INSERT INTO @t (Code, Naam, Duur, Activiteit) VALUES
 (N'AFWERKING_ELEKTRA',  N'Afwerking elektriciteit (schakelaars, armaturen)', 8, (SELECT TOP 1 ActivityId FROM dbo.Activity WHERE Omschrijving = N'Elektrische installatie' ORDER BY ActivityId)),
 (N'SANITAIRE_TOESTELLEN', N'Plaatsen sanitaire toestellen',                  8, (SELECT TOP 1 ActivityId FROM dbo.Activity WHERE Omschrijving = N'Sanitaire installatie' ORDER BY ActivityId)),
 (N'VERWARMINGSTOESTELLEN', N'Plaatsen verwarmingstoestellen',               8, (SELECT TOP 1 ActivityId FROM dbo.Activity WHERE Omschrijving LIKE N'%verwarming%' ORDER BY ActivityId));

INSERT INTO dbo.PlanningSjabloonTaak (BlokId, Code, Naam, Duur, Volgorde, Estafette, EstafetteWacht, ActivityId)
SELECT @blok, t.Code, t.Naam, t.Duur, @vol + t.Nr * 10, 0, 0, t.Activiteit
FROM @t t
WHERE NOT EXISTS (SELECT 1 FROM dbo.PlanningSjabloonTaak e JOIN dbo.PlanningSjabloonBlok b ON b.Id = e.BlokId WHERE b.SjabloonId = @s AND e.Code = t.Code);
PRINT N'Taken toegevoegd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- Voorgangers: na het schilderwerk en de lift; onderling geen voorganger (parallel)
INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
SELECT x.Id, v.Code, 0, 0
FROM dbo.PlanningSjabloonTaak x
JOIN @t t ON t.Code = x.Code
CROSS JOIN (VALUES (N'SCHILDERWERK'), (N'LIFT')) v(Code)
WHERE x.BlokId = @blok
  AND NOT EXISTS (SELECT 1 FROM dbo.PlanningSjabloonVoorganger g WHERE g.TaakId = x.Id AND g.VoorgangerCode = v.Code);
PRINT N'Voorgangers toegevoegd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

SELECT N'Activiteit gevonden?' AS Controle, t.Code, t.Activiteit FROM @t t;
COMMIT;
PRINT N'KLAAR. Pas het projectsjabloon opnieuw toe (Vervang) om de nieuwe taken te zien.';
