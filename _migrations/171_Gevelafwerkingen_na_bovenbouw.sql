-- =============================================
-- 171 (28/11/2026): gevelafwerkingen in de planningssjablonen "Appartementsgebouw" en "Woning" aangepast na de test van Niels.
--   * Het gevelmetselwerk is nu ook optioneel (taakvoorwaarde gevel:steen, naast laag:eenmalig).
--   * Gevelbepleistering, houten en aluminium gevelbekleding starten pas als de HELE bovenbouw af is (voorganger = blok B_BOVENBOUW,
--     dus ook het gevelmetselwerk als dat gekozen is). Hout en aluminium wachten (indien gekozen) ook nog op de gevelbepleistering.
--   * De opkuis wacht ook op het gevelmetselwerk (indien gekozen), zodat het nooit na de oplevering eindigt.
-- Vereist: 170_Planningssjabloon_Gevelafwerkingen.sql. Idempotent; een sjabloon dat niet bestaat wordt overgeslagen.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @sj TABLE (Id INT);
INSERT INTO @sj (Id) SELECT MIN(Id) FROM dbo.PlanningSjabloon WHERE Naam IN (N'Appartementsgebouw', N'Woning') GROUP BY Naam;

-- 1. Gevelmetselwerk optioneel
UPDATE t SET Voorwaarde = N'laag:eenmalig,gevel:steen'
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @sj s ON s.Id = b.SjabloonId
WHERE t.Code = N'GEVELMETSELWERK' AND ISNULL(t.Voorwaarde, N'') <> N'laag:eenmalig,gevel:steen';
PRINT N'Gevelmetselwerk optioneel gemaakt: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 2. Pleister, hout en aluminium: niet meer na het gevelmetselwerk maar na de hele bovenbouw
DELETE g FROM dbo.PlanningSjabloonVoorganger g
JOIN dbo.PlanningSjabloonTaak t ON t.Id = g.TaakId JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @sj s ON s.Id = b.SjabloonId
WHERE t.Code IN (N'GEVELBEPLEISTERING', N'GEVELBEKLEDING_HOUT', N'GEVELBEKLEDING_ALU') AND g.VoorgangerCode = N'GEVELMETSELWERK';
PRINT N'Oude voorgangers (gevelmetselwerk) verwijderd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
SELECT t.Id, N'B_BOVENBOUW', 0, 0
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @sj s ON s.Id = b.SjabloonId
WHERE t.Code IN (N'GEVELBEPLEISTERING', N'GEVELBEKLEDING_HOUT', N'GEVELBEKLEDING_ALU')
  AND EXISTS (SELECT 1 FROM dbo.PlanningSjabloonBlok bb WHERE bb.SjabloonId = s.Id AND bb.Code = N'B_BOVENBOUW')
  AND NOT EXISTS (SELECT 1 FROM dbo.PlanningSjabloonVoorganger g WHERE g.TaakId = t.Id AND g.VoorgangerCode = N'B_BOVENBOUW');
PRINT N'Voorgangers "bovenbouw af" toegevoegd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- 3. De opkuis wacht ook op het gevelmetselwerk
INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
SELECT t.Id, N'GEVELMETSELWERK', 0, 0
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @sj s ON s.Id = b.SjabloonId
WHERE t.Code = N'OPKUIS'
  AND NOT EXISTS (SELECT 1 FROM dbo.PlanningSjabloonVoorganger g WHERE g.TaakId = t.Id AND g.VoorgangerCode = N'GEVELMETSELWERK');
PRINT N'Opkuis wacht ook op het gevelmetselwerk: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

COMMIT;
PRINT N'KLAAR. Pas het projectsjabloon opnieuw toe (Vervang) en kies de gevelafwerkingen in het venster.';
