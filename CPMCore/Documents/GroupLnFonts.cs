using QuestPDF.Drawing;

namespace CPMCore.Documents
{
    /// <summary>
    /// Registreert Avenir één keer per proces (QuestPDF's <see cref="FontManager"/> is globaal) —
    /// gedeeld door elke <see cref="GroupLnPdfDocument"/>-afgeleide zodat niemand dezelfde TTF's een
    /// tweede keer registreert (dat gaf eerder een kringafhankelijkheids-vrije, maar dubbele,
    /// registratie tussen <c>ChangeOrderPdfBuilder</c> en de fase-2-renderer). Zonder een geregistreerd
    /// lettertype valt QuestPDF terug op een systeemfont dat "ti"/"tt"-ligaturen soms niet correct naar
    /// Unicode terugmapt (letters vallen weg, bv. "bevesgt" i.p.v. "bevestigt") — vandaar dat élk
    /// document hier expliciet Avenir opvraagt in plaats van de naam "Lato" door te geven.
    /// </summary>
    public static class GroupLnFonts
    {
        private static readonly object Lock = new();
        private static bool _registered;
        private static string? _fontFamily;

        public static string? EnsureAvenirRegistered(IWebHostEnvironment env, ILogger logger)
        {
            lock (Lock)
            {
                if (_registered) return _fontFamily;
                var fontsRoot = Path.Combine(env.WebRootPath, "fonts");
                var any = false;
                try
                {
                    foreach (var f in new[]
                    {
                        "Avenir-Roman.ttf", "Avenir-Medium.ttf", "Avenir-Heavy.ttf", "Avenir-Black.ttf",
                        "Avenir-Oblique.ttf", "Avenir-MediumOblique.ttf", "Avenir-HeavyOblique.ttf", "Avenir-BlackOblique.ttf"
                    })
                    {
                        var fp = Path.Combine(fontsRoot, f);
                        if (!File.Exists(fp)) continue;
                        using var stream = File.OpenRead(fp);
                        FontManager.RegisterFont(stream);
                        any = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Avenir-fonts konden niet geregistreerd worden; PDF's vallen terug op het standaardlettertype.");
                }
                _registered = true;
                _fontFamily = any ? "Avenir" : null;
                return _fontFamily;
            }
        }
    }
}
