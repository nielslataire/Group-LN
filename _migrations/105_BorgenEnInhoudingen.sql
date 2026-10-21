-- =============================================
-- Migratie: 105_BorgenEnInhoudingen
-- Datum: 2026-10-21
-- Omschrijving: Nacalculatie, design-handoff 42h/42i/42j/42k/42l: borg op een factuur, extra inhouding, vrijgave van borgen.
--   Contract.GuaranteeReleaseVOPercentage : % van de borg dat bij de voorlopige oplevering vrijkomt (rest bij DO). NULL = 50.
--   IncommingInvoices.BorgInhouden/BorgPercentage/BorgBedrag/BorgAfwijkReden/BorgVoPercentage : borg op deze factuur. BorgInhouden NULL = factuur van
--       vóór deze module (de borg wordt dan nog afgeleid uit het contractpercentage, zoals voorheen).
--   IncommingInvoiceInhouding            : extra inhouding op een factuur (tijdelijk niet uitbetalen), met reden.
--   IncommingInvoiceInhoudingAfhandeling : elke stap waarmee die inhouding wordt afgehandeld (vrijgeven, verrekenen met creditnota, afboeken).
--   ContractBorgVrijgave                 : elke vrijgave van borg (meerdere deelvrijgaves mogelijk). Neemt de waarden over uit de vier
--       kolommen Contract.GuaranteeReleased* van migratie 087 (die kolommen blijven bestaan maar worden niet meer gelezen).
--   Strikt additief + idempotent. Draai dit NA 087.
-- =============================================

IF COL_LENGTH('dbo.Contract', 'GuaranteeReleaseVOPercentage') IS NULL
BEGIN
    ALTER TABLE [dbo].[Contract] ADD [GuaranteeReleaseVOPercentage] DECIMAL(5,2) NULL;
    PRINT 'Kolom Contract.GuaranteeReleaseVOPercentage toegevoegd.';
END
ELSE PRINT 'Kolom Contract.GuaranteeReleaseVOPercentage bestaat al, overgeslagen.';
GO

IF COL_LENGTH('dbo.IncommingInvoices', 'BorgInhouden') IS NULL
BEGIN
    ALTER TABLE [dbo].[IncommingInvoices] ADD [BorgInhouden] BIT NULL;
    PRINT 'Kolom IncommingInvoices.BorgInhouden toegevoegd.';
END
ELSE PRINT 'Kolom IncommingInvoices.BorgInhouden bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.IncommingInvoices', 'BorgPercentage') IS NULL
BEGIN
    ALTER TABLE [dbo].[IncommingInvoices] ADD [BorgPercentage] DECIMAL(5,2) NULL;
    PRINT 'Kolom IncommingInvoices.BorgPercentage toegevoegd.';
END
ELSE PRINT 'Kolom IncommingInvoices.BorgPercentage bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.IncommingInvoices', 'BorgBedrag') IS NULL
BEGIN
    ALTER TABLE [dbo].[IncommingInvoices] ADD [BorgBedrag] DECIMAL(18,2) NULL;
    PRINT 'Kolom IncommingInvoices.BorgBedrag toegevoegd.';
END
ELSE PRINT 'Kolom IncommingInvoices.BorgBedrag bestaat al, overgeslagen.';
GO
IF COL_LENGTH('dbo.IncommingInvoices', 'BorgAfwijkReden') IS NULL
BEGIN
    ALTER TABLE [dbo].[IncommingInvoices] ADD [BorgAfwijkReden] NVARCHAR(500) NULL;
    PRINT 'Kolom IncommingInvoices.BorgAfwijkReden toegevoegd.';
END
ELSE PRINT 'Kolom IncommingInvoices.BorgAfwijkReden bestaat al, overgeslagen.';
GO

IF COL_LENGTH('dbo.IncommingInvoices', 'BorgVoPercentage') IS NULL
BEGIN
    -- Enkel voor een borg zonder contract (er is dan geen contractregel): % dat bij de voorlopige oplevering vrijkomt.
    ALTER TABLE [dbo].[IncommingInvoices] ADD [BorgVoPercentage] DECIMAL(5,2) NULL;
    PRINT 'Kolom IncommingInvoices.BorgVoPercentage toegevoegd.';
END
ELSE PRINT 'Kolom IncommingInvoices.BorgVoPercentage bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.IncommingInvoiceInhouding', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IncommingInvoiceInhouding] (
        [Id]                 INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_IncommingInvoiceInhouding] PRIMARY KEY,
        [IncommingInvoiceId] INT NOT NULL,
        [Bedrag]             DECIMAL(18,2) NOT NULL,
        -- 1 = creditnota te ontvangen, 2 = werk niet volledig uitgevoerd, 3 = andere
        [Reden]              TINYINT NOT NULL,
        [Toelichting]        NVARCHAR(500) NULL,
        [CreatedAt]          DATETIME2(0) NOT NULL CONSTRAINT [DF_IncommingInvoiceInhouding_CreatedAt] DEFAULT (sysutcdatetime()),
        [CreatedBy]          NVARCHAR(100) NULL,
        CONSTRAINT [FK_IncommingInvoiceInhouding_Invoice] FOREIGN KEY ([IncommingInvoiceId]) REFERENCES [dbo].[IncommingInvoices]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_IncommingInvoiceInhouding_Invoice] ON [dbo].[IncommingInvoiceInhouding] ([IncommingInvoiceId]);
    PRINT 'Tabel IncommingInvoiceInhouding aangemaakt.';
END
ELSE PRINT 'Tabel IncommingInvoiceInhouding bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.IncommingInvoiceInhoudingAfhandeling', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IncommingInvoiceInhoudingAfhandeling] (
        [Id]                 INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_IncommingInvoiceInhoudingAfhandeling] PRIMARY KEY,
        [InhoudingId]        INT NOT NULL,
        [Datum]              DATE NOT NULL,
        -- 1 = vrijgegeven (betalen), 2 = verrekend met creditnota, 3 = afgeboekt (niet betalen)
        [Soort]              TINYINT NOT NULL,
        -- Deel van de inhouding dat met deze stap is afgehandeld.
        [Bedrag]             DECIMAL(18,2) NOT NULL,
        -- Bij vrijgeven: wat daarvan als borg wordt ingehouden (contractpercentage). 0 bij verrekenen/afboeken.
        [BorgBedrag]         DECIMAL(18,2) NOT NULL CONSTRAINT [DF_InhoudingAfhandeling_Borg] DEFAULT (0),
        [CreditnotaInvoiceId] INT NULL,
        [Opmerking]          NVARCHAR(500) NULL,
        [CreatedAt]          DATETIME2(0) NOT NULL CONSTRAINT [DF_InhoudingAfhandeling_CreatedAt] DEFAULT (sysutcdatetime()),
        [CreatedBy]          NVARCHAR(100) NULL,
        CONSTRAINT [FK_InhoudingAfhandeling_Inhouding] FOREIGN KEY ([InhoudingId]) REFERENCES [dbo].[IncommingInvoiceInhouding]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InhoudingAfhandeling_Creditnota] FOREIGN KEY ([CreditnotaInvoiceId]) REFERENCES [dbo].[IncommingInvoices]([Id])
    );
    CREATE INDEX [IX_InhoudingAfhandeling_Inhouding] ON [dbo].[IncommingInvoiceInhoudingAfhandeling] ([InhoudingId]);
    CREATE INDEX [IX_InhoudingAfhandeling_Creditnota] ON [dbo].[IncommingInvoiceInhoudingAfhandeling] ([CreditnotaInvoiceId]);
    PRINT 'Tabel IncommingInvoiceInhoudingAfhandeling aangemaakt.';
END
ELSE PRINT 'Tabel IncommingInvoiceInhoudingAfhandeling bestaat al, overgeslagen.';
GO

IF OBJECT_ID('dbo.ContractBorgVrijgave', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ContractBorgVrijgave] (
        [Id]         INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ContractBorgVrijgave] PRIMARY KEY,
        [ContractId] INT NOT NULL,
        -- 1 = voorlopige oplevering, 2 = definitieve oplevering, 3 = andere (bv. vervroegde deelvrijgave)
        [Fase]       TINYINT NOT NULL,
        [Datum]      DATE NOT NULL,
        [Bedrag]     DECIMAL(18,2) NOT NULL,
        [Opmerking]  NVARCHAR(500) NULL,
        [CreatedAt]  DATETIME2(0) NOT NULL CONSTRAINT [DF_ContractBorgVrijgave_CreatedAt] DEFAULT (sysutcdatetime()),
        [CreatedBy]  NVARCHAR(100) NULL,
        CONSTRAINT [FK_ContractBorgVrijgave_Contract] FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contract]([ID]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ContractBorgVrijgave_Contract] ON [dbo].[ContractBorgVrijgave] ([ContractId]);
    PRINT 'Tabel ContractBorgVrijgave aangemaakt.';
END
ELSE PRINT 'Tabel ContractBorgVrijgave bestaat al, overgeslagen.';
GO

-- Bestaande vrijgaves uit migratie 087 overnemen (eenmalig: enkel als het contract nog geen rij voor die fase heeft).
IF COL_LENGTH('dbo.Contract', 'GuaranteeReleasedVOAt') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[ContractBorgVrijgave] ([ContractId], [Fase], [Datum], [Bedrag], [Opmerking])
    SELECT c.[ID], 1, c.[GuaranteeReleasedVOAt], c.[GuaranteeReleasedVOAmount], N'overgenomen uit migratie 087'
      FROM [dbo].[Contract] c
     WHERE c.[GuaranteeReleasedVOAt] IS NOT NULL AND c.[GuaranteeReleasedVOAmount] IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [dbo].[ContractBorgVrijgave] v WHERE v.[ContractId] = c.[ID] AND v.[Fase] = 1);
    PRINT CONCAT('Vrijgaves VO overgenomen: ', @@ROWCOUNT);

    INSERT INTO [dbo].[ContractBorgVrijgave] ([ContractId], [Fase], [Datum], [Bedrag], [Opmerking])
    SELECT c.[ID], 2, c.[GuaranteeReleasedDOAt], c.[GuaranteeReleasedDOAmount], N'overgenomen uit migratie 087'
      FROM [dbo].[Contract] c
     WHERE c.[GuaranteeReleasedDOAt] IS NOT NULL AND c.[GuaranteeReleasedDOAmount] IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [dbo].[ContractBorgVrijgave] v WHERE v.[ContractId] = c.[ID] AND v.[Fase] = 2);
    PRINT CONCAT('Vrijgaves DO overgenomen: ', @@ROWCOUNT);
END
GO
