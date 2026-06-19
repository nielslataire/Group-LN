namespace GroupLN.MarketData.Core.Entities;

public class ProjectPhotoHash
{
    public long Id { get; set; }

    public long MarketAssetId { get; set; }
    public string SourceName { get; set; } = "";
    public string ProjectExternalId { get; set; } = "";

    public string ImageUrl { get; set; } = "";
    public string NormalizedImageUrl { get; set; } = "";
    public int ImageOrder { get; set; }

    public string HashAlgorithm { get; set; } = "SHA256+dHash1";
    public string ContentHash { get; set; } = "";
    public long? PerceptualHash { get; set; }
    public string? PerceptualHashVersion { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }

    public DateTime DownloadedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public MarketAsset? MarketAsset { get; set; }
}
