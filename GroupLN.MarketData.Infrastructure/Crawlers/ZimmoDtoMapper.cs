using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

/// <summary>
/// Mapt Zimmo-specifieke ruwe veldwaarden naar ListingDto.
/// Alle string-constanten zijn zoals Zimmo ze stuurt in __NEXT_DATA__.
/// </summary>
public static class ZimmoDtoMapper
{
    /// <summary>
    /// Zet Zimmo category/type string om naar PropertyTypeRaw zoals de PropertyNormalizer verwacht.
    /// </summary>
    public static string? MapPropertyType(string? zimmoType) => zimmoType?.ToUpperInvariant() switch
    {
        "HOUSE"     => "HOUSE",
        "APARTMENT" => "APARTMENT",
        "LAND"      => "LAND",
        "GARAGE"    => "GARAGE",
        "COMMERCIAL"=> "COMMERCIAL",
        "OFFICE"    => "OFFICE",
        "PROJECT"   => "PROJECT",
        null        => null,
        var raw     => raw  // doorsturen, normalizer beslist
    };

    /// <summary>
    /// Zet Zimmo status/transactionType om naar TransactionTypeRaw.
    /// </summary>
    public static string? MapTransactionType(string? zimmoStatus) => zimmoStatus?.ToUpperInvariant() switch
    {
        "FOR_SALE"  => "FOR_SALE",
        "TAKE_OVER" => "TAKE_OVER",
        "TO_RENT"   => "TO_RENT",
        "FOR_RENT"  => "TO_RENT",
        null        => null,
        var raw     => raw
    };

    /// <summary>
    /// Bouwt een ListingDto op vanuit velden die al geëxtraheerd zijn.
    /// Alle velden zijn optioneel — null-waarden worden doorgegeven aan de normalizer.
    /// </summary>
    public static ListingDto Build(
        string externalId,
        string url,
        string? title,
        string? zimmoPropertyType,
        string? zimmoPropertySubType,
        string? zimmoTransactionType,
        string? postalCode,
        string? city,
        string? street,
        string? houseNumber,
        decimal? latitude,
        decimal? longitude,
        decimal? askingPrice,
        decimal? maxPrice,
        decimal? livingArea,
        decimal? landArea,
        int? bedrooms,
        int? bathrooms,
        string? epcLabel,
        decimal? epcScore,
        bool? isNewConstruction,
        string? developerName,
        string? developerPhone,
        string? developerWebsite,
        string? projectName,
        string? description,
        string? rawJson)
    {
        return new ListingDto
        {
            ExternalId         = externalId,
            Url                = url,
            CanonicalUrl       = url,
            Title              = title,
            PropertyTypeRaw    = MapPropertyType(zimmoPropertyType),
            PropertySubTypeRaw = zimmoPropertySubType,
            TransactionTypeRaw = MapTransactionType(zimmoTransactionType) ?? "FOR_SALE",
            PostalCode         = postalCode,
            City               = city,
            Street             = street,
            HouseNumber        = houseNumber,
            Latitude           = latitude,
            Longitude          = longitude,
            AskingPrice        = askingPrice,
            MaxPrice           = maxPrice,
            LivingArea         = livingArea,
            LandArea           = landArea,
            Bedrooms           = bedrooms,
            Bathrooms          = bathrooms,
            EPCLabelRaw        = epcLabel,
            EPCScore           = epcScore,
            IsNewBuild         = isNewConstruction,
            IsNewBuildSource   = isNewConstruction == true ? "Zimmo/__NEXT_DATA__" : null,
            DeveloperName      = developerName,
            DeveloperPhone     = developerPhone,
            DeveloperWebsite   = developerWebsite,
            ProjectName        = projectName,
            Description        = description,
            RawJson            = rawJson?.Length > 50_000 ? rawJson[..50_000] : rawJson
        };
    }
}
