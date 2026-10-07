using System;
using System.Text.RegularExpressions;
namespace CPMCore.Documents.GlV2
{
    /// <summary>
    /// Kleur- en typografietokens voor de gl-v2-documentlayout (design-handoff punt 36 "Opmaak-
    /// specificaties"). Eén bron voor elk QuestPDF-document onder <c>Documents/GlV2/</c>, zodat
    /// factuur, offerte/wijzigingsopdracht, klantenlijst, aannemerslijst, prijslijst en budget er
    /// identiek uitzien. Raakt bewust niets van <see cref="GroupLnPdfDocument"/> (de bestaande
    /// werf-PDF's) of de facturatie-pipeline in <c>ServiceCore/Invoicing/Pdf/</c> (LayoutA/B/HE,
    /// per-uitgever instelbaar — dat blijft het systeem voor de échte, verstuurde facturen).
    /// Namen en hex-waarden zijn letterlijk uit punt 36.1/36.2 overgenomen.
    /// </summary>
    public static class GlV2PdfTheme
    {
        // ── Lettertypes (punt 36.1) ──────────────────────────────────────────────
        /// <summary>Uitsluitend de documenttitel, 1× per pagina — zie <see cref="GlV2PdfFonts"/>.</summary>
        public const string Playfair = "Playfair Display";
        /// <summary>Al de rest: kickers, tabellen, meta, voet — gewichten 400/500/600/700.</summary>
        public const string Plex = "IBM Plex Sans";

        // ── Kleuren (punt 36.2) ──────────────────────────────────────────────────
        public const string Groen = "#00532D";          // banden, sectietitels, tabelkop-lijn, "Te betalen"
        public const string Inkt = "#1F2A1E";            // alle hoofdtekst
        public const string Gedempt = "#56664F";         // labels, subregels, wettelijke tekst, voet
        public const string Licht = "#8A978A";           // placeholders, legenda
        public const string Lijn = "#241F2A1E";          // scheidingslijnen (ARGB van rgba(31,42,30,.14))
        public const string VlakGroen = "#F3F7F0";       // betaalblok, opvallende kaders
        public const string VlakGroepsrij = "#EEF4EA";   // groepsrij in lijsten, btw-tag 6%
        public const string VlakWarm = "#F6EEDC";        // btw-tag 21%, aandachtspunt
        public const string AccentGroupLn = "#5DA935";   // uitsluitend de tagline onder het logo
        public const string Wit = "#FFFFFF";

        /// <summary>Geldige #RRGGBB-kleur? (bedrijfskleur uit de factuurinstellingen).</summary>
        public static bool IsHex(string? c) => !string.IsNullOrWhiteSpace(c) && Regex.IsMatch(c.Trim(), "^#[0-9a-fA-F]{6}$");

        /// <summary>Lichte tint van een kleur (mengen met wit): voor vlakken en tags in de huiskleur van een bedrijf.
        /// <paramref name="aandeel"/> = hoeveel van de kleur overblijft (0.08 ≈ #EEF4EA bij het standaardgroen).</summary>
        public static string Tint(string hex, double aandeel)
        {
            var c = hex.Trim().TrimStart('#');
            int Mix(int i) => (int)Math.Round(255 - (255 - Convert.ToInt32(c.Substring(i, 2), 16)) * aandeel);
            return $"#{Mix(0):X2}{Mix(2):X2}{Mix(4):X2}";
        }

        // Enkel bij de bedrijfsnaam naast het logo (punt 35b): eigen, iets lossere grijstint —
        // bewust niet "Gedempt", dat is specifiek voor labels/voet.
        public const string Bedrijfsnaam = "#4B4B4B";
        public const string BedrijfsnaamSub = "#6B6B6B";

        // ── Maten (punt 36.4/36.5, in mm) ────────────────────────────────────────
        public const float ZijMargeMm = 18f;
        public const float BandHoogteMm = 6f;
        /// <summary>Hoogte van het logo op de volledige kop (Niels, 2026-10-05: geen bedrijfsnaam
        /// ernaast, en groter dan het ontwerp's eigen 16mm-plaatshouder — leesbaarder voor een echt
        /// logobeeldmerk met interne witruimte).</summary>
        public const float KopHoogteMm = 28f;
        public const float LogoCompactMm = 10f;
        public const float AdresKolomBreedteMm = 72f;

        /// <summary>Nooit kleiner dan 6 pt; lopende tekst minstens 8 pt (punt 36.3's eigen regel).</summary>
        public const float MinPunten = 6f;
    }
}
