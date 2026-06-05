namespace GroupLN.MarketData.Core.Enums;

public enum PropertySubType
{
    Unknown = 0,

    // Woningen
    DetachedHouse = 1,      // Open bebouwing
    SemiDetachedHouse = 2,  // Halfopen bebouwing
    TerracedHouse = 3,      // Gesloten bebouwing / Rijwoning
    Villa = 4,
    Mansion = 5,
    Bungalow = 6,
    Farmhouse = 7,
    CountryHouse = 8,

    // Appartementen
    Studio = 20,
    Apartment = 21,
    Penthouse = 22,
    Duplex = 23,
    Triplex = 24,
    ServiceFlat = 25,
    GroundFloorFlat = 26,

    // Projectgroepen
    ApartmentGroup = 27,   // APARTMENT_GROUP — nieuwbouwproject appartementen
    HouseGroup = 28,       // HOUSE_GROUP — nieuwbouwproject woningen

    // Grond & commercieel
    BuildingLand = 30,
    LandWithBuilding = 31,
    IndustrialBuilding = 32,
    Office = 33,
    CommercialGround = 34,
    Parking = 35,
    ExceptionalProperty = 36
}
