-- =============================================
-- 172 (28/11/2026): afwerking van het planningssjabloon "Appartementsgebouw" deels parallel (B2).
--   Nu liep de afwerking helemaal in serie (103 werkdagen). Aanname (Niels mag corrigeren):
--     * Tegelwerk en parket lopen parallel: allebei na de chape (andere ruimtes: tegels in bad en keuken, parket in woonkamer en slaapkamers).
--     * De keuken komt na het tegelwerk en het parket (die bleef al op het parket wachten, nu ook op het tegelwerk).
--     * Het binnenschrijnwerk blijft na het parket (start dus al tijdens het tegelwerk).
--   Gevolg: tegelwerk (20) en parket (10) lopen naast elkaar; het kritieke pad door de afwerking wordt ongeveer 88 in plaats van 103 werkdagen.
-- Idempotent; een sjabloon dat niet bestaat wordt overgeslagen.
-- =============================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @sj TABLE (Id INT);
INSERT INTO @sj (Id) SELECT MIN(Id) FROM dbo.PlanningSjabloon WHERE Naam = N'Appartementsgebouw';

-- Parket wacht niet meer op het tegelwerk maar op de chape
DELETE g FROM dbo.PlanningSjabloonVoorganger g
JOIN dbo.PlanningSjabloonTaak t ON t.Id = g.TaakId JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @sj s ON s.Id = b.SjabloonId
WHERE t.Code = N'PARKET' AND g.VoorgangerCode = N'TEGELWERK';
PRINT N'Parket: voorganger tegelwerk verwijderd: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
SELECT t.Id, N'CHAPE', 0, 0
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @sj s ON s.Id = b.SjabloonId
WHERE t.Code = N'PARKET' AND NOT EXISTS (SELECT 1 FROM dbo.PlanningSjabloonVoorganger g WHERE g.TaakId = t.Id AND g.VoorgangerCode = N'CHAPE');
PRINT N'Parket wacht op de chape: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

-- Keuken wacht ook op het tegelwerk
INSERT INTO dbo.PlanningSjabloonVoorganger (TaakId, VoorgangerCode, Type, WachtDagen)
SELECT t.Id, N'TEGELWERK', 0, 0
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @sj s ON s.Id = b.SjabloonId
WHERE t.Code = N'KEUKEN' AND NOT EXISTS (SELECT 1 FROM dbo.PlanningSjabloonVoorganger g WHERE g.TaakId = t.Id AND g.VoorgangerCode = N'TEGELWERK');
PRINT N'Keuken wacht ook op het tegelwerk: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

COMMIT;
PRINT N'KLAAR. Pas het projectsjabloon opnieuw toe (Vervang) om de nieuwe volgorde te zien.';
