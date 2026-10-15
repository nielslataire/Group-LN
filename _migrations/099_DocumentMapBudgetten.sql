-- 099 — Documenten: vaste map "Budgetten" voor de budget-PDF's (PDF intern / bank uit stap 9, design-handoff 44f).
-- Idempotent; enkel een extra rij in DocumentFolders (geen schemawijziging).
IF NOT EXISTS (SELECT 1 FROM [dbo].[DocumentFolders] WHERE [Code] = N'budgetten')
BEGIN
    INSERT INTO [dbo].[DocumentFolders] ([Code],[Name],[Icon],[SortOrder],[ViewKind])
    VALUES (N'budgetten', N'Budgetten', N'calculator', 35, N'default');
    PRINT 'Map Budgetten aangemaakt.';
END
ELSE PRINT 'Map Budgetten bestaat al, overgeslagen.';
GO
