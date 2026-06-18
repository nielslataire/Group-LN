namespace GroupLN.MarketData.Core.DTOs;

public sealed record GeocodingResult(
    decimal Latitude,
    decimal Longitude,
    int Confidence,
    string Provider,
    string? RawResponseJson = null);
