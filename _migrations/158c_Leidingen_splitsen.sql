-- =============================================
-- 158c: de sjabloontaak LEIDINGEN ("Plaatsen leidingen technieken") splitsen in twee taken (24/11/2026).
--   LEIDINGEN_ELEKTRA   "Plaatsen leidingen elektriciteit"            activiteit Elektrische installatie
--   LEIDINGEN_SANITAIR  "Plaatsen leidingen sanitair en verwarming"   activiteit Sanitaire installatie
--   Beide leveren de bouwstaat TECHNIEKEN_RUW (de schijf wordt betaalbaar als beide klaar zijn; die regel komt in deel 2).
--   Per sjabloon waar LEIDINGEN staat (en LEIDINGEN_ELEKTRA nog niet bestaat):
--     * de bestaande taak krijgt de code en naam van elektriciteit;
--     * de nieuwe taak heeft dezelfde duur, voorwaarde en voorgangers (de twee lopen parallel) en komt er direct na;
--     * elke taak die op LEIDINGEN wachtte, wacht nu op beide;
--     * sjabloonschijven met planningcode LEIDINGEN verwijzen voorlopig naar LEIDINGEN_ELEKTRA (de bouwstaat blijft TECHNIEKEN_RUW).
--   Niet aangepast: reeds toegepaste plannings (hun taak LEIDINGEN blijft), en het trajectsjabloon (mijlpaalcode LEIDINGEN
--   hangt dus niet meer automatisch aan een taak; dat lost deel 2 op via de bouwstaat).
--   Idempotent. Vereist 158 (bouwstaten en kolommen).
-- =============================================

DECLARE @bs INT = (SELECT TOP 1 Id FROM [dbo].[Bouwstaat] WHERE Code = N'TECHNIEKEN_RUW' AND ProjectId IS NULL);
DECLARE @actE INT = (SELECT TOP 1 ActivityId FROM [dbo].[Activity] WHERE Omschrijving = N'Elektrische installatie');
DECLARE @actS INT = (SELECT TOP 1 ActivityId FROM [dbo].[Activity] WHERE Omschrijving = N'Sanitaire installatie');

DECLARE @oud TABLE (TaakId INT, BlokId INT, SjabloonId INT, Volgorde INT);
INSERT INTO @oud (TaakId, BlokId, SjabloonId, Volgorde)
SELECT t.Id, t.BlokId, b.SjabloonId, t.Volgorde
FROM [dbo].[PlanningSjabloonTaak] t
JOIN [dbo].[PlanningSjabloonBlok] b ON b.Id = t.BlokId
WHERE t.Code = N'LEIDINGEN'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[PlanningSjabloonTaak] t2 JOIN [dbo].[PlanningSjabloonBlok] b2 ON b2.Id = t2.BlokId
                  WHERE b2.SjabloonId = b.SjabloonId AND t2.Code IN (N'LEIDINGEN_ELEKTRA', N'LEIDINGEN_SANITAIR'));

DECLARE @taak INT, @blok INT, @sjab INT, @volg INT, @nieuw INT;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT TaakId, BlokId, SjabloonId, Volgorde FROM @oud;
OPEN c; FETCH NEXT FROM c INTO @taak, @blok, @sjab, @volg;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- plaats maken direct na de oude taak
    UPDATE [dbo].[PlanningSjabloonTaak] SET Volgorde = Volgorde + 1 WHERE BlokId = @blok AND Volgorde > @volg;

    INSERT INTO [dbo].[PlanningSjabloonTaak] (BlokId, Code, Naam, Beschrijving, Duur, Volgorde, Estafette, EstafetteWacht, Voorwaarde, BouwstaatId, ActivityId)
    SELECT BlokId, N'LEIDINGEN_SANITAIR', N'Plaatsen leidingen sanitair en verwarming', Beschrijving, Duur, @volg + 1, Estafette, EstafetteWacht, Voorwaarde, @bs, @actS
    FROM [dbo].[PlanningSjabloonTaak] WHERE Id = @taak;
    SET @nieuw = SCOPE_IDENTITY();

    -- zelfde voorgangers als de oude taak
    INSERT INTO [dbo].[PlanningSjabloonVoorganger] (TaakId, VoorgangerCode, Type, WachtDagen)
    SELECT @nieuw, VoorgangerCode, Type, WachtDagen FROM [dbo].[PlanningSjabloonVoorganger] WHERE TaakId = @taak;

    -- de oude taak wordt elektriciteit
    UPDATE [dbo].[PlanningSjabloonTaak]
    SET Code = N'LEIDINGEN_ELEKTRA', Naam = N'Plaatsen leidingen elektriciteit', BouwstaatId = @bs, ActivityId = @actE
    WHERE Id = @taak;

    -- wie op LEIDINGEN wachtte, wacht nu op beide
    INSERT INTO [dbo].[PlanningSjabloonVoorganger] (TaakId, VoorgangerCode, Type, WachtDagen)
    SELECT v.TaakId, N'LEIDINGEN_SANITAIR', v.Type, v.WachtDagen
    FROM [dbo].[PlanningSjabloonVoorganger] v
    JOIN [dbo].[PlanningSjabloonTaak] t ON t.Id = v.TaakId
    JOIN [dbo].[PlanningSjabloonBlok] b ON b.Id = t.BlokId
    WHERE v.VoorgangerCode = N'LEIDINGEN' AND b.SjabloonId = @sjab;
    UPDATE v SET v.VoorgangerCode = N'LEIDINGEN_ELEKTRA'
    FROM [dbo].[PlanningSjabloonVoorganger] v
    JOIN [dbo].[PlanningSjabloonTaak] t ON t.Id = v.TaakId
    JOIN [dbo].[PlanningSjabloonBlok] b ON b.Id = t.BlokId
    WHERE v.VoorgangerCode = N'LEIDINGEN' AND b.SjabloonId = @sjab;

    -- sjabloonschijven van dit sjabloon-bouwdeel: de schijf-sjablonen hebben geen sjabloonkoppeling, dus enkel de code omzetten (zie hieronder)
    FETCH NEXT FROM c INTO @taak, @blok, @sjab, @volg;
END
CLOSE c; DEALLOCATE c;

-- Schijven (schijvensjablonen staan los van de planningssjablonen): verwijzen voortaan naar elektriciteit
UPDATE [dbo].[SchijfSjabloonSchijf] SET PlanningCode = N'LEIDINGEN_ELEKTRA'
WHERE PlanningCode = N'LEIDINGEN' AND EXISTS (SELECT 1 FROM @oud);
GO

-- Controle
SELECT s.Naam AS Sjabloon, t.Code, t.Naam, t.Duur, t.Volgorde, a.Omschrijving AS Activiteit, bs.Code AS Bouwstaat
FROM [dbo].[PlanningSjabloonTaak] t
JOIN [dbo].[PlanningSjabloonBlok] b ON b.Id = t.BlokId
JOIN [dbo].[PlanningSjabloon] s ON s.Id = b.SjabloonId
LEFT JOIN [dbo].[Activity] a ON a.ActivityId = t.ActivityId
LEFT JOIN [dbo].[Bouwstaat] bs ON bs.Id = t.BouwstaatId
WHERE t.Code LIKE N'LEIDINGEN%'
ORDER BY s.Naam, t.Volgorde;

SELECT v.Id, s.Naam AS Sjabloon, t.Code AS Taak, v.VoorgangerCode
FROM [dbo].[PlanningSjabloonVoorganger] v
JOIN [dbo].[PlanningSjabloonTaak] t ON t.Id = v.TaakId
JOIN [dbo].[PlanningSjabloonBlok] b ON b.Id = t.BlokId
JOIN [dbo].[PlanningSjabloon] s ON s.Id = b.SjabloonId
WHERE v.VoorgangerCode LIKE N'LEIDINGEN%' OR t.Code LIKE N'LEIDINGEN%'
ORDER BY s.Naam, t.Code;
GO
