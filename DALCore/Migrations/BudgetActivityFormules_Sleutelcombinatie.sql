-- ============================================================
-- BudgetActivityFormule — sleutelcombinaties
-- (aantal wooneenheden + aantal commerciële eenheden) × kostprijs sleutelcombinatie.
-- @aantal_wooncomm telt precies die twee groepen samen (zie BudgetActivityFormuleService).
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM [dbo].[BudgetActivityFormule] WHERE [ActivityId] = 208)
    INSERT INTO [dbo].[BudgetActivityFormule] ([ActivityId], [Omschrijving], [Formule]) VALUES
    (208, N'Sleutelcombinaties', N'@mat_sleutelcombinatie * @aantal_wooncomm');

GO
