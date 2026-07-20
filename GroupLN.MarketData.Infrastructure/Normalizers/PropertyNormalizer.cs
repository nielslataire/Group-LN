using System.Security.Cryptography;
using System.Text;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Normalizers;

public class PropertyNormalizer : IPropertyNormalizer
{
    private readonly ILogger<PropertyNormalizer> _logger;

    public PropertyNormalizer(ILogger<PropertyNormalizer> logger)
    {
        _logger = logger;
    }

    public NormalizedPropertyDto Normalize(ListingDto listing, int sourceId)
    {
        var mappedType = ParsePropertyType(listing.PropertyTypeRaw, listing.PropertySubTypeRaw);
        var mappedSubType = ParsePropertySubType(listing.PropertySubTypeRaw, listing.PropertyTypeRaw);
        var mappedTx = ParseTransactionType(listing.TransactionTypeRaw);
        var isProject = mappedType == PropertyType.ProjectGroup;

        _logger.LogInformation(
            "[Normalizer] ID={ExternalId} | RawType={RawType} | RawSubType={RawSubType} | " +
            "RawTx={RawTx} | MappedType={Type} | MappedSubType={SubType} | MappedTx={Tx}",
            listing.ExternalId,
            listing.PropertyTypeRaw ?? "(null)",
            listing.PropertySubTypeRaw ?? "(null)",
            listing.TransactionTypeRaw ?? "(null)",
            mappedType,
            mappedSubType,
            mappedTx);

        if (mappedType == PropertyType.Unknown)
            _logger.LogWarning(
                "[Normalizer] Onbekend PropertyType voor raw='{Raw}'. ExternalId={Id}",
                listing.PropertyTypeRaw, listing.ExternalId);

        return new NormalizedPropertyDto
        {
            SourceId = sourceId,
            ExternalId = listing.ExternalId,
            Url = listing.Url,
            Title = listing.Title?.Trim(),

            PropertyType = mappedType,
            PropertySubType = mappedSubType,
            TransactionType = mappedTx,
            IsProjectListing = isProject,

            Country = "BE",
            PostalCode = listing.PostalCode?.Trim(),
            City = NormalizeCity(listing.City),
            Street = listing.Street?.Trim(),
            HouseNumber = listing.HouseNumber?.Trim(),
            Floor = listing.Floor,
            UnitNumber = listing.UnitNumber?.Trim(),

            Latitude = listing.Latitude,
            Longitude = listing.Longitude,

            AskingPrice = listing.AskingPrice,
            MaxPrice = listing.MaxPrice,
            LivingArea = listing.LivingArea,
            LandArea = listing.LandArea,
            TerraceArea = listing.TerraceArea,
            GardenArea = listing.GardenArea,

            Bedrooms = listing.Bedrooms,
            Bathrooms = listing.Bathrooms,
            ShowerCount = listing.ShowerCount,
            ToiletCount = listing.ToiletCount,
            GarageCount = listing.GarageCount,

            ConstructionYear = listing.ConstructionYear,

            EPCScore = listing.EPCScore,
            EPCLabel = ParseEPCLabel(listing.EPCLabelRaw),

            IsNewBuild = listing.IsNewBuild ?? false,

            DeveloperName = listing.DeveloperName?.Trim(),
            DeveloperWebsite = listing.DeveloperWebsite?.Trim(),
            DeveloperPhone = listing.DeveloperPhone?.Trim(),

            EnergyFeatures  = listing.EnergyFeatures,
            DescriptionHash = HashDescription(listing.Description),
            ListingTitle    = string.IsNullOrWhiteSpace(listing.Title) ? null : listing.Title.Trim(),
            Description     = string.IsNullOrWhiteSpace(listing.Description) ? null : listing.Description.Trim(),
            RawJson = listing.RawJson,

            MetaTitle       = string.IsNullOrWhiteSpace(listing.MetaTitle)       ? null : listing.MetaTitle.Trim(),
            OgTitle         = string.IsNullOrWhiteSpace(listing.OgTitle)         ? null : listing.OgTitle.Trim(),
            MetaDescription = string.IsNullOrWhiteSpace(listing.MetaDescription) ? null : listing.MetaDescription.Trim(),
            H1              = string.IsNullOrWhiteSpace(listing.H1)              ? null : listing.H1.Trim(),
            H2              = string.IsNullOrWhiteSpace(listing.H2)              ? null : listing.H2.Trim(),
            H3              = string.IsNullOrWhiteSpace(listing.H3)              ? null : listing.H3.Trim(),
            StructuredData  = string.IsNullOrWhiteSpace(listing.StructuredData)  ? null : listing.StructuredData.Trim(),

            PhotoUrls = listing.PhotoUrls.Count > 0 ? new List<string>(listing.PhotoUrls) : new List<string>()
        };
    }

    // ── PropertyType-mapping ───────────────────────────────────────────────────
    // Immoweb gebruikt soms subtype-waarden ook als type (VILLA, DUPLEX, enz.)

    private static PropertyType ParsePropertyType(string? rawType, string? rawSubType)
    {
        var upper = (rawType ?? "").Trim().ToUpperInvariant();

        return upper switch
        {
            // ── Woningen ──────────────────────────────────────────────────────
            "HOUSE" or "WONING" or "HUIS" or "MAISON" => PropertyType.House,
            "VILLA" or "BUNGALOW" or "MANSION" or "HERENHUIS" or
            "TOWN_HOUSE" or "FARMHOUSE" or "HOEVE" or "FERME" or
            "COUNTRY_HOUSE" or "EXCEPTIONAL_PROPERTY" or
            "DETACHED_HOUSE" or "SEMI_DETACHED_HOUSE" or "TERRACED_HOUSE" or
            "CHALET" => PropertyType.House,

            // ── Appartementen ─────────────────────────────────────────────────
            "APARTMENT" or "APPARTEMENT" or "FLAT" or
            "PENTHOUSE" or "DUPLEX" or "TRIPLEX" or
            "GROUND_FLOOR" or "STUDIO" or "SERVICE_FLAT" => PropertyType.Apartment,

            // ── Projectgroepen (nieuwbouw) ─────────────────────────────────────
            "APARTMENT_GROUP" or "HOUSE_GROUP" or
            "NEW_DEVELOPMENT" or "NEWDEVELOPMENT" or
            "DEVELOPMENT" or "PROJECT" or "PROJECTBUILDING" => PropertyType.ProjectGroup,

            // ── Grond ─────────────────────────────────────────────────────────
            "LAND" or "GROND" or "TERRAIN" or
            "BUILDING_LAND" or "BOUWGROND" or "TERRAIN_A_BATIR" or
            "LAND_WITH_BUILDING" => PropertyType.Land,

            // ── Commercieel ───────────────────────────────────────────────────
            "COMMERCIAL" or "COMMERCIEEL" or
            "OFFICE" or "KANTOOR" or "BUREAU" or
            "INDUSTRY" or "INDUSTRIE" or
            "BUILDING" or "GEBOUW" or "IMMEUBLE" => PropertyType.CommercialProperty,

            // ── Garage / Parking ──────────────────────────────────────────────
            "GARAGE" or "PARKING" => PropertyType.Garage,

            "" => PropertyType.Unknown,

            _ => PropertyType.Unknown
        };
    }

    // ── PropertySubType-mapping ────────────────────────────────────────────────

    private static PropertySubType ParsePropertySubType(string? rawSubType, string? rawType)
    {
        // Probeer eerst het subtype; val terug op het type voor project-groepen
        var upperSub = (rawSubType ?? "").Trim().ToUpperInvariant();
        var upperType = (rawType ?? "").Trim().ToUpperInvariant();

        // Project-groepen via type-veld
        if (upperType == "APARTMENT_GROUP") return PropertySubType.ApartmentGroup;
        if (upperType == "HOUSE_GROUP") return PropertySubType.HouseGroup;

        return upperSub switch
        {
            // ── Woningsubtypes ────────────────────────────────────────────────
            "DETACHED_HOUSE" or "OPEN_BEBOUWING" or "VRIJSTAANDE_WONING" or
            "DETACHED" or "OPEN" => PropertySubType.DetachedHouse,

            "SEMI_DETACHED_HOUSE" or "HALFOPEN_BEBOUWING" or "HALF_OPEN" or
            "SEMI_DETACHED" => PropertySubType.SemiDetachedHouse,

            "TERRACED_HOUSE" or "GESLOTEN_BEBOUWING" or "RIJWONING" or
            "TOWN_HOUSE" or "CLOSED" or "TERRACED" => PropertySubType.TerracedHouse,

            "VILLA" => PropertySubType.Villa,
            "MANSION" or "HERENHUIS" => PropertySubType.Mansion,
            "BUNGALOW" => PropertySubType.Bungalow,
            "FARMHOUSE" or "HOEVE" or "FERME" => PropertySubType.Farmhouse,
            "COUNTRY_HOUSE" or "LANDELIJKE_WONING" => PropertySubType.CountryHouse,
            "CHALET" => PropertySubType.CountryHouse,
            "EXCEPTIONAL_PROPERTY" => PropertySubType.ExceptionalProperty,

            // ── Appartementsubtypes ───────────────────────────────────────────
            "STUDIO" => PropertySubType.Studio,
            "APARTMENT" or "APPARTEMENT" or "FLAT" => PropertySubType.Apartment,
            "PENTHOUSE" => PropertySubType.Penthouse,
            "DUPLEX" => PropertySubType.Duplex,
            "TRIPLEX" => PropertySubType.Triplex,
            "SERVICE_FLAT" or "SERVICEFLAT" => PropertySubType.ServiceFlat,
            "GROUND_FLOOR" or "GELIJKVLOERS" => PropertySubType.GroundFloorFlat,

            // ── Project-subtypes ──────────────────────────────────────────────
            "APARTMENT_GROUP" => PropertySubType.ApartmentGroup,
            "HOUSE_GROUP" => PropertySubType.HouseGroup,

            // ── Grond ─────────────────────────────────────────────────────────
            "BUILDING_LAND" or "BOUWGROND" or "TERRAIN_A_BATIR" => PropertySubType.BuildingLand,
            "LAND_WITH_BUILDING" => PropertySubType.LandWithBuilding,

            // ── Commercieel ───────────────────────────────────────────────────
            "INDUSTRY" or "INDUSTRIE" or "INDUSTRIAL_BUILDING" => PropertySubType.IndustrialBuilding,
            "OFFICE" or "KANTOOR" or "BUREAU" => PropertySubType.Office,
            "COMMERCIAL" or "COMMERCIEEL" or "COMMERCIAL_GROUND" => PropertySubType.CommercialGround,
            "PARKING" => PropertySubType.Parking,
            "GARAGE" => PropertySubType.Unknown,

            "" => PropertySubType.Unknown,
            _ => PropertySubType.Unknown
        };
    }

    // ── TransactionType-mapping ───────────────────────────────────────────────

    private static TransactionType ParseTransactionType(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return TransactionType.Unknown;

        return raw.Trim().ToUpperInvariant() switch
        {
            "FORSALE" or "FOR_SALE" or "TE_KOOP" or "SALE" or "VENTE" => TransactionType.ForSale,
            "FORRENT" or "FOR_RENT" or "TE_HUUR" or "RENT" or "LOCATION" => TransactionType.ForRent,
            "NEWDEVELOPMENT" or "NEW_DEVELOPMENT" or "NIEUWBOUW" => TransactionType.NewDevelopment,
            "PUBLICSALE" or "PUBLIC_SALE" or "OPENBARE_VERKOOP" or "VENTE_PUBLIQUE" => TransactionType.PublicSale,
            _ => TransactionType.Unknown
        };
    }

    // ── EPC ───────────────────────────────────────────────────────────────────

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
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(city.Trim().ToLower());
    }

    private static string? HashDescription(string? description)
    {
        if (string.IsNullOrEmpty(description)) return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(description));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
