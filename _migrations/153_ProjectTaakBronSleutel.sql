-- =============================================
-- Migratie: 153_ProjectTaakBronSleutel
-- Omschrijving: de planning maakt een taak in Mijn taken voor de projectleider zodra een taak of fase die aan een
--   betalingsschijf hangt klaar is ("Schijf 3 is bereikt, klaarzetten voor facturatie?"). Zodra de schijf klaarstaat
--   verdwijnt die taak vanzelf. Om per schijf en planning niet dubbel aan te maken krijgt ProjectTaak een bronsleutel.
--   * Nieuwe nullable kolom ProjectTaak.BronSleutel (bv. "schijf:12:345" = planning 12, schijf 345) + index.
--   Additief en idempotent. Draai vóór app-start.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProjectTaak]') AND name = N'BronSleutel')
BEGIN
    ALTER TABLE [dbo].[ProjectTaak] ADD [BronSleutel] NVARCHAR(100) NULL;
    PRINT 'Kolom ProjectTaak.BronSleutel toegevoegd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectTaak_BronSleutel' AND object_id = OBJECT_ID(N'[dbo].[ProjectTaak]'))
    CREATE INDEX [IX_ProjectTaak_BronSleutel] ON [dbo].[ProjectTaak]([BronSleutel]);
GO
