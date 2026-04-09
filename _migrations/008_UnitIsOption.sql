-- =============================================================================
-- 008_UnitIsOption.sql
-- Eenheden: IsOption kolom toevoegen voor "in optie" status
-- =============================================================================

ALTER TABLE Units
    ADD IsOption BIT NOT NULL DEFAULT 0;
