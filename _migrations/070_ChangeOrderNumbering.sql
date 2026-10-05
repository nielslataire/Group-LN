-- =============================================
-- Migratie: 070_ChangeOrderNumbering
-- Datum: 2026-10-05
-- Omschrijving: Publieke nummering van offertes (OF) en wijzigingsopdrachten (WO) los van het interne Id:
--   "OF-2026-014" / "WO-2026-006", met een versiesuffix voor bijgewerkte versies ("OF-2026-014-v2").
--   - Teller per PROJECT, per JAAR en per type (offerte/WO apart): NumberProjectId + NumberYear + NumberSeq.
--   - Een nieuwe versie van een document (SourceKind = 2) erft jaar/teller van de eerste versie en krijgt
--     VersionNo + 1; RootChangeOrderId wijst naar die eerste versie (NULL bij de eerste versie zelf).
--   - Bestaande rijen worden achteraf genummerd (op Id-volgorde binnen project/jaar/type).
--   - Nieuwe rijen krijgen hun nummer in cpmRunningContext.SaveChanges (DALCore/ChangeOrderNumbering).
--   Verwijderde nummers worden niet hergebruikt. Strikt additief + idempotent.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ChangeOrder') AND name = 'NumberSeq')
BEGIN
    ALTER TABLE [dbo].[ChangeOrder] ADD
        [NumberProjectId]     INT NULL,
        [NumberYear]          INT NULL,
        [NumberSeq]           INT NULL,
        [VersionNo]           INT NOT NULL CONSTRAINT [DF_ChangeOrder_VersionNo] DEFAULT (1),
        [RootChangeOrderId]   INT NULL;
    PRINT 'Nummeringskolommen toegevoegd aan ChangeOrder.';
END
ELSE PRINT 'Nummeringskolommen bestaan al, overgeslagen.';
GO

-- Backfill (enkel rijen zonder nummer).
IF EXISTS (SELECT 1 FROM [dbo].[ChangeOrder] WHERE [NumberSeq] IS NULL)
BEGIN
    -- 1. versieketens: eerste versie = geen SourceKind 2 (of geen bron); latere = SourceKind 2.
    ;WITH chain AS (
        SELECT c.Id, CAST(NULL AS INT) AS RootId, 1 AS Ver, c.Id AS TopId
        FROM [dbo].[ChangeOrder] c
        WHERE ISNULL(c.SourceKind, 0) <> 2 OR c.SourceChangeOrderId IS NULL
        UNION ALL
        SELECT c.Id, ch.TopId, ch.Ver + 1, ch.TopId
        FROM [dbo].[ChangeOrder] c
        JOIN chain ch ON c.SourceChangeOrderId = ch.Id
        WHERE c.SourceKind = 2
    )
    UPDATE co SET co.RootChangeOrderId = ch.RootId, co.VersionNo = ch.Ver
    FROM [dbo].[ChangeOrder] co JOIN chain ch ON ch.Id = co.Id
    WHERE co.NumberSeq IS NULL;

    -- 2. project per rij
    UPDATE co SET co.NumberProjectId = k.ProjectId
    FROM [dbo].[ChangeOrder] co
    JOIN [dbo].[ContractActivity] a ON a.Id = co.ContractActivityId
    JOIN [dbo].[Contract] k ON k.Id = a.ContractId
    WHERE co.NumberSeq IS NULL;

    -- 3. teller voor de eerste versies, per project/type/jaar op Id-volgorde
    ;WITH roots AS (
        SELECT Id, ROW_NUMBER() OVER (PARTITION BY NumberProjectId, IsQuote, YEAR([Date]) ORDER BY Id) AS rn, YEAR([Date]) AS yr
        FROM [dbo].[ChangeOrder]
        WHERE NumberSeq IS NULL AND RootChangeOrderId IS NULL
    )
    UPDATE co SET co.NumberYear = r.yr, co.NumberSeq = r.rn
    FROM [dbo].[ChangeOrder] co JOIN roots r ON r.Id = co.Id;

    -- 4. latere versies erven jaar/teller van de eerste versie
    UPDATE v SET v.NumberYear = r.NumberYear, v.NumberSeq = r.NumberSeq
    FROM [dbo].[ChangeOrder] v JOIN [dbo].[ChangeOrder] r ON r.Id = v.RootChangeOrderId
    WHERE v.NumberSeq IS NULL;

    PRINT 'Bestaande offertes/wijzigingsopdrachten genummerd.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ChangeOrder_Number' AND object_id = OBJECT_ID('dbo.ChangeOrder'))
BEGIN
    CREATE UNIQUE INDEX [UX_ChangeOrder_Number]
        ON [dbo].[ChangeOrder] ([NumberProjectId], [IsQuote], [NumberYear], [NumberSeq], [VersionNo])
        WHERE [NumberSeq] IS NOT NULL AND [NumberProjectId] IS NOT NULL;
    PRINT 'Unieke index UX_ChangeOrder_Number aangemaakt.';
END
GO
