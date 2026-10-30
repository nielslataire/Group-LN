-- =============================================
-- Migratie: 126_RechtVerkoopInstellingen
-- Datum: 2026-10-29
-- Omschrijving: Eigen recht "Projecten > Verkoop > Instellingen" (Projects.SalesSettings) voor de pagina Projecten/SalesSettings,
--   los van "Te Koop" (Projects.ForSale). Lezen, schrijven en verwijderen zijn apart in te stellen bij de gebruikersrechten.
--   Dit script maakt het recht aan en kopieert de bestaande rechten op "Te Koop" (per rol en per gebruikersafwijking) naar het nieuwe recht,
--   zodat niemand na de update plots de verkoopinstellingen kwijt is. Pas ze daarna naar wens aan in de gebruikersrechten.
--   Strikt additief + idempotent: bestaande rijen voor het nieuwe recht worden niet overschreven.
--   Draai dit script VOOR je de nieuwe versie van CPMCore opstart.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[SecurityPermission] WHERE [Code] = 'Projects.SalesSettings')
BEGIN
    INSERT INTO [dbo].[SecurityPermission] ([Code], [Name], [ParentCode], [SortOrder], [IsActive])
    VALUES ('Projects.SalesSettings', 'Instellingen', 'Projects.Verkoop', 64, 1);
    PRINT 'Recht Projects.SalesSettings aangemaakt.';
END
ELSE PRINT 'Recht Projects.SalesSettings bestaat al, overgeslagen.';
GO

INSERT INTO [dbo].[SecurityRolePermission] ([RoleId], [PermissionCode], [CanRead], [CanWrite], [CanDelete])
SELECT r.[RoleId], 'Projects.SalesSettings', r.[CanRead], r.[CanWrite], r.[CanDelete]
FROM [dbo].[SecurityRolePermission] r
WHERE r.[PermissionCode] = 'Projects.ForSale'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[SecurityRolePermission] x WHERE x.[RoleId] = r.[RoleId] AND x.[PermissionCode] = 'Projects.SalesSettings');
PRINT 'Rolrechten gekopieerd: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO

INSERT INTO [dbo].[SecurityUserPermissionOverride] ([UserId], [PermissionCode], [CanRead], [CanWrite], [CanDelete])
SELECT o.[UserId], 'Projects.SalesSettings', o.[CanRead], o.[CanWrite], o.[CanDelete]
FROM [dbo].[SecurityUserPermissionOverride] o
WHERE o.[PermissionCode] = 'Projects.ForSale'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[SecurityUserPermissionOverride] x WHERE x.[UserId] = o.[UserId] AND x.[PermissionCode] = 'Projects.SalesSettings');
PRINT 'Gebruikersafwijkingen gekopieerd: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
GO
