using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Infrastructure.Services;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Services;

/// <summary>
/// Tests voor BuildAssetKey — verifieert dat de juiste strategie en sleutel
/// worden gegenereerd per propertytype. De sleutel bepaalt of een nieuw MarketAsset
/// wordt aangemaakt of een bestaand wordt hergebruikt.
/// </summary>
public class AssetKeyTests
{
    private static NormalizedPropertyDto Dto(
        PropertyType type = PropertyType.House,
        string? street = "Kerkstraat",
        string? houseNr = "10",
        string? postalCode = "8730",
        string? externalId = "EXT123",
        bool isProjectListing = false,
        bool isProjectUnit = false,
        string? projectExternalId = null,
        string? unitExternalId = null,
        int? floor = null,
        string? unitNumber = null)
        => new()
        {
            PropertyType       = type,
            TransactionType    = TransactionType.ForSale,
            Country            = "BE",
            PostalCode         = postalCode,
            Street             = street,
            HouseNumber        = houseNr,
            ExternalId         = externalId!,
            IsProjectListing   = isProjectListing,
            IsProjectUnit      = isProjectUnit,
            ProjectExternalId  = projectExternalId,
            UnitExternalId     = unitExternalId,
            Floor              = floor,
            UnitNumber         = unitNumber,
        };

    // ══════════════════════════════════════════════════════════════════════════
    // ProjectGroup
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildAssetKey_ProjectGroup_UsesProjectGroupStrategy()
    {
        var dto = Dto(type: PropertyType.ProjectGroup, isProjectListing: true, externalId: "PG001");
        var (key, strategy) = MarketListingService.BuildAssetKey(dto);

        Assert.Equal("ProjectGroup", strategy);
        Assert.Contains("PG001", key);
        Assert.Contains("BE", key);
        Assert.Contains("8730", key);
    }

    [Fact]
    public void BuildAssetKey_TwoProjectGroupsSameExternalId_SameKey()
    {
        var dto1 = Dto(type: PropertyType.ProjectGroup, isProjectListing: true, externalId: "PG001");
        var dto2 = Dto(type: PropertyType.ProjectGroup, isProjectListing: true, externalId: "PG001");
        var (key1, _) = MarketListingService.BuildAssetKey(dto1);
        var (key2, _) = MarketListingService.BuildAssetKey(dto2);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void BuildAssetKey_TwoProjectGroupsDifferentExternalId_DifferentKey()
    {
        var dto1 = Dto(type: PropertyType.ProjectGroup, isProjectListing: true, externalId: "PG001");
        var dto2 = Dto(type: PropertyType.ProjectGroup, isProjectListing: true, externalId: "PG002");
        var (key1, _) = MarketListingService.BuildAssetKey(dto1);
        var (key2, _) = MarketListingService.BuildAssetKey(dto2);

        Assert.NotEqual(key1, key2);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // House (adres-gebaseerde key, geen ExternalId)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildAssetKey_House_UsesHouseStrategy()
    {
        var dto = Dto(type: PropertyType.House, street: "Kerkstraat", houseNr: "10");
        var (_, strategy) = MarketListingService.BuildAssetKey(dto);

        Assert.Equal("House", strategy);
    }

    [Fact]
    public void BuildAssetKey_House_SameAddress_SameKey()
    {
        var dto1 = Dto(type: PropertyType.House, street: "Kerkstraat", houseNr: "10", externalId: "EXT001");
        var dto2 = Dto(type: PropertyType.House, street: "Kerkstraat", houseNr: "10", externalId: "EXT002");
        var (key1, _) = MarketListingService.BuildAssetKey(dto1);
        var (key2, _) = MarketListingService.BuildAssetKey(dto2);

        // Zelfde adres → zelfde key (ExternalId speelt geen rol bij House)
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void BuildAssetKey_House_DifferentHouseNumber_DifferentKey()
    {
        var dto1 = Dto(type: PropertyType.House, street: "Kerkstraat", houseNr: "10");
        var dto2 = Dto(type: PropertyType.House, street: "Kerkstraat", houseNr: "11");
        var (key1, _) = MarketListingService.BuildAssetKey(dto1);
        var (key2, _) = MarketListingService.BuildAssetKey(dto2);

        Assert.NotEqual(key1, key2);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Apartment met floor/unit — herkent specifieke unit
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildAssetKey_ApartmentWithFloor_UsesFloorStrategy()
    {
        var dto = Dto(type: PropertyType.Apartment, floor: 3, unitNumber: "B");
        var (_, strategy) = MarketListingService.BuildAssetKey(dto);

        Assert.Equal("Apartment+Floor", strategy);
    }

    [Fact]
    public void BuildAssetKey_ApartmentWithFloor_SameUnit_SameKey()
    {
        var dto1 = Dto(type: PropertyType.Apartment, floor: 3, unitNumber: "B", externalId: "APT001");
        var dto2 = Dto(type: PropertyType.Apartment, floor: 3, unitNumber: "B", externalId: "APT002");
        var (key1, _) = MarketListingService.BuildAssetKey(dto1);
        var (key2, _) = MarketListingService.BuildAssetKey(dto2);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void BuildAssetKey_ApartmentWithFloor_DifferentFloor_DifferentKey()
    {
        var dto1 = Dto(type: PropertyType.Apartment, floor: 3, unitNumber: "B");
        var dto2 = Dto(type: PropertyType.Apartment, floor: 4, unitNumber: "B");
        var (key1, _) = MarketListingService.BuildAssetKey(dto1);
        var (key2, _) = MarketListingService.BuildAssetKey(dto2);

        Assert.NotEqual(key1, key2);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Pipe-characters in velden worden gescaped (voorkomt key-poisoning)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildAssetKey_PipeInStreet_IsEscaped()
    {
        var dto = Dto(type: PropertyType.House, street: "Kerk|straat", houseNr: "10");
        var (key, _) = MarketListingService.BuildAssetKey(dto);

        // Pipe in straatnaam moet gescaped zijn als underscore
        Assert.DoesNotContain("Kerk|straat", key);
        Assert.Contains("KERK_STRAAT", key);
    }
}
