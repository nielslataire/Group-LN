namespace CPMCore.Models.Account;

public class MijnHandtekeningVM
{
    public string? SignatureHtml { get; set; }

    /// <summary>"Visual" (laatst bewerkt via Quill) of "Html" (laatst bewerkt/geplakt als ruwe HTML-bron).</summary>
    public string Format { get; set; } = "Visual";
}
