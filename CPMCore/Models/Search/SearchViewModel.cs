using System;
using System.Collections.Generic;

namespace CPMCore.Models.Search;

public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;

    public IReadOnlyList<SearchResultSection> Sections { get; set; } = Array.Empty<SearchResultSection>();
}

public class SearchResultSection
{
    public string Title { get; set; } = string.Empty;

    public IReadOnlyList<SearchResultItem> Items { get; set; } = Array.Empty<SearchResultItem>();
}

public class SearchResultItem
{
    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string Url { get; set; } = string.Empty;
}