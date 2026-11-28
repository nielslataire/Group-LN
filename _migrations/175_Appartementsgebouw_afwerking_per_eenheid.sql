-- =============================================
-- 175 (28/11/2026): alle taken van het blok Afwerking in het sjabloon "Appartementsgebouw" duren per eenheid (Niels: de afwerking is per eenheid).
--   Dagen per appartement (Niels akkoord met het voorstel; benadert de vroegere vaste duren bij ongeveer 12 appartementen):
--     Pleisterwerken 2 · Vloerisolatie 0,5 · Chape 1 · Tegelwerk 2 · Parket 1 · Binnenschrijnwerk 1 · Keuken 1 · Schilderwerk 2
--     (de techniektaken uit 173 en 174 staan al op: elektriciteit 1, sanitair 1, verwarming 2).
--   De duur in de planning is dagen x het aantal hoofdeenheden van de planning, minstens 1 dag. De vaste duur blijft in de kolom Duur staan
--   voor wie in de editor terugschakelt naar een vaste duur.
-- Vereist: 174 (kolom DuurPerEenheid). Idempotent. Het sjabloon "Woning" (estafette per woning) blijft ongewijzigd.
-- =============================================
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
INSERT INTO @taken (Code, DagenPerEenheid) VALUES
 (N'PLEISTER', 2), (N'ISOLATIE', 0.5), (N'CHAPE', 1), (N'TEGELWERK', 2), (N'PARKET', 1),
 (N'BINNENSCHRIJNWERK', 1), (N'KEUKEN', 1), (N'SCHILDERWERK', 2);

UPDATE t SET DuurPerEenheid = x.DagenPerEenheid
FROM dbo.PlanningSjabloonTaak t JOIN dbo.PlanningSjabloonBlok b ON b.Id = t.BlokId JOIN @taken x ON x.Code = t.Code
WHERE b.SjabloonId = @s AND b.Code = N'B_AFWERKING' AND (t.DuurPerEenheid IS NULL OR t.DuurPerEenheid <> x.DagenPerEenheid);
PRINT N'Taken van de afwerking op duur per eenheid gezet: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

COMMIT;
PRINT N'KLAAR. Pas het projectsjabloon opnieuw toe (Vervang) om de nieuwe duren te zien.';
