#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

// Website-inhoud per project (publieke projectpagina). 1 rij per project; zie _migrations/083_ProjectWebsite.sql.
public partial class ProjectWebsite
{
    public int ProjectId { get; set; }

    public string Location { get; set; }

    public string Title { get; set; }

    public string Subtitle { get; set; }

    public string IntroText { get; set; }

    // Gekozen projectfoto (bestandsnaam in "pictures") voor de interactieve projectkaart
    public string AerialImageName { get; set; }

    // Titel en omschrijving van het blok "De woningen" (leeg = standaard)
    public string HomesTitle { get; set; }

    public string HomesIntro { get; set; }

    // Volgorde + zichtbaarheid van de blokken: [{"key":"story","visible":true},...]
    public string BlocksJson { get; set; }

    // Blok Verhaal
    public bool ShowBrochure { get; set; }

    public string StoryTitle { get; set; }

    public string StoryText { get; set; }

    // Blok Architectuur
    public string ArchEyebrow { get; set; }

    public string ArchTitle { get; set; }

    public string ArchText { get; set; }

    // Blok Comfort en energie
    public string ComfortTitle { get; set; }

    public string ComfortSubtitle { get; set; }

    // Blok Zo verloopt de aankoop
    public string BuyTitle { get; set; }

    // Contactformulier
    public string ContactTitle { get; set; }

    public string ContactText { get; set; }

    // Blok Ligging
    public string LocationTitle { get; set; }

    public string LocationText { get; set; }

    public string LocationMunicipality { get; set; }

    public decimal? LocationLat { get; set; }

    public decimal? LocationLng { get; set; }

    public DateTime UpdatedOn { get; set; }

    public virtual Project Project { get; set; }
}

// Eigen kerncijfer (titel + tekst) op de publieke projectpagina.
public partial class ProjectWebsiteKpi
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string Title { get; set; }

    public string Text { get; set; }

    public int SortOrder { get; set; }

    public virtual Project Project { get; set; }
}

// Omtrek van een eenheid op de projectkaart: polygoon "x,y x,y" in % van de foto (0-100) + labelpositie.
public partial class ProjectWebsiteLot
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int UnitId { get; set; }

    public string Polygon { get; set; }

    public decimal LabelX { get; set; }

    public decimal LabelY { get; set; }

    public virtual Project Project { get; set; }

    public virtual Units Unit { get; set; }
}

// Beeld + tekst in het blok Verhaal.
public partial class ProjectWebsiteStoryItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ImageName { get; set; }
    public string Text { get; set; }
    public int SortOrder { get; set; }
    public virtual Project Project { get; set; }
}

// Quote (tekst + persoon) in het blok Architectuur.
public partial class ProjectWebsiteQuote
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Text { get; set; }
    public string Person { get; set; }
    public int SortOrder { get; set; }
    public virtual Project Project { get; set; }
}

// Detail (foto + titel + tekst) in het blok Architectuur.
public partial class ProjectWebsiteDetail
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ImageName { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public int SortOrder { get; set; }
    public virtual Project Project { get; set; }
}

// Puntje (titel + tekst) in het blok Comfort en energie.
public partial class ProjectWebsiteComfortItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public int SortOrder { get; set; }
    public virtual Project Project { get; set; }
}

// Vraag + antwoord (opgeschoonde HTML) in het blok Vragen.
public partial class ProjectWebsiteFaq
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Question { get; set; }
    public string Answer { get; set; }
    public int SortOrder { get; set; }
    public virtual Project Project { get; set; }
}

// Stap (titel + een zin) in het blok "Zo verloopt de aankoop".
public partial class ProjectWebsiteBuyStep
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public int SortOrder { get; set; }
    public virtual Project Project { get; set; }
}

// Puntje (titel + tekst) in het blok Ligging, bv. "Dorpsleven".
public partial class ProjectWebsiteGuideItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public int SortOrder { get; set; }
    public virtual Project Project { get; set; }
}

// Voorziening in de buurt (badge op de kaart): categorie school/shop/ov/groen/sport.
public partial class ProjectWebsitePoi
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Category { get; set; }
    public string Name { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string Distance { get; set; }
    public string TimeText { get; set; }
    public int SortOrder { get; set; }
    public virtual Project Project { get; set; }
}

// Belangrijke plaats met reistijd, bv. "Aalst" - "12 min".
public partial class ProjectWebsiteTravel
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Place { get; set; }
    public string Duration { get; set; }
    public int SortOrder { get; set; }
    public virtual Project Project { get; set; }
}
