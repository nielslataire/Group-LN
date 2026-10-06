using Microsoft.Net.Http.Headers;

namespace CPMCore.Services.Signing;

/// <summary>Content-Disposition met een bestandsnaam die ook niet-ASCII tekens (bv. een gedachtestreep uit een
/// projectnaam) overleeft: Kestrel weigert die rechtstreeks in een header (InvalidOperationException 0x2014).
/// <c>SetHttpFileName</c> zet de naam om naar <c>filename*=UTF-8''…</c> wanneer dat nodig is.</summary>
public static class ContentDispositionHelper
{
    public static string Inline(string fileName)
    {
        var header = new ContentDispositionHeaderValue("inline");
        header.SetHttpFileName(fileName);
        return header.ToString();
    }
}
