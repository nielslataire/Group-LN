-- =============================================
-- TEST_project79_opvullen.sql  (GEEN genummerde migratie: enkel voor het testproject)
-- Zet het testproject klaar voor de test van de planning, de schijven en het projectsjabloon. Draai dit NA TEST_project79_opruimen.sql.
--
-- WAT HET DOET (de bestaande eenheden blijven, ze worden niet opnieuw aangemaakt):
--   * maakt 2 betalingsgroepen zonder schijven: "TEST Lots" (eenheden met "Lot" in de naam) en "TEST Woningen" (eenheden met "Woning" in de naam)
--   * koppelt elke eenheid aan zijn groep (UnitConstructionValue) en geeft ze een bouwwaarde van 300.000 (verkocht: 300.000)
--   * zorgt dat elke eenheid een klant heeft ("verkocht"), anders kan een schijf niet bereikt gemarkeerd worden:
--     een eenheid zonder klant krijgt de klant van een andere eenheid van dit project, of anders een nieuwe klant "TEST Klant"
-- WAT HET NIET DOET: planning, traject en schijven aanmaken. Dat doe je met het projectsjabloon in de app.
--
-- HOE: zelfde als het opruimscript. Eerst proefrit (@Uitvoeren = 0, wordt teruggedraaid), daarna @Uitvoeren = 1.
-- Het script is herhaalbaar: een groep die al bestaat wordt niet nog eens aangemaakt.
-- Veiligheid: de projectnaam moet met "TEST" beginnen.
-- =============================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @ProjectId INT = 79;     -- <<< het testproject
DECLARE @Uitvoeren BIT = 0;      -- <<< 0 = proefrit (alles terugdraaien), 1 = echt bewaren

DECLARE @naam NVARCHAR(100) = (SELECT ProjectName FROM Project WHERE ProjectID = @ProjectId);
IF @naam IS NULL THROW 50010, N'Project niet gevonden.', 1;
IF @naam NOT LIKE N'TEST%' THROW 50011, N'De naam van het project begint niet met TEST: het script stopt uit voorzorg.', 1;
PRINT N'Project ' + CAST(@ProjectId AS NVARCHAR(20)) + N': ' + @naam;
PRINT CASE WHEN @Uitvoeren = 1 THEN N'MODUS: UITVOEREN (wordt bewaard)' ELSE N'MODUS: PROEFRIT (wordt teruggedraaid)' END;

BEGIN TRAN;

-- 1. Groepen
DECLARE @gLots INT = (SELECT TOP 1 Id FROM InvoicingPaymentGroup WHERE ProjectId = @ProjectId AND Name = N'TEST Lots');
IF @gLots IS NULL
BEGIN
    INSERT INTO InvoicingPaymentGroup (Name, ProjectId, VatPercentage) VALUES (N'TEST Lots', @ProjectId, 21);
    SET @gLots = SCOPE_IDENTITY();
    PRINT N'   groep aangemaakt: TEST Lots';
END
DECLARE @gWon INT = (SELECT TOP 1 Id FROM InvoicingPaymentGroup WHERE ProjectId = @ProjectId AND Name = N'TEST Woningen');
IF @gWon IS NULL
BEGIN
    INSERT INTO InvoicingPaymentGroup (Name, ProjectId, VatPercentage) VALUES (N'TEST Woningen', @ProjectId, 21);
    SET @gWon = SCOPE_IDENTITY();
    PRINT N'   groep aangemaakt: TEST Woningen';
END

-- 2. Klant voor eenheden die er nog geen hebben
DECLARE @klant INT = (SELECT TOP 1 ClientAccountId FROM Units WHERE ProjectId = @ProjectId AND ClientAccountId IS NOT NULL ORDER BY Id);
IF @klant IS NULL AND EXISTS (SELECT 1 FROM Units WHERE ProjectId = @ProjectId AND ClientAccountId IS NULL)
BEGIN
    SET @klant = (SELECT TOP 1 Id FROM ClientAccount WHERE Name = N'TEST Klant');
    IF @klant IS NULL
    BEGIN
        INSERT INTO ClientAccount (Name) VALUES (N'TEST Klant');
        SET @klant = SCOPE_IDENTITY();
        PRINT N'   klant aangemaakt: TEST Klant';
    END
END
UPDATE Units SET ClientAccountId = @klant WHERE ProjectId = @ProjectId AND ClientAccountId IS NULL AND @klant IS NOT NULL;
PRINT N'   eenheden die een klant kregen: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 3. Eenheden aan hun groep hangen, met een waarde
UPDATE c SET c.PaymentGroupId = CASE WHEN u.Name LIKE N'%Lot%' THEN @gLots ELSE @gWon END,
             c.Value = ISNULL(NULLIF(c.Value, 0), 300000), c.ValueSold = ISNULL(NULLIF(c.ValueSold, 0), 300000)
FROM UnitConstructionValue c JOIN Units u ON u.Id = c.UnitId
WHERE u.ProjectId = @ProjectId AND (u.Name LIKE N'%Lot%' OR u.Name LIKE N'%Woning%');
PRINT N'   bestaande bouwwaarden aan een groep gehangen: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

INSERT INTO UnitConstructionValue (UnitId, PaymentGroupId, Value, ValueSold, Description)
SELECT u.Id, CASE WHEN u.Name LIKE N'%Lot%' THEN @gLots ELSE @gWon END, 300000, 300000, N'TEST'
FROM Units u
WHERE u.ProjectId = @ProjectId AND (u.Name LIKE N'%Lot%' OR u.Name LIKE N'%Woning%')
  AND NOT EXISTS (SELECT 1 FROM UnitConstructionValue c WHERE c.UnitId = u.Id);
PRINT N'   nieuwe bouwwaarden aangemaakt: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- Controle
SELECT u.Id, u.Name, g.Name AS Groep, c.ValueSold AS BouwwaardeVerkocht, CASE WHEN u.ClientAccountId IS NULL THEN N'nee' ELSE N'ja' END AS Verkocht
FROM Units u
LEFT JOIN UnitConstructionValue c ON c.UnitId = u.Id
LEFT JOIN InvoicingPaymentGroup g ON g.Id = c.PaymentGroupId
WHERE u.ProjectId = @ProjectId ORDER BY u.Id;

IF @Uitvoeren = 1
BEGIN
    COMMIT;
    PRINT N'KLAAR: bewaard.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT N'PROEFRIT afgelopen: alles is teruggedraaid. Zet @Uitvoeren op 1 om het echt te doen.';
END
GO
