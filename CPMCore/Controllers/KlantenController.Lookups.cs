using BOCore;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Klanten;
using CPMCore.Models.Projecten;
using FacadeCore;
using CPMCore.Services.Octopus;
using CPMCore.Services.Security;
using DALCore.Models;
using DinkToPdf;
using FacadeCore;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Configuration;
using ServiceCore;
using ServiceCore.Signing;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;

namespace CPMCore.Controllers
{
    /// <summary>AJAX-opzoekingen: BTW-nummer valideren (VIES) en postcode zoeken; geen eigen views. Opgesplitst uit KlantenController.cs (okt. 2026, structureren) - views in Views/Klanten/Lookups/. Zelfde partial class: alle private velden/services van KlantenController.cs blijven gewoon bruikbaar.</summary>
    public partial class KlantenController
    {
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

        private static string? CombineEnterpriseNumber(string? countryCode, string? number)
        {
            var sanitizedNumber = SanitizeVatPart(number);
            if (string.IsNullOrWhiteSpace(sanitizedNumber))
            {
                return null;
            }

            var sanitizedCountry = SanitizeVatCountry(countryCode);
            return string.IsNullOrEmpty(sanitizedCountry) ? sanitizedNumber : sanitizedCountry + sanitizedNumber;
        }

        /// <summary>Klantenlijst/breadcrumb-weergavenaam: bedrijfsnaam wint hier bewust (andere regel dan
        /// ClientAccountBO.DisplayName, dat Naam eerst toont) — enkel Voornaam (migratie 057) toegevoegd,
        /// de precedentie blijft ongewijzigd.</summary>
        private static string DisplayNameOf(DALCore.Models.ClientAccount client)
        {
            if (!string.IsNullOrWhiteSpace(client.CompanyName)) return client.CompanyName;
            if (string.IsNullOrWhiteSpace(client.Name)) return client.CompanyName ?? client.Name;
            return string.IsNullOrWhiteSpace(client.Forename) ? client.Name : client.Name + " " + client.Forename;
        }

        private static (string? CountryCode, string? NumberPart) SplitEnterpriseNumber(string? value)
        {
            var sanitized = SanitizeVatPart(value);

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return (null, null);
            }

            if (sanitized.Length >= 2 && char.IsLetter(sanitized[0]) && char.IsLetter(sanitized[1]))
            {
                var prefix = sanitized.Substring(0, 2);
                var remainder = sanitized.Substring(2);
                return (prefix, string.IsNullOrWhiteSpace(remainder) ? null : remainder);
            }

            return (null, sanitized);
        }


        private static string? NormalizeEnterpriseNumber(string? value)
        {
            var (_, numberPart) = SplitEnterpriseNumber(value);
            return string.IsNullOrWhiteSpace(numberPart) ? null : numberPart;
        }

        private static string? SanitizeVatPart(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(char.ToUpperInvariant(ch));
                }
            }

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static string? SanitizeVatCountry(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return null;
            }

            var builder = new StringBuilder(2);
            foreach (var ch in countryCode.Trim())
            {
                if (char.IsLetter(ch))
                {
                    builder.Append(char.ToUpperInvariant(ch));
                }

                if (builder.Length == 2)
                {
                    break;
                }
            }

            return builder.Length == 0 ? null : builder.ToString();
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
}
