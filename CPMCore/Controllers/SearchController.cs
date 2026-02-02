using System;
using System.Collections.Generic;
using DALCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

[Authorize]
public class SearchController : BaseController
{
    private readonly cpmRunningContext _db;

    public SearchController(cpmRunningContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(string? term, CancellationToken ct)
    {
        term = term?.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            return Json(new { results = Array.Empty<object>() });
        }

        var projectRows = await _db.Project
            .AsNoTracking()
            .Where(p => p.ProjectName.Contains(term) || p.CommercialTitleNl.Contains(term))
            .OrderBy(p => p.ProjectName)
            .Select(p => new { p.ProjectId, p.ProjectName, p.CommercialTitleNl })
            .Take(25)
            .ToListAsync(ct);

        var clientRows = await _db.ClientAccount
            .AsNoTracking()
            .Where(c => c.Name.Contains(term) || c.CompanyName.Contains(term))
            .OrderBy(c => c.CompanyName)
            .ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.CompanyName, c.Email })
            .Take(25)
            .ToListAsync(ct);

        var supplierRows = await _db.CompanyInfo
            .AsNoTracking()
            .Where(c => c.BedrijfsNaam.Contains(term))
            .OrderBy(c => c.BedrijfsNaam)
            .Select(c => new { c.CompanyId, c.BedrijfsNaam, c.Email })
            .Take(25)
            .ToListAsync(ct);

        var sections = new List<SearchResultSection>();

        if (projectRows.Count > 0)
        {
            sections.Add(new SearchResultSection
            {
                Text = "Aanbevolen resultaten",
                Children = projectRows
                    .Select(project => new SearchResultItem
                    {
                        Title = string.IsNullOrWhiteSpace(project.ProjectName)
                            ? project.CommercialTitleNl ?? "(zonder naam)"
                            : project.ProjectName,
                        Subtitle = string.IsNullOrWhiteSpace(project.CommercialTitleNl)
                            || string.Equals(project.ProjectName, project.CommercialTitleNl, StringComparison.OrdinalIgnoreCase)
                            ? null
                            : project.CommercialTitleNl,
                        Url = Url.Action("Detail", "Projecten", new { projectid = project.ProjectId }) ?? "#"
                    })
                    .ToList()
            });
        }

        var alternativeItems = new List<SearchResultItem>();
        if (clientRows.Count > 0)
        {
            alternativeItems.AddRange(clientRows
                .Select(client => new SearchResultItem
                {
                    Title = string.IsNullOrWhiteSpace(client.CompanyName) ? client.Name : client.CompanyName,
                    Subtitle = string.IsNullOrWhiteSpace(client.CompanyName)
                        ? client.Email
                        : $"{client.Name}{(string.IsNullOrWhiteSpace(client.Email) ? string.Empty : $" • {client.Email}")}",
                    Url = Url.Action("Details", "Klanten", new { id = client.Id }) ?? "#"
                })
                .ToList());
        }

        if (supplierRows.Count > 0)
        {
            alternativeItems.AddRange(supplierRows
                .Select(supplier => new SearchResultItem
                {
                    Title = supplier.BedrijfsNaam ?? "(zonder naam)",
                    Subtitle = supplier.Email,
                    Url = Url.Action("Details", "Leveranciers", new { id = supplier.CompanyId }) ?? "#"
                })
                .ToList());
        }

        if (alternativeItems.Count > 0)
        {
            sections.Add(new SearchResultSection
            {
                Text = "Alternatieve resultaten",
                Children = alternativeItems
            });
        }
        return Json(new { results = sections });
    }

    private class SearchResultSection
    {
        public string Text { get; init; } = string.Empty;

        public IReadOnlyList<SearchResultItem> Children { get; init; } = Array.Empty<SearchResultItem>();
    }

    private class SearchResultItem
    {
        public string Id => Url ?? string.Empty;

        public string Text => Title;

        public string Title { get; init; } = string.Empty;

        public string? Subtitle { get; init; }

        public string? Url { get; init; }
    }
}