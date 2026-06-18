namespace GroupLN.MarketData.Core.Entities;

public class GeocodingCache
{
    public long Id { get; set; }

    // SHA-256 hash van genormaliseerd adres — unieke index, voorkomt dubbele lookups
    public string AddressKey { get; set; } = "";

    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "BE";

    // null = geocoding mislukt of geen resultaat (wordt ook gecached om herhaalde lookups te voorkomen)
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? Confidence { get; set; }
    public string? Provider { get; set; }
    public string? RawResponseJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
