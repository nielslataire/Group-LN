namespace GroupLN.MarketData.Core.DTOs;

public record ProjectGroupSaveResult(
    int UnitsFound,
    int UnitsCreated,
    int UnitsUpdated,
    int HouseUnits,
    int ApartmentUnits,
    int CommercialUnits,
    int SoldUnits,
    int AvailableUnits,
    int ReservedUnits,
    int OptionUnits,
    int UnknownUnits);
