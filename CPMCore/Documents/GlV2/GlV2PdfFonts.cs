using QuestPDF.Drawing;

namespace CPMCore.Documents.GlV2
{
    /// <summary>
    /// Registreert Playfair Display (titel) en IBM Plex Sans (al de rest) één keer per proces —
    /// zelfde lock-patroon als <see cref="GroupLnFonts"/>, maar losstaand: de bestaande Avenir-
    /// registratie voor de legacy werf-PDF's/offertes blijft ongewijzigd. Lettertypen staan als
    /// .ttf in <c>wwwroot/fonts</c> (Google Fonts, OFL-licentie, gratis voor commercieel gebruik):
    /// PlayfairDisplay-Medium.ttf (enige gewicht dat het ontwerp gebruikt, 500) en
    /// IBMPlexSans-Regular/-Medium/-SemiBold/-Bold.ttf (400/500/600/700).
    /// </summary>
    public static class GlV2PdfFonts
    {
        private static readonly object Lock = new();
        private static bool _registered;
        private static bool _available;

        /// <summary>True zodra beide families geregistreerd zijn (of al waren); false als één van
        /// de .ttf-bestanden ontbreekt — de aanroeper valt dan terug op het systeemfont in plaats
        /// van een halve huisstijl (enkel Plex, geen Playfair, of omgekeerd) te tonen.</summary>
        public static bool EnsureRegistered(IWebHostEnvironment env, ILogger logger)
        {
            lock (Lock)
            {
                if (_registered) return _available;
                var fontsRoot = Path.Combine(env.WebRootPath, "fonts");
                var required = new[]
                {
                    "PlayfairDisplay-Medium.ttf",
                    "IBMPlexSans-Regular.ttf", "IBMPlexSans-Medium.ttf", "IBMPlexSans-SemiBold.ttf", "IBMPlexSans-Bold.ttf",
                };
                try
                {
                    var allFound = true;
                    foreach (var f in required)
                    {
                        var fp = Path.Combine(fontsRoot, f);
                        if (!File.Exists(fp)) { allFound = false; continue; }
                        using var stream = File.OpenRead(fp);
                        FontManager.RegisterFont(stream);
                    }
                    _available = allFound;
                    if (!allFound)
                        logger.LogWarning("gl-v2 PDF-lettertypen (Playfair Display/IBM Plex Sans) ontbreken in wwwroot/fonts; documenten vallen terug op het systeemfont.");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "gl-v2 PDF-lettertypen konden niet geregistreerd worden.");
                    _available = false;
                }
                _registered = true;
                return _available;
            }
        }
    }
}
