-- =============================================
-- Migratie: 111_VerzekeringenAttestEenhedenEnTienjarigeKeuze
-- Datum: 2026-10-22
-- Omschrijving: Verzekeringen, vervolg op 110 (design-handoff punt 43b).
--   1. Contract.TienjarigeBron: de keuze op het contract "Tienjarige aansprakelijkheid" (NULL = nog niet ingevuld, CPM leidt het dan af uit de
--      activiteiten): 0 via de aannemer (attest per woning), 1 via de projectpolis (appartementen), 2 niet vereist.
--   2. InsuranceAttestUnit: voor welke woningen een tienjarige-attest van een aannemer geldt. Daaruit koppelt CPM het attest aan de koper van
--      elke woning (Documenten) en deelt het in het klantenportaal. Geen FK op UnitId (zou een tweede cascade-pad naar Project geven).
--   Strikt additief + idempotent. Draai 110 eerst.
-- =============================================
IF COL_LENGTH('dbo.Contract', 'TienjarigeBron') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [TienjarigeBron] INT NULL;
    PRINT 'Kolom Contract.TienjarigeBron toegevoegd.';
END
ELSE PRINT 'Kolom Contract.TienjarigeBron bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.InsuranceAttestUnit', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InsuranceAttestUnit] (
        [AttestId] INT NOT NULL,
        [UnitId]   INT NOT NULL,
        CONSTRAINT [PK_InsuranceAttestUnit] PRIMARY KEY ([AttestId], [UnitId]),
        CONSTRAINT [FK_InsuranceAttestUnit_Attest] FOREIGN KEY ([AttestId]) REFERENCES [dbo].[InsuranceAttest]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabel InsuranceAttestUnit aangemaakt.';
END
ELSE PRINT 'Tabel InsuranceAttestUnit bestaat al, overgeslagen.';
GO
