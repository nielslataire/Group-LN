-- =============================================
-- Migratie: 141_ChangeOrderMinwerkRegels
-- Datum: 2026-11-11
-- Omschrijving: Minwerk als eigen regelsoort (design-handoff 28v). Het scherm toont aantal en prijs als positief getal; de knop vóór de
--   omschrijving geeft het teken (+ meerwerk, - minwerk). Opgeslagen blijft een minwerk een regel met een NEGATIEVE PRIJS (aantal positief),
--   zodat elke som (aantal x prijs x (1 + commissie)) in lijst, facturatie, nacalculatie en PDF ongewijzigd klopt.
--   Bestaande bedragen met een min worden daarnaar omgezet:
--     - negatief AANTAL: aantal en prijs wisselen van teken (het product blijft identiek, dus ook het bedrag);
--     - negatieve PRIJS met positief aantal is al een minwerk en blijft zoals het is.
--   De COMMISSIE van bestaande regels blijft ongewijzigd (verzonden/ondertekende documenten behouden hun bedragen); het scherm zet de
--   commissie van een minwerk pas op 0 % (aan kostprijs) zodra het concept opnieuw wordt opgeslagen.
--   Idempotent: na de eerste keer is er geen regel met een negatief aantal meer.
-- =============================================

UPDATE [dbo].[ChangeOrderDetail]
SET [Number] = -[Number],
    [Price]  = -[Price]
WHERE [Number] < 0;
PRINT 'Regels met een negatief aantal omgezet naar een minwerk (positief aantal, negatieve prijs).';
GO
