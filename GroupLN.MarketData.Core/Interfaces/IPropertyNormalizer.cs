using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IPropertyNormalizer
{
    NormalizedPropertyDto Normalize(ListingDto listing, int sourceId);
}
