using System.Collections.Generic;

namespace FacadeCore
{
    // Website-inhoud van een project (publieke projectpagina). Bewust een platte DTO: de pagina in
    // WWWCOPRO leest dezelfde tabellen met één kleine query, CPMCore bewerkt ze via dit model.
    public class ProjectWebsiteDto
    {
        public int ProjectId { get; set; }

        // Aangepaste locatielijn boven de titel, bv. "Vlekkem · Erpe-Mere · Oost-Vlaanderen"
        public string Location { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }

        // Inleidende tekst; alinea's gescheiden door een lege regel.
        public string IntroText { get; set; }

        // Gekozen projectfoto (bestandsnaam in "pictures") voor de interactieve projectkaart
        public string AerialImageName { get; set; }

        // Titel en omschrijving van het blok "De woningen" (leeg = standaard)
        public string HomesTitle { get; set; }

        public string HomesIntro { get; set; }

        // Volgorde + zichtbaarheid van de sorteerbare blokken. BlocksJson = formulierwaarde
        // ([{"key":"story","visible":true},...]); null = ongewijzigd laten.
        public string BlocksJson { get; set; }

        public List<ProjectWebsiteBlockDto> Blocks { get; set; } = new List<ProjectWebsiteBlockDto>();

        // Blok Verhaal
        public bool ShowBrochure { get; set; }

        public string StoryTitle { get; set; }

        public string StoryText { get; set; }

        public List<ProjectWebsiteStoryItemDto> StoryItems { get; set; } = new List<ProjectWebsiteStoryItemDto>();

        // Blok Architectuur
        public string ArchEyebrow { get; set; }

        public string ArchTitle { get; set; }

        public string ArchText { get; set; }

        // Blok Comfort en energie
        public string ComfortTitle { get; set; }

        public string ComfortSubtitle { get; set; }

        public List<ProjectWebsiteComfortItemDto> ComfortItems { get; set; } = new List<ProjectWebsiteComfortItemDto>();

        // Blok Vragen (FAQ)
        public List<ProjectWebsiteFaqItemDto> FaqItems { get; set; } = new List<ProjectWebsiteFaqItemDto>();

        // Blok Zo verloopt de aankoop (standaardtitel en -stappen tot er iets bewaard is)
        public string BuyTitle { get; set; }

        // Contactformulier (leeg = standaard)
        public string ContactTitle { get; set; }

        public string ContactText { get; set; }

        public List<ProjectWebsiteBuyStepDto> BuySteps { get; set; } = new List<ProjectWebsiteBuyStepDto>();

        // Blok Ligging
        public string LocationTitle { get; set; }

        public string LocationText { get; set; }

        // Gemeente voor "Wil je weten of ... bij je past?"
        public string LocationMunicipality { get; set; }

        // Kaartpunt van het project, als tekst met punt of komma (bv. 50,9155)
        public string LocationLat { get; set; }

        public string LocationLng { get; set; }

        public List<ProjectWebsiteGuideItemDto> GuideItems { get; set; } = new List<ProjectWebsiteGuideItemDto>();

        public List<ProjectWebsitePoiDto> Pois { get; set; } = new List<ProjectWebsitePoiDto>();

        public List<ProjectWebsiteTravelDto> TravelTimes { get; set; } = new List<ProjectWebsiteTravelDto>();

        public List<ProjectWebsiteQuoteDto> Quotes { get; set; } = new List<ProjectWebsiteQuoteDto>();

        public List<ProjectWebsiteDetailDto> Details { get; set; } = new List<ProjectWebsiteDetailDto>();

        // Omtrekken per eenheid. Het formulier stuurt ze als JSON mee (LotShapesJson), de service zet ze om.
        // LotShapesJson == null: het formulier stuurde geen kaart mee, bestaande omtrekken blijven ongewijzigd.
        public string LotShapesJson { get; set; }

        public List<ProjectWebsiteLotDto> LotShapes { get; set; } = new List<ProjectWebsiteLotDto>();

        // Aanwezig-marker in het formulier: enkel wanneer true wordt er bewaard, zodat het oude
        // Edit-scherm (zonder deze velden) bestaande website-inhoud nooit leegmaakt.
        public bool Present { get; set; }

        public List<ProjectWebsiteKpiDto> Kpis { get; set; } = new List<ProjectWebsiteKpiDto>();
    }

    public class ProjectWebsiteKpiDto
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }
    }

    public class ProjectWebsiteLotDto
    {
        public int UnitId { get; set; }

        // "x,y x,y x,y" in % van de foto (0-100)
        public string Polygon { get; set; }

        public decimal LabelX { get; set; }

        public decimal LabelY { get; set; }
    }

    public class ProjectWebsiteBlockDto
    {
        public string Key { get; set; }

        public string Label { get; set; }

        public bool Visible { get; set; }
    }

    public class ProjectWebsiteStoryItemDto
    {
        public string ImageName { get; set; }

        public string Text { get; set; }
    }

    public class ProjectWebsiteQuoteDto
    {
        public string Text { get; set; }

        public string Person { get; set; }
    }

    public class ProjectWebsiteDetailDto
    {
        public string ImageName { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }
    }

    public class ProjectWebsiteComfortItemDto
    {
        public string Title { get; set; }

        public string Text { get; set; }
    }

    public class ProjectWebsiteFaqItemDto
    {
        public string Question { get; set; }

        // Opgemaakte tekst (HTML); wordt in de service opgeschoond (enkel eenvoudige opmaak en links)
        public string Answer { get; set; }
    }

    public class ProjectWebsiteBuyStepDto
    {
        public string Title { get; set; }

        public string Text { get; set; }
    }

    public class ProjectWebsiteGuideItemDto
    {
        public string Title { get; set; }

        public string Text { get; set; }
    }

    public class ProjectWebsitePoiDto
    {
        // school | shop | ov | groen | sport
        public string Category { get; set; }

        public string Name { get; set; }

        public string Lat { get; set; }

        public string Lng { get; set; }

        public string Distance { get; set; }

        public string Time { get; set; }
    }

    public class ProjectWebsiteTravelDto
    {
        public string Place { get; set; }

        public string Duration { get; set; }
    }
}
