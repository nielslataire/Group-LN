namespace CPMCore.Middleware;

/// <summary>
/// Security headers voor de publieke signing-pagina's (ONDERTEKENEN_VOORSTEL.md §6.5): alles onder
/// <c>/ondertekenen</c> en <c>/verifieer</c>. Geen caching (de pagina's tonen persoonsgegevens en
/// een sessiegebonden document), geen Referer (de eerste URL bevat het token), geen inbedding door
/// derden, en een CSP zonder externe bronnen — dus géén CDN-fonts of -iconen op deze pagina's;
/// <c>_LayoutPublic.cshtml</c> laadt alles lokaal. Headers worden via <c>OnStarting</c> gezet en
/// enkel wanneer een actie ze niet zelf al zette (de PDF-actie kiest bv. SAMEORIGIN voor zijn iframe).
/// Registratie in Program.cs vóór routing; niets hiervan raakt de rest van de applicatie.
/// </summary>
public sealed class SigningSecurityHeadersMiddleware
{
    // MERGE 28/09/2026: "/ondertekenen" is nog altijd de route van SigningController (de
    // link-per-e-mail-flow van de andere pc, ONDERTEKENEN_VOORTGANG.md "Samenloop") en blijft erbuiten
    // tot de cutover. Fase 2 van de signingmodule (OndertekenenController) gebruikt "/tekenen".
    private static readonly string[] Prefixes = { "/verifieer", "/tekenen" };

    private const string Csp =
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "font-src 'self'; " +
        "script-src 'self'; " +
        "frame-src 'self'; " +
        "object-src 'self'; " +
        "connect-src 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'self'";

    private readonly RequestDelegate _next;

    public SigningSecurityHeadersMiddleware(RequestDelegate next) { _next = next; }

    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var applies = Prefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        if (!applies) return _next(context);

        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            SetIfMissing(h, "Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
            SetIfMissing(h, "Pragma", "no-cache");
            SetIfMissing(h, "Expires", "0");
            SetIfMissing(h, "Referrer-Policy", "no-referrer");
            SetIfMissing(h, "X-Content-Type-Options", "nosniff");
            SetIfMissing(h, "X-Frame-Options", "DENY");
            SetIfMissing(h, "Content-Security-Policy", Csp);
            SetIfMissing(h, "Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            return Task.CompletedTask;
        });
        return _next(context);
    }

    private static void SetIfMissing(IHeaderDictionary headers, string name, string value)
    {
        if (!headers.ContainsKey(name)) headers[name] = value;
    }
}
