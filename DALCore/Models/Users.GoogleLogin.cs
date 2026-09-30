#nullable disable

namespace DALCore.Models;

// Migratie 060_GoogleLogin.sql. Gasten (aannemers, klanten) kunnen naast Microsoft Entra ook met een
// Google-account inloggen; de app praat daarvoor rechtstreeks met Google via OpenID Connect.
public partial class Users
{
    /// <summary>Stabiele Google "sub"-claim. Null zolang de gebruiker nooit met Google inlogde.
    /// Eerste Google-login koppelt op het uitgenodigde e-mailadres en vult dit; daarna wordt enkel
    /// nog hierop gematcht (zelfde patroon als <see cref="EntraObjectId"/> voor Entra).</summary>
    public string GoogleSubjectId { get; set; }
}
