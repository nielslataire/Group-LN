using System.Text.RegularExpressions;

namespace CPMCore.Service
{
    /// <summary>
    /// Schoont Word/Outlook-HTML op (bv. een geplakte Outlook-handtekening): Outlook zelf
    /// gebruikt de Word-renderengine en begrijpt VML, dus het toont de "&lt;!--[if gte vml 1]--&gt;"-tak
    /// (een &lt;v:imagedata&gt; die verwijst naar een lokaal bestand dat nooit meeverstuurd is) in
    /// plaats van de werkende &lt;img src="data:..."&gt; fallback uit "&lt;![if !vml]&gt;". Andere
    /// mailclients snappen deze conditionele-comment-truc niet en tonen toch al de fallback.
    ///
    /// BELANGRIJK: dit moet toegepast worden op elk HTML-fragment AFZONDERLIJK, vóór het
    /// samenvoegen van bv. templatetekst + handtekening — anders zou het "haal de inhoud tussen
    /// &lt;body&gt;/&lt;/body&gt; op"-onderdeel de tekst die vóór een later toegevoegd, volledig
    /// Word-document (met eigen &lt;html&gt;/&lt;body&gt;) staat, weggooien.
    /// </summary>
    public static class WordHtmlSanitizer
    {
        private static readonly Regex WordBodyRegex = new(
            @"<body[^>]*>([\s\S]*)</body>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VmlMsoConditionalBlockRegex = new(
            @"<!--\[if[^\]]*\b(?:vml|mso)\b[^\]]*\]>[\s\S]*?<!\[endif\]-->",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DownlevelRevealedMarkerRegex = new(
            @"<!\[if[^\]]*\]>|<!\[endif\]>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex OfficeNamespaceTagRegex = new(
            @"</?(?:v|o|w|m):\w+(?:\s[^>]*)?/?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Clean(string? html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            var bodyMatch = WordBodyRegex.Match(html);
            var content = bodyMatch.Success ? bodyMatch.Groups[1].Value : html;

            content = VmlMsoConditionalBlockRegex.Replace(content, string.Empty);
            content = DownlevelRevealedMarkerRegex.Replace(content, string.Empty);
            content = OfficeNamespaceTagRegex.Replace(content, string.Empty);

            return content;
        }
    }
}
