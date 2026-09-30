namespace CPMCore.Services;

/// <summary>
/// Of de Google-login (tweede OIDC-schema, rechtstreeks naar Google) actief is. Wordt in Program.cs
/// afgeleid uit Google:ClientId + Google:ClientSecret: ontbreekt één van beide, dan wordt het schema
/// niet geregistreerd (een OIDC-handler zonder ClientId laat de hele authenticatie-pipeline falen)
/// en verbergt de loginpagina de Google-knop.
/// </summary>
public sealed class GoogleLoginSettings
{
    public bool Enabled { get; init; }
}
