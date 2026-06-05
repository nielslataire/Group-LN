using System.Security.Cryptography;
using System.Text;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Interfaces;

namespace GroupLN.MarketData.Infrastructure.Normalizers;

public class PropertyNormalizer : IPropertyNormalizer
{
    public NormalizedPropertyDto Normalize(ListingDto listing, int sourceId)
    {
        return new NormalizedPropertyDto
        {
            SourceId = sourceId,
            ExternalId = listing.ExternalId,
            Url = listing.Url,
            Title = listing.Title,

            PropertyType = ParsePropertyType(listing.PropertyTypeRaw),
            PropertySubType = ParsePropertySubType(listing.PropertySubTypeRaw),
            TransactionType = ParseTransactionType(listing.TransactionTypeRaw),

            Country = "BE",
            PostalCode = listing.PostalCode?.Trim(),
            City = NormalizeCity(listing.City),
            Street = listing.Street?.Trim(),
            HouseNumber = listing.HouseNumber?.Trim(),

            Latitude = listing.Latitude,
            Longitude = listing.Longitude,

            AskingPrice = listing.AskingPrice,
            LivingArea = listing.LivingArea,
            LandArea = listing.LandArea,

            Bedrooms = listing.Bedrooms,
            Bathrooms = listing.Bathrooms,
            GarageCount = listing.GarageCount,

            ConstructionYear = listing.ConstructionYear,

            EPCScore = listing.EPCScore,
            EPCLabel = ParseEPCLabel(listing.EPCLabelRaw),

            IsNewBuild = listing.IsNewBuild ?? false,

            DescriptionHash = HashDescription(listing.Description),
            RawJson = listing.RawJson
        };
    }

    private static PropertyType ParsePropertyType(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return PropertyType.Unknown;

        return raw.ToUpperInvariant() switch
        {
            "HOUSE" or "WONING" or "HUIS" => PropertyType.House,
            "APARTMENT" or "APPARTEMENT" or "FLAT" => PropertyType.Apartment,
            "LAND" or "GROND" or "TERRAIN" => PropertyType.Land,
            "COMMERCIAL" or "COMMERCIEEL" or "OFFICE" or "KANTOOR" => PropertyType.CommercialProperty,
            "GARAGE" or "PARKING" => PropertyType.Garage,
            _ => PropertyType.Unknown
        };
    }

    private static PropertySubType ParsePropertySubType(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return PropertySubType.Unknown;

        return raw.ToUpperInvariant() switch
        {
            "DETACHED_HOUSE" or "OPEN_BEBOUWING" or "VRIJSTAANDE_WONING" or "DETACHED" => PropertySubType.DetachedHouse,
            "SEMI_DETACHED_HOUSE" or "HALFOPEN_BEBOUWING" or "HALF_OPEN" => PropertySubType.SemiDetachedHouse,
            "TERRACED_HOUSE" or "GESLOTEN_BEBOUWING" or "RIJWONING" or "TOWN_HOUSE" or "CLOSED" => PropertySubType.TerracedHouse,
            "VILLA" => PropertySubType.Villa,
            "MANSION" or "HERENHUIS" => PropertySubType.Mansion,
            "BUNGALOW" => PropertySubType.Bungalow,
            "FARMHOUSE" or "HOEVE" or "FERME" => PropertySubType.Farmhouse,
            "COUNTRY_HOUSE" or "LANDELIJKE_WONING" => PropertySubType.CountryHouse,
            "STUDIO" => PropertySubType.Studio,
            "APARTMENT" or "APPARTEMENT" => PropertySubType.Apartment,
            "PENTHOUSE" => PropertySubType.Penthouse,
            "DUPLEX" => PropertySubType.Duplex,
            "TRIPLEX" => PropertySubType.Triplex,
            "SERVICE_FLAT" or "SERVICEFLAT" => PropertySubType.ServiceFlat,
            "GROUND_FLOOR" or "GELIJKVLOERS" => PropertySubType.GroundFloorFlat,
            "BUILDING_LAND" or "BOUWGROND" => PropertySubType.BuildingLand,
            _ => PropertySubType.Unknown
        };
    }

    private static TransactionType ParseTransactionType(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return TransactionType.Unknown;

        return raw.ToUpperInvariant() switch
        {
            "FORSALE" or "FOR_SALE" or "TE_KOOP" or "SALE" => TransactionType.ForSale,
            "FORRENT" or "FOR_RENT" or "TE_HUUR" or "RENT" => TransactionType.ForRent,
            "NEWDEVELOPMENT" or "NEW_DEVELOPMENT" or "NIEUWBOUW" => TransactionType.NewDevelopment,
            "PUBLICSALE" or "PUBLIC_SALE" or "OPENBARE_VERKOOP" => TransactionType.PublicSale,
            _ => TransactionType.Unknown
        };
    }

    private static EPCLabel? ParseEPCLabel(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        return raw.Trim().ToUpperInvariant() switch
        {
            "A++" or "A_PLUS_PLUS" => EPCLabel.APlusPlus,
            "A+" or "A_PLUS" => EPCLabel.APlus,
            "A" => EPCLabel.A,
            "B" => EPCLabel.B,
            "C" => EPCLabel.C,
            "D" => EPCLabel.D,
            "E" => EPCLabel.E,
            "F" => EPCLabel.F,
            "G" => EPCLabel.G,
            _ => null
        };
    }

    private static string? NormalizeCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        // Eerste letter van elk woord hoofdletter
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(city.Trim().ToLower());
    }

    private static string? HashDescription(string? description)
    {
        if (string.IsNullOrEmpty(description)) return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(description));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
