using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Leveranciers;
using CPMCore.Services;
using CPMCore.Services.Octopus;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

/// <summary>AJAX-opzoekingen: leverancier zoeken, BTW-nummer valideren (VIES), postcode zoeken; geen eigen views. Opgesplitst uit LeveranciersController.cs (okt. 2026, structureren) - views in Views/Leveranciers/Lookups/. Zelfde partial class: alle private velden/services van LeveranciersController.cs blijven gewoon bruikbaar.</summary>
public partial class LeveranciersController
{
    [HttpGet]
    public async Task<IActionResult> Lookup(string? term, int take = 20, CancellationToken ct = default)
    {
        var query = _db.CompanyInfo.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var like = $"%{term.Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.BedrijfsNaam, like));
        }

        var results = await query
            .OrderBy(c => c.BedrijfsNaam)
            .Take(take)
            .Select(c => new
            {
                id = c.CompanyId,
                text = c.BedrijfsNaam
            })
            .ToListAsync(ct);

        return Json(new { results });
    }

    [HttpGet]
    public async Task<IActionResult> ValidateVat(string vatNumber, string vatCountryCode, CancellationToken ct)
    {
        var normalized = CombineEnterpriseNumber(vatCountryCode, vatNumber);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return BadRequest(new { error = "Ongeldig ondernemingsnummer" });
        }

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            using var response = await httpClient.GetAsync($"https://controleerbtwnummer.eu/api/validate/{Uri.EscapeDataString(normalized)}.json", ct);
            var rawContent = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = string.IsNullOrWhiteSpace(rawContent)
                    ? "De controle van het btw-nummer is mislukt."
                    : rawContent;

                return StatusCode((int)response.StatusCode, new { error = errorMessage });
            }

            VatLookupResponse? payload;

            try
            {
                payload = JsonSerializer.Deserialize<VatLookupResponse>(rawContent, VatLookupSerializerOptions);
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(new { error = $"Het antwoord van de btw-service kon niet worden gelezen: {jsonEx.Message}" });
            }

            if (payload == null)
            {
                return BadRequest(new { error = "Ontving een leeg antwoord van de btw-service." });
            }

            payload.address ??= new VatLookupAddress();

            if (string.IsNullOrWhiteSpace(payload.countryCode))
            {
                payload.countryCode = payload.address.countryCode;
            }

            return Json(payload);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, new { error = "De btw-service heeft niet tijdig geantwoord. Probeer het later opnieuw." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> FindPostalMatch(string postalCode, string city, string? countryIso, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
        {
            return BadRequest();
        }

        var trimmedPostal = postalCode.Trim();
        var trimmedCity = city?.Trim();
        var normalizedIso = countryIso?.Trim().ToUpperInvariant();

        var baseQuery = _db.PostalCode
            .Include(p => p.Country)
            .AsNoTracking()
            .Where(p => p.Postcode == trimmedPostal);

        if (!string.IsNullOrWhiteSpace(normalizedIso))
        {
            baseQuery = baseQuery.Where(p => p.Country != null && p.Country.LandIsocode != null && p.Country.LandIsocode.ToUpper() == normalizedIso);
        }

        var candidates = await baseQuery
            .OrderBy(p => p.Gemeente)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            return NotFound();
        }

        PostalCode? match = null;

        if (!string.IsNullOrWhiteSpace(trimmedCity))
        {
            match = candidates.FirstOrDefault(p => string.Equals(p.Gemeente?.Trim(), trimmedCity, StringComparison.OrdinalIgnoreCase));
        }

        match ??= candidates.First();

        return Json(new { id = match.PostcodeId, text = $"{match.Postcode} - {match.Gemeente}", countryId = match.CountryId });
    }

    private sealed class VatLookupResponse
    {
        public bool valid { get; set; }

        public string vatNumber { get; set; } = string.Empty;

        public string name { get; set; } = string.Empty;

        public string countryCode { get; set; } = string.Empty;

        public VatLookupAddress? address { get; set; }

        [JsonPropertyName("strAddress")]
        public string? strAddress { get; set; }
    }

    private sealed class VatLookupAddress
    {
        public string street { get; set; } = string.Empty;

        public string number { get; set; } = string.Empty;

        [JsonPropertyName("zip_code")]
        public string zip { get; set; } = string.Empty;

        public string city { get; set; } = string.Empty;

        public string country { get; set; } = string.Empty;

        public string countryCode { get; set; } = string.Empty;
    }
}
