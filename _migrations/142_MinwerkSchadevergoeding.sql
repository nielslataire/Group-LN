-- =============================================
-- Migratie: 142_MinwerkSchadevergoeding
-- Datum: 2026-11-11
-- Omschrijving: Schadevergoeding op minwerk (design-handoff 28v). Bij een minwerk krijgt de klant soms maar een deel van de waarde terug.
--   - ChangeOrderDetail.DamagePercentage: het percentage schadevergoeding (NULL = geen).
--   - ChangeOrderDetail.DamageGoesTo: 1 = bouwheer (de leverancier crediteert de volle waarde, de vergoeding blijft bij de bouwheer),
--     2 = leverancier (de leverancier crediteert enkel wat de klant terugkrijgt).
--   - ChangeOrderDetail.ValuePrice: de eenheidsprijs van de waarde van het minwerk VOOR de vergoeding (positief). ChangeOrderDetail.Price blijft
--     wat de KLANT ziet en krijgt terug (negatief, = -ValuePrice x (1 - %)), zodat elke bestaande som (PDF, portaal, facturatie, btw op het
--     eindbedrag) ongewijzigd klopt: de klant ziet één minwerkregel aan het eindbedrag, geen vergoeding, geen percentage.
--   - Tabel ChangeOrderSettings (één rij): het standaardpercentage voor een nieuwe schadevergoeding
--     (Instellingen > Facturatie > Minwerk · standaard schadevergoeding).
--   Idempotent: tweemaal draaien doet niets extra.
-- =============================================
IF COL_LENGTH('dbo.ChangeOrderDetail', 'DamagePercentage') IS NULL
    ALTER TABLE [dbo].[ChangeOrderDetail] ADD [DamagePercentage] DECIMAL(5,2) NULL;
GO
IF COL_LENGTH('dbo.ChangeOrderDetail', 'DamageGoesTo') IS NULL
    ALTER TABLE [dbo].[ChangeOrderDetail] ADD [DamageGoesTo] TINYINT NULL;
GO
IF COL_LENGTH('dbo.ChangeOrderDetail', 'ValuePrice') IS NULL
    ALTER TABLE [dbo].[ChangeOrderDetail] ADD [ValuePrice] DECIMAL(19,4) NULL;
GO

IF OBJECT_ID('dbo.ChangeOrderSettings', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChangeOrderSettings]
    (
        [Id] INT NOT NULL CONSTRAINT [PK_ChangeOrderSettings] PRIMARY KEY,
        [MinworkDamagePercentage] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_ChangeOrderSettings_Damage] DEFAULT (25),
        CONSTRAINT [CK_ChangeOrderSettings_SingleRow] CHECK ([Id] = 1)
    );
    PRINT 'Tabel ChangeOrderSettings aangemaakt.';
END
GO
IF NOT EXISTS (SELECT 1 FROM [dbo].[ChangeOrderSettings] WHERE [Id] = 1)
    INSERT INTO [dbo].[ChangeOrderSettings] ([Id], [MinworkDamagePercentage]) VALUES (1, 25);
GO
