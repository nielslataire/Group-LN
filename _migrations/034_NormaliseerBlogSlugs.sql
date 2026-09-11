-- =============================================
-- Migratie: 034_NormaliseerBlogSlugs
-- Datum: 2026-09-11
-- Omschrijving: Herstelt bestaande BlogArtikel.Slug-waarden die met een hoofdletter
--   beginnen of rand-/dubbele spaties bevatten, naar dezelfde vorm die de site overal
--   in URL's/canonical/sitemap/social-share al toont (kleine letters, spaties -> streepjes).
--   Nodig omdat de vorige code-fixes (.ToLowerInvariant() bij weergave, GenereerSlug()
--   bij opslaan) enkel nieuwe/toekomstige gevallen dekken — niet wat er nu al in de
--   tabel staat. Dit is precies de oorzaak van de "Alternatieve pagina met correcte
--   canonieke tag"-melding in Search Console voor bv. energiezuinige-nieuwbouw-kopen-2026:
--   de opgeslagen Slug begint met een hoofdletter, terwijl canonical/sitemap/links al
--   kleine letters tonen, waardoor Google's crawl van de kleine-letter-URL een canonical-
--   tag terugkreeg die naar de hoofdletter-variant wijst — een URL waar verder nergens
--   nog naar gelinkt wordt.
--   Update enkel wanneer de genormaliseerde vorm nog niet door een ANDERE rij bezet is
--   (zodat de UNIQUE INDEX [UX_BlogArtikel_Slug] nooit kan breken); conflicten worden
--   overgeslagen en gerapporteerd, niet automatisch samengevoegd.
--   Strikt een data-fix, geen schemawijziging. Idempotent: een tweede uitvoering vindt
--   niets meer om te normaliseren.
-- =============================================

;WITH genormaliseerd AS (
    SELECT
        Id,
        Slug AS OudeSlug,
        LOWER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(Slug, '  ', ' '), '  ', ' '), ' ', '-')))) AS NieuweSlug
    FROM dbo.BlogArtikel
)
SELECT Id, OudeSlug, NieuweSlug
INTO #BlogSlugFixes
FROM genormaliseerd
WHERE NieuweSlug <> OudeSlug;

-- Rapporteer conflicten (twee rijen die na normalisatie dezelfde slug zouden krijgen)
-- vóór de update, zodat die zichtbaar blijven i.p.v. stil overgeslagen te worden.
IF EXISTS (
    SELECT 1 FROM #BlogSlugFixes f
    WHERE EXISTS (
        SELECT 1 FROM dbo.BlogArtikel other
        WHERE other.Id <> f.Id AND other.Slug = f.NieuweSlug
    )
)
BEGIN
    PRINT 'Let op: onderstaande artikels botsen na normalisatie met een bestaande slug en worden NIET aangepast — manueel bekijken:';
    SELECT f.Id, f.OudeSlug, f.NieuweSlug
    FROM #BlogSlugFixes f
    WHERE EXISTS (
        SELECT 1 FROM dbo.BlogArtikel other
        WHERE other.Id <> f.Id AND other.Slug = f.NieuweSlug
    );
END

UPDATE b
SET b.Slug = f.NieuweSlug
FROM dbo.BlogArtikel b
JOIN #BlogSlugFixes f ON f.Id = b.Id
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.BlogArtikel other
    WHERE other.Id <> b.Id AND other.Slug = f.NieuweSlug
);

PRINT CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' BlogArtikel-slug(s) genormaliseerd.';

DROP TABLE #BlogSlugFixes;

PRINT 'Migratie 034_NormaliseerBlogSlugs voltooid.';
