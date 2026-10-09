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
}
