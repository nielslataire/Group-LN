-- =============================================
-- Aanvulling op 158_Bouwstaten (24/11/2026): codes die de controlelijst van 158 overliet.
--   Idempotent; enkel waar BouwstaatId / ActivityId nog leeg is.
--   Hernoemt eerst codes naar de standaardcode: FUNDERINGE -> FUNDERINGEN, VLOERPLAAT_GELIJKVLOERS -> VLOERPLAAT_GLVL,
--   NIEUWE_TAAK_3 -> BINNENSCHRIJNWERK, NIEUWE_TAAK_4 -> KEUKEN (taak, voorgangers, schijven, planningtaken en -fases), enkel waar de nieuwe code in dat sjabloon nog vrij is.
--   Bewust niet gekoppeld: NIEUWE_TAAK_5 ("Test") en DAKSTRUCTUUR (geen bouwstaat, wel een activiteit).
--   Aanname: "Ruwbouw gelijkvloers" / "Ruwbouw eerste verdieping" (woningsjabloon) leveren METSELWERK_RUWBOUW per bouwlaag.
-- =============================================

-- Codes rechtzetten: taak, voorgangers, schijven, planningtaken en -fases.
-- Een code wordt enkel hernoemd in sjablonen waar de nieuwe code nog niet bestaat (codes moeten uniek blijven binnen een sjabloon);
-- zo niet, dan blijft de oude code staan en toont de controle onderaan dat.
DECLARE @ren TABLE (Oud NVARCHAR(100), Nieuw NVARCHAR(100));
INSERT INTO @ren (Oud, Nieuw) VALUES
    (N'FUNDERINGE',              N'FUNDERINGEN'),
    (N'VLOERPLAAT_GELIJKVLOERS', N'VLOERPLAAT_GLVL'),
    (N'NIEUWE_TAAK_3',           N'BINNENSCHRIJNWERK'),
    (N'NIEUWE_TAAK_4',           N'KEUKEN');

DECLARE @hernoemd TABLE (SjabloonId INT, Oud NVARCHAR(100), Nieuw NVARCHAR(100));
INSERT INTO @hernoemd (SjabloonId, Oud, Nieuw)
SELECT DISTINCT b.SjabloonId, r.Oud, r.Nieuw
FROM [dbo].[PlanningSjabloonTaak] t
JOIN [dbo].[PlanningSjabloonBlok] b ON b.Id = t.BlokId
JOIN @ren r ON r.Oud = t.Code
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[PlanningSjabloonTaak] t2 JOIN [dbo].[PlanningSjabloonBlok] b2 ON b2.Id = t2.BlokId
                  WHERE b2.SjabloonId = b.SjabloonId AND t2.Code = r.Nieuw);

UPDATE v SET v.VoorgangerCode = h.Nieuw
FROM [dbo].[PlanningSjabloonVoorganger] v
JOIN [dbo].[PlanningSjabloonTaak] t ON t.Id = v.TaakId
JOIN [dbo].[PlanningSjabloonBlok] b ON b.Id = t.BlokId
JOIN @hernoemd h ON h.SjabloonId = b.SjabloonId AND h.Oud = v.VoorgangerCode;

UPDATE t SET t.Code = h.Nieuw
FROM [dbo].[PlanningSjabloonTaak] t
JOIN [dbo].[PlanningSjabloonBlok] b ON b.Id = t.BlokId
JOIN @hernoemd h ON h.SjabloonId = b.SjabloonId AND h.Oud = t.Code;

-- Schijven en planningen: enkel de codes die in minstens één sjabloon hernoemd zijn (anders is de nieuwe code daar niet bekend).
UPDATE s SET s.PlanningCode = r.Nieuw
FROM [dbo].[SchijfSjabloonSchijf] s JOIN @ren r ON r.Oud = s.PlanningCode
WHERE EXISTS (SELECT 1 FROM @hernoemd h WHERE h.Oud = r.Oud);
UPDATE s SET s.PlanningCode = r.Nieuw
FROM [dbo].[InvoicingPaymentStages] s JOIN @ren r ON r.Oud = s.PlanningCode
WHERE EXISTS (SELECT 1 FROM @hernoemd h WHERE h.Oud = r.Oud);
UPDATE x SET x.Code = r.Nieuw
FROM [dbo].[PlanningTaak] x JOIN @ren r ON r.Oud = x.Code
WHERE EXISTS (SELECT 1 FROM @hernoemd h WHERE h.Oud = r.Oud);
UPDATE x SET x.Code = r.Nieuw
FROM [dbo].[PlanningSectie] x JOIN @ren r ON r.Oud = x.Code
WHERE EXISTS (SELECT 1 FROM @hernoemd h WHERE h.Oud = r.Oud);

SELECT 'Hernoemd' AS Soort, h.SjabloonId, h.Oud, h.Nieuw FROM @hernoemd h;
SELECT 'Niet hernoemd (nieuwe code bestond al in dat sjabloon)' AS Soort, b.SjabloonId, t.Code, t.Naam
FROM [dbo].[PlanningSjabloonTaak] t JOIN [dbo].[PlanningSjabloonBlok] b ON b.Id = t.BlokId
WHERE t.Code IN (SELECT Oud FROM @ren);
GO

DECLARE @map TABLE (Code NVARCHAR(100), Bouwstaat NVARCHAR(60) NULL, Activiteit NVARCHAR(200) NULL);
INSERT INTO @map (Code, Bouwstaat, Activiteit) VALUES
    (N'FUNDERINGEN',           N'FUNDERING_KLAAR',   N'Ruwbouw'),
    (N'VLOERPLAAT_GLVL',       N'VLOERPLAAT_GLVL',   N'Ruwbouw'),
    (N'VLOERPLAAT_GELIJKVLOERS', N'VLOERPLAAT_GLVL', N'Ruwbouw'),
    (N'BINNENSCHRIJNWERK',     N'BINNENSCHRIJNWERK', N'Binnendeuren'),
    (N'NIEUWE_TAAK_3',         N'BINNENSCHRIJNWERK', N'Binnendeuren'),
    (N'KEUKEN',                N'KEUKEN',            N'Vast Meubilair - Keukens'),
    (N'NIEUWE_TAAK_4',         N'KEUKEN',            N'Vast Meubilair - Keukens'),
    (N'RUWBOUW_GLVL',          N'METSELWERK_RUWBOUW', N'Ruwbouw'),
    (N'RUWBOUW_V1',            N'METSELWERK_RUWBOUW', N'Ruwbouw'),
    (N'DAKSTRUCTUUR',          NULL,                 N'Daktimmerwerken');

UPDATE t SET t.BouwstaatId = b.Id
FROM [dbo].[PlanningSjabloonTaak] t JOIN @map m ON m.Code = t.Code
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE t.BouwstaatId IS NULL;

UPDATE t SET t.ActivityId = a.ActivityId
FROM [dbo].[PlanningSjabloonTaak] t JOIN @map m ON m.Code = t.Code
JOIN [dbo].[Activity] a ON a.Omschrijving = m.Activiteit
WHERE t.ActivityId IS NULL;

UPDATE t SET t.BouwstaatId = b.Id
FROM [dbo].[PlanningTaak] t JOIN @map m ON m.Code = t.Code
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE t.BouwstaatId IS NULL;

UPDATE t SET t.ActivityId = a.ActivityId
FROM [dbo].[PlanningTaak] t JOIN @map m ON m.Code = t.Code
JOIN [dbo].[Activity] a ON a.Omschrijving = m.Activiteit
WHERE t.ActivityId IS NULL;

UPDATE s SET s.BouwstaatId = b.Id
FROM [dbo].[PlanningSectie] s JOIN @map m ON m.Code = s.Code
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE s.BouwstaatId IS NULL;

UPDATE s SET s.BouwstaatId = b.Id
FROM [dbo].[SchijfSjabloonSchijf] s JOIN @map m ON m.Code = s.PlanningCode
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE s.BouwstaatId IS NULL;

UPDATE s SET s.BouwstaatId = b.Id
FROM [dbo].[InvoicingPaymentStages] s JOIN @map m ON m.Code = s.PlanningCode
JOIN [dbo].[Bouwstaat] b ON b.Code = m.Bouwstaat AND b.ProjectId IS NULL
WHERE s.BouwstaatId IS NULL;
GO

-- Controle 1: sjabloontaken zonder bouwstaat (mag nog NIEUWE_TAAK_5 en DAKSTRUCTUUR bevatten)
SELECT 'Zonder bouwstaat' AS Soort, t.Code, t.Naam FROM [dbo].[PlanningSjabloonTaak] t WHERE t.BouwstaatId IS NULL;

-- Controle 2: sjabloontaken zonder activiteit (de gesplitste leidingtaken en de nieuwe Landmeter-taak krijgen die handmatig)
SELECT 'Zonder activiteit' AS Soort, t.Code, t.Naam FROM [dbo].[PlanningSjabloonTaak] t WHERE t.ActivityId IS NULL;
GO
