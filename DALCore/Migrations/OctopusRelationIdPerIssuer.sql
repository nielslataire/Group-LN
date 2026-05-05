-- ============================================================
-- OctopusRelationId verplaatst naar per-issuer junction tables
--
-- CompanyInfo.OctopusRelationId     → CompanyIssuerCompany.OctopusRelationId
-- ClientAccount.OctopusRelationId   → ClientAccountIssuerCompany.OctopusRelationId
-- ClientContacts.OctopusRelationId  → ClientContactIssuerCompany.OctopusRelationId
--
-- De junction tables bevatten al de correcte per-issuer OctopusRelationId.
-- Vóór het uitvoeren van dit script: verifieer dat alle bestaande
-- OctopusRelationId waarden op de entities al gemigreerd zijn naar de
-- junction tables voor het juiste issuer-bedrijf.
-- ============================================================

-- ── 1. Migreer bestaande entity-level waarden naar junction tables ────────────
-- (Alleen voor entiteiten die al een koppeling hebben met exact één issuer.
--  Voor entiteiten met meerdere issuers: handmatig controleren.)

-- CompanyInfo → CompanyIssuerCompany
UPDATE cic
SET    cic.OctopusRelationId = ci.OctopusRelationId
FROM   dbo.CompanyIssuerCompany cic
JOIN   dbo.CompanyInfo          ci  ON ci.CompanyID = cic.CompanyId
WHERE  ci.OctopusRelationId IS NOT NULL
  AND  cic.OctopusRelationId IS NULL;

GO

-- ClientAccount → ClientAccountIssuerCompany
UPDATE caic
SET    caic.OctopusRelationId = ca.OctopusRelationId
FROM   dbo.ClientAccountIssuerCompany caic
JOIN   dbo.ClientAccount              ca  ON ca.Id = caic.ClientAccountId
WHERE  ca.OctopusRelationId IS NOT NULL
  AND  caic.OctopusRelationId IS NULL;

GO

-- ClientContacts → ClientContactIssuerCompany
UPDATE ccic
SET    ccic.OctopusRelationId = cc.OctopusRelationId
FROM   dbo.ClientContactIssuerCompany ccic
JOIN   dbo.ClientContacts             cc  ON cc.Id = ccic.ClientContactId
WHERE  cc.OctopusRelationId IS NOT NULL
  AND  ccic.OctopusRelationId IS NULL;

GO

-- ── 2. Verwijder de entity-level indexen ─────────────────────────────────────

DROP INDEX IF EXISTS [IX_ClientAccount_OctopusRelationId]  ON [dbo].[ClientAccount];
DROP INDEX IF EXISTS [IX_ClientContacts_OctopusRelationId] ON [dbo].[ClientContacts];
DROP INDEX IF EXISTS [IX_CompanyInfo_OctopusRelationId]    ON [dbo].[CompanyInfo];

GO

-- ── 3. Verwijder de entity-level kolommen ────────────────────────────────────

ALTER TABLE [dbo].[ClientAccount]  DROP COLUMN [OctopusRelationId];
ALTER TABLE [dbo].[ClientContacts] DROP COLUMN [OctopusRelationId];
ALTER TABLE [dbo].[CompanyInfo]    DROP COLUMN [OctopusRelationId];

GO
