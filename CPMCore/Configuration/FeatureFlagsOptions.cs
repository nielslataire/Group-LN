namespace CPMCore.Configuration;

public class FeatureFlagsOptions
{
    public bool EnableQRCode { get; set; } = false;
    public bool EnableContractorPortal { get; set; } = false;

    /// <summary>
    /// Elektronisch ondertekenen (ONDERTEKENEN_VOORSTEL.md). Uit = de module is aanwezig maar
    /// onbereikbaar: geen knoppen, geen publieke route, geen achtergrondjob — zodat ze "donker"
    /// uitgerold en op de testomgeving apart aangezet kan worden. Aan = Signing:* wordt bij het
    /// opstarten gevalideerd (HMAC-sleutel, publieke URL) en de app weigert te starten zonder.
    /// </summary>
    public bool EnableSigning { get; set; } = false;
}