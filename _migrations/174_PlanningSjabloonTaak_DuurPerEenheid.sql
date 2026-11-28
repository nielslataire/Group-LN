-- =============================================
-- 174 (28/11/2026): duur per eenheid bij een planningstaak + afwerking van de technieken in het sjabloon "Appartementsgebouw" aangepast (Niels).
--   1. Kolom PlanningSjabloonTaak.DuurPerEenheid (werkdagen per eenheid, nullable). De duur van de taak in de planning is dan
--      dagen x het aantal hoofdeenheden (woning, appartement, commercieel) van de planning, minstens 1. Leeg = de vaste duur.
--   2. De drie taken van migratie 173 komen NA DE VLOERAFWERKING (tegelwerk en parket), niet na het schilderwerk en de lift,
--      en duren per eenheid: afwerking elektriciteit 1 dag, sanitaire toestellen 1 dag, verwarmingstoestellen 2 dagen.
--      Ze lopen tegelijk (onderling geen voorganger). De omgevingswerken en de oplevering wachten erop via het blok Afwerking.
-- Vereist: 173. Idempotent.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.PlanningSjabloonTaak', N'DuurPerEenheid') IS NULL
BEGIN
    ALTER TABLE dbo.PlanningSjabloonTaak ADD DuurPerEenheid DECIMAL(5,2) NULL;
    PRINT N'Kolom DuurPerEenheid toegevoegd.';
END
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @s INT;
SELECT @s = MIN(Id) FROM dbo.PlanningSjabloon WHERE Naam = N'Appartementsgebouw';
IF @s IS NULL
BEGIN
    PRINT N'Sjabloon "Appartementsgebouw" niet gevonden: niets gewijzigd.';
    ROLLBACK; RETURN;
END

DECLARE @taken TABLE (Code NVARCHAR(60), DagenPerEenheid DECIMAL(5,2));
INSERT INTO @taken (Code, DagenPerEenheid) VALUES (N'AFWERKING_ELEKTRA', 1), (N'SANITAIRE_TOESTELLEN', 1), (N'VERWARMINGSTOESTELLEN', 2);

-- duur per eenheid
UPDATE t SET DuurPerEenheid = x.DagenPerEenheid
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @taken x ON x.Code = t.Code
WHERE b.SjabloonId = @s;
PRINT N'Duur per eenheid gezet: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- voorgangers: weg van schilderwerk en lift, naar tegelwerk en parket (de vloerafwerking)
DELETE g FROM dbo.PlanningSjabloonVoorganger g
JOIN dbo.PlanningSjabloonTaak t ON t.Id = g.TaakId JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @taken x ON x.Code = t.Code
WHERE b.SjabloonId = @s AND g.VoorgangerCode IN (N'SCHILDERWERK', N'LIFT');
PRINT N'Oude voorgangers (schilderwerk, lift) verwijderd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
SELECT t.Id, v.Code, 0, 0
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @taken x ON x.Code = t.Code
CROSS JOIN (VALUES (N'TEGELWERK'), (N'PARKET')) v(Code)
WHERE b.SjabloonId = @s
  AND NOT EXISTS (SELECT 1 FROM dbo.PlanningSjabloonVoorganger g WHERE g.TaakId = t.Id AND g.VoorgangerCode = v.Code);
PRINT N'Voorgangers tegelwerk en parket toegevoegd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

COMMIT;
PRINT N'KLAAR. Pas het projectsjabloon opnieuw toe (Vervang) om de nieuwe duren en volgorde te zien.';
