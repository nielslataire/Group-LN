-- =====================================================================================
-- INVENTARIS — de twee koppelmechanismen op Units. LEEST ALLEEN, wijzigt NIETS.
--
-- Geen genummerde migratie: dit hoort niet in de uitrolreeks. Het is de nulmeting die
-- moet bepalen of het oude KOPPELING-mechanisme (Units.IsLink / Units.LinkedUnitId)
-- überhaupt nog in gebruik is, en of het veilig uitgefaseerd kan worden ten voordele van
-- Units.AttachedUnitId — het mechanisme waarop Projecten/DetailUnitsV2 (design-handoff
-- punt 16) en Projecten/DetailV2 hun eenhedenboom bouwen.
--
-- De twee mechanismen, kort:
--   A  Units.AttachedUnitId  → een berging/parking hangt ONDER een lot. De eenheid houdt
--                              haar eigen naam, prijs, aandeel en status. Eén extra
--                              kolomwaarde, geen extra rij.
--   B  Units.IsLink = 1      → een EXTRA Units-rij die staat voor "deze eenheden worden
--      + Units.LinkedUnitId    samen verkocht". De leden krijgen LinkedUnitId = die rij.
--                              De pseudo-rij krijgt bij het opslaan een samengestelde naam
--                              ("Lot 1 - Berging B1"), het aandeel als SOM van de leden, en
--                              het type/niveau van het eerste lid — plus haar eigen
--                              grond-/bouwwaarde.
--
-- Draai de blokken hieronder in volgorde en lees ze samen: als 1 nul rijen geeft, is er
-- niets uit te faseren en kan mechanisme B gewoon uit de UI verdwijnen.
--
-- UITGEVOERD op db_ab5fbb_testdb, 2026-09-25. Uitkomst in DESIGN.md ("Nulmeting op de
-- testdatabase"): 11 koppelingen / 22 leden in 3 projecten, GEEN van de conflictgevallen
-- uit blok 3, 4 en 5, maar wél 57 factuurlijnen die naar pseudo-rijen verwijzen (blok 6) en
-- een aandeeltotaal dat op de legacy pagina te hoog uitkomt en op DetailUnitsV2 exact klopt.
-- Bewaar dit script: na elke wijziging aan de koppelmechanismen is dit de hercontrole.
-- =====================================================================================

-- ── 1. Bestaan er KOPPELING-pseudo-eenheden, en in welke projecten? ──────────────────
SELECT  p.ProjectId,
        p.ProjectName,
        COUNT(*)                                AS AantalKoppelingen,
        SUM(CASE WHEN u.ClientAccountId IS NOT NULL THEN 1 ELSE 0 END) AS WaarvanVerkocht,
        SUM(CASE WHEN u.Landshare > 0           THEN 1 ELSE 0 END)     AS WaarvanMetAandeel,
        SUM(CASE WHEN ISNULL(u.LandValue,0) + ISNULL(u.ConstructionValue,0) > 0
                 THEN 1 ELSE 0 END)             AS WaarvanMetEigenBedrag
FROM    dbo.Units   u
JOIN    dbo.Project p ON p.ProjectId = u.ProjectId
WHERE   u.IsLink = 1
GROUP BY p.ProjectId, p.ProjectName
ORDER BY AantalKoppelingen DESC;

-- ── 2. Elke KOPPELING met haar leden — hier zie je of er een natuurlijke hoofdeenheid
--      in zit (een wooneenheid, GroupId 1) om de leden onder te hangen bij een migratie.
--      Een koppeling die alleen uit nevenruimtes bestaat, heeft die niet. ──────────────
SELECT  k.ProjectId,
        k.Id                AS KoppelingId,
        k.Name              AS KoppelingNaam,
        k.Landshare         AS KoppelingAandeel,
        k.LandValue         AS KoppelingGrondwaarde,
        k.ConstructionValue AS KoppelingBouwwaarde,
        k.ClientAccountId   AS KoppelingKlant,
        lid.Id              AS LidId,
        lid.Name            AS LidNaam,
        t.Name              AS LidType,
        t.GroupId           AS LidGroupId,   -- 1 = wooneenheid, 4 = commercieel, 2 = berging, 3 = parking
        lid.Landshare       AS LidAandeel,
        lid.LandValue       AS LidGrondwaarde,
        lid.ClientAccountId AS LidKlant,
        lid.AttachedUnitId  AS LidHangtOokOnder  -- niet NULL = het conflict uit blok 4
FROM    dbo.Units      k
LEFT JOIN dbo.Units    lid ON lid.LinkedUnitId = k.Id
LEFT JOIN dbo.UnitTypes t  ON t.Id = lid.TypeId
WHERE   k.IsLink = 1
ORDER BY k.ProjectId, k.Id, t.GroupId, lid.Name;

-- ── 3. Wees-situaties: rijen die het mechanisme half gebruiken. Beide lijsten horen
--      leeg te zijn; staat er iets in, dan is dat data die geen enkel scherm nog toont. ─
SELECT 'IsLink zonder leden' AS Soort, u.ProjectId, u.Id, u.Name
FROM   dbo.Units u
WHERE  u.IsLink = 1
  AND  NOT EXISTS (SELECT 1 FROM dbo.Units m WHERE m.LinkedUnitId = u.Id)
UNION ALL
SELECT 'LinkedUnitId naar een niet-IsLink-rij', u.ProjectId, u.Id, u.Name
FROM   dbo.Units u
JOIN   dbo.Units k ON k.Id = u.LinkedUnitId
WHERE  ISNULL(k.IsLink, 0) = 0;

-- ── 4. HET CONFLICT: een eenheid die zowel onder een lot hangt (A) als lid is van een
--      KOPPELING (B). Zulke rijen worden dubbel geteld — één keer in de prijs van het lot
--      waaronder ze hangt, één keer in de eigen bedragen van de pseudo-rij.
--      Sinds 2026-09-25 kan GEEN van beide dialogen dit meer aanmaken: de nieuwe (punt 16c)
--      bood al enkel nevenruimtes aan die nog nergens aan hangen, en de oude gebruikt nu
--      GetUnitsForLinkSelect i.p.v. GetUnitsByProjectIdForSelect — die filterde alleen op
--      eenheidstype. Deze query blijft de hercontrole. ─────────────────────────────────
SELECT  u.ProjectId, u.Id, u.Name,
        u.AttachedUnitId, lot.Name  AS HangtOnder,
        u.LinkedUnitId,   k.Name    AS LidVanKoppeling
FROM    dbo.Units u
LEFT JOIN dbo.Units lot ON lot.Id = u.AttachedUnitId
LEFT JOIN dbo.Units k   ON k.Id   = u.LinkedUnitId
WHERE   u.AttachedUnitId IS NOT NULL
  AND   u.LinkedUnitId   IS NOT NULL;

-- ── 5. Genest: een KOPPELING die zelf lid is van een andere KOPPELING. Mogelijk omdat de
--      pseudo-rij het TypeId van haar eerste lid overneemt en dus in de keuzelijst van de
--      oude dialoog opduikt. ──────────────────────────────────────────────────────────
SELECT u.ProjectId, u.Id, u.Name AS KoppelingNaam, u.LinkedUnitId, k.Name AS LidVanKoppeling
FROM   dbo.Units u
JOIN   dbo.Units k ON k.Id = u.LinkedUnitId
WHERE  u.IsLink = 1;

-- ── 6. Waar hangt een KOPPELING-rij nog aan vast? Dit bepaalt of ze bij een migratie
--      verwijderd KAN worden. Units.AttachedUnitId/LinkedUnitId worden door
--      UnitService.DeleteUnit zelf al losgemaakt, maar deze verwijzingen niet. ─────────
SELECT  k.ProjectId, k.Id AS KoppelingId, k.Name,
        (SELECT COUNT(*) FROM dbo.UnitConstructionValue x WHERE x.UnitId = k.Id) AS Bouwwaarderegels,
        (SELECT COUNT(*) FROM dbo.UnitFinishingOption   x WHERE x.UnitId = k.Id) AS Afwerkingsopties,
        (SELECT COUNT(*) FROM dbo.UnitRooms             x WHERE x.UnitId = k.Id) AS Ruimtes,
        (SELECT COUNT(*) FROM dbo.UnitExecutionPlan     x WHERE x.UnitId = k.Id) AS Uitvoeringsplannen,
        (SELECT COUNT(*) FROM dbo.InvoicesDetails       x WHERE x.UnitId = k.Id) AS Factuurlijnen,
        (SELECT COUNT(*) FROM dbo.Mijlpaal              x WHERE x.UnitId = k.Id) AS Mijlpalen,
        (SELECT COUNT(*) FROM dbo.ProjectDossier        x WHERE x.UnitId = k.Id) AS Dossiers,
        (SELECT COUNT(*) FROM dbo.ProjectTaak           x WHERE x.UnitId = k.Id) AS Taken,
        (SELECT COUNT(*) FROM dbo.ConstructionIssue     x WHERE x.UnitId = k.Id) AS Punten,
        (SELECT COUNT(*) FROM dbo.ProjectPictures       x WHERE x.UnitId = k.Id) AS Medias,
        (SELECT COUNT(*) FROM dbo.ProjectConnectionKey  x WHERE x.UnitId = k.Id) AS Nutssleutels,
        (SELECT COUNT(*) FROM dbo.ProjectNutsAansluiting x WHERE x.UnitId = k.Id) AS Nutsaansluitingen,
        (SELECT COUNT(*) FROM dbo.BudgetVerkoopLijnen   x WHERE x.UnitId = k.Id) AS Budgetverkooplijnen,
        (SELECT COUNT(*) FROM dbo.ContactRequests       x WHERE x.UnitId = k.Id) AS Contactaanvragen
FROM    dbo.Units k
WHERE   k.IsLink = 1
ORDER BY k.ProjectId, k.Id;

-- ── 7. Referentiecijfer voor mechanisme A, zodat je de twee kan vergelijken. ──────────
SELECT  p.ProjectId, p.ProjectName,
        SUM(CASE WHEN u.AttachedUnitId IS NOT NULL THEN 1 ELSE 0 END) AS GekoppeldViaAttachedUnitId,
        SUM(CASE WHEN u.LinkedUnitId   IS NOT NULL THEN 1 ELSE 0 END) AS LidVanKoppeling,
        SUM(CASE WHEN u.IsLink = 1                 THEN 1 ELSE 0 END) AS Koppelingen,
        COUNT(*)                                                      AS TotaalRijen
FROM    dbo.Units   u
JOIN    dbo.Project p ON p.ProjectId = u.ProjectId
GROUP BY p.ProjectId, p.ProjectName
HAVING  SUM(CASE WHEN u.AttachedUnitId IS NOT NULL THEN 1 ELSE 0 END)
      + SUM(CASE WHEN u.LinkedUnitId   IS NOT NULL THEN 1 ELSE 0 END) > 0
ORDER BY p.ProjectName;
