using System.Net;
using System.Text.RegularExpressions;

namespace ServiceCore.Helpers
{
    /// <summary>
    /// Eenvoudige allow-list-opschoning voor opgemaakte tekst die door beheerders wordt ingegeven en op de publieke website wordt
    /// getoond (bv. antwoorden van de FAQ). Toegelaten: p, br, strong/b, em/i, u, ul, ol, li en a (enkel href naar http, https, mailto,
    /// tel of een relatief pad; links openen in een nieuw tabblad). Alle andere tags, alle attributen en scripts/stijlen/iframes
    /// (met inhoud) worden verwijderd.
    /// </summary>
    public static class HtmlSanitizer
    {
        private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
            { "p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a" };

        private static readonly Regex Blocks = new(@"<(script|style|iframe|object|embed)\b[^>]*>.*?</\1\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Comments = new(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex Tags = new(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)\b([^>]*)>", RegexOptions.Compiled);
        private static readonly Regex Href = new(@"href\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Clean(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            html = Blocks.Replace(html, string.Empty);
            html = Comments.Replace(html, string.Empty);
            var cleaned = Tags.Replace(html, m =>
            {
                var closing = m.Groups[1].Value == "/";
                var tag = m.Groups[2].Value.ToLowerInvariant();
                if (!Allowed.Contains(tag)) return string.Empty;
                if (closing) return $"</{tag}>";
                if (tag == "a")
                {
                    var h = Href.Match(m.Groups[3].Value);
                    var url = h.Success ? (h.Groups[1].Success ? h.Groups[1].Value : h.Groups[2].Value) : string.Empty;
                    url = WebUtility.HtmlDecode(url).Trim();
                    if (!IsSafeUrl(url)) return "<a>";
                    return $"<a href=\"{WebUtility.HtmlEncode(url)}\" target=\"_blank\" rel=\"noopener noreferrer\">";
                }
                return $"<{tag}>";
            });
            // lege Quill-inhoud ("<p><br></p>") telt niet als tekst
            return Regex.Replace(cleaned, @"<[^>]+>|\s|&nbsp;", string.Empty).Length == 0 ? string.Empty : cleaned.Trim();
        }

        private static bool IsSafeUrl(string url) =>
            url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            (url.StartsWith("/") && !url.StartsWith("//"));
    }
}
