using System;
using System.Collections.Generic;
using System.Text;

namespace CPMCore.Models.Leveranciers;

public class SupplierIndexViewModel
{
    public IReadOnlyList<SupplierListItemViewModel> Suppliers { get; init; } = new List<SupplierListItemViewModel>();
    public IReadOnlyList<ActivityFilterItemViewModel> Activities { get; init; } = new List<ActivityFilterItemViewModel>();
    public IReadOnlyList<IssuerCompanyOptionViewModel> IssuerCompanies { get; init; } = new List<IssuerCompanyOptionViewModel>();
    public int? SelectedIssuerCompanyId { get; init; }
}

public class SupplierListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? EnterpriseNumber { get; init; }
    public string? Phone { get; init; }
    public string? Mobile { get; init; }
    public string? Email { get; init; }
    public string? CountryCode { get; init; }
    public int ContractCount { get; init; }
    public decimal TotalContractAmount { get; init; }
    public IReadOnlyList<int> ActivityIds { get; init; } = new List<int>();
    public IReadOnlyList<int> IssuerCompanyIds { get; init; } = new List<int>();

    public string? PrimaryPhone
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Phone))
            {
                return Phone;
            }

            return string.IsNullOrWhiteSpace(Mobile) ? null : Mobile;
        }
    }
    public string EnterpriseNumberDisplay => FormatEnterpriseNumber(EnterpriseNumber, CountryCode) ?? "-";

    public string PrimaryPhoneDisplay => FormatPhoneNumber(PrimaryPhone) ?? "-";

    private static readonly IReadOnlyDictionary<string, Func<string, string?>> EnterpriseFormatters =
        new Dictionary<string, Func<string, string?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["BE"] = number => FormatBelgianEnterprise(number),
            ["NL"] = number => FormatExact(number, "^\\d{8}$", n => n),
            ["FR"] = number => FormatExact(number, "^\\d{9}$", n => $"{n.Substring(0, 3)} {n.Substring(3, 3)} {n.Substring(6, 3)}"),
            ["DE"] = number => FormatExact(number, "^\\d{9}$", n => n),
            ["LU"] = number => FormatExact(number, "^\\d{8}$", n => n),
            ["ES"] = number => FormatExact(number, "^[A-Z]\\d{7}[A-Z0-9]$", n => n),
            ["IT"] = number => FormatExact(number, "^\\d{11}$", n => n),
            ["PT"] = number => FormatExact(number, "^\\d{9}$", n => n),
            ["AT"] = number => FormatExact(number, "^FN\\d{5}[A-Z]$", n => n),
            ["PL"] = number => FormatExact(number, "^\\d{9}(\\d{5})?$", n => n),
            ["CZ"] = number => FormatExact(number, "^\\d{8}$", n => n),
            ["DK"] = number => FormatExact(number, "^\\d{8}$", n => n),
            ["SE"] = number => FormatExact(number, "^\\d{10}$", n => $"{n.Substring(0, 6)}-{n.Substring(6, 4)}"),
            ["IE"] = number => FormatExact(number, "^\\d{7}[A-Z]{1,2}$", n => n),
            ["GR"] = number => FormatExact(number, "^\\d{9}$", n => n)
        };

    private static string? FormatEnterpriseNumber(string? value, string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return null;
        }

        var prefix = cleaned.Length >= 2 && char.IsLetter(cleaned[0]) && char.IsLetter(cleaned[1])
            ? cleaned.Substring(0, 2)
            : string.Empty;

        var numberPart = cleaned.Substring(prefix.Length);
        if (string.IsNullOrWhiteSpace(numberPart))
        {
            return prefix;
        }

        var sanitizedCountry = SanitizeCountryCode(countryCode);
        var effectiveCountry = !string.IsNullOrEmpty(prefix) ? prefix : sanitizedCountry;
        var formatted = !string.IsNullOrEmpty(effectiveCountry) && EnterpriseFormatters.TryGetValue(effectiveCountry, out var formatter)
            ? formatter(numberPart)
            : null;

        // Assume Belgian-style formatting if no country prefix is present but the number fits the pattern.
        if (formatted == null && string.IsNullOrEmpty(prefix))
        {
            formatted = FormatBelgianEnterprise(numberPart);
        }

        formatted ??= GroupDigits(numberPart);

        return string.IsNullOrEmpty(prefix)
            ? formatted
            : $"{prefix} {formatted}";
    }

    private static string GroupDigits(string numberPart)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < numberPart.Length; i++)
        {
            if (i > 0 && i % 3 == 0)
            {
                builder.Append(' ');
            }

            builder.Append(numberPart[i]);
        }

        return builder.ToString();
    }

    private static string? SanitizeCountryCode(string? countryCode)
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

    private static string? FormatExact(string numberPart, string regexPattern, Func<string, string> formatter)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(numberPart, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return null;
        }

        return formatter(numberPart);
    }

    private static string? FormatBelgianEnterprise(string numberPart)
    {
        if (!numberPart.All(char.IsDigit))
        {
            return null;
        }

        var padded = numberPart.PadLeft(10, '0');
        if (padded.Length > 10)
        {
            padded = padded.Substring(padded.Length - 10);
        }

        return $"{padded.Substring(0, 1)}{padded.Substring(1, 3)}.{padded.Substring(4, 3)}.{padded.Substring(7)}";
    }

    private static string? FormatPhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var hasPlus = trimmed.StartsWith("+");
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        var countryPrefix = string.Empty;
        var nationalNumber = digits;

        if (hasPlus && digits.Length > 2)
        {
            countryPrefix = "+" + digits.Substring(0, 2);
            nationalNumber = digits.Substring(2);

            if (countryPrefix == "+32" && nationalNumber.StartsWith("0"))
            {
                nationalNumber = nationalNumber.Substring(1);
            }
        }
        else if (digits.StartsWith("32") && digits.Length > 9)
        {
            countryPrefix = "+32";
            nationalNumber = digits.Substring(2);

            if (nationalNumber.StartsWith("0"))
            {
                nationalNumber = nationalNumber.Substring(1);
            }
        }

        var formattedNational = FormatBelgianNumber(nationalNumber);
        if (string.IsNullOrEmpty(formattedNational))
        {
            formattedNational = GroupPhoneDigits(nationalNumber);
        }

        return string.IsNullOrEmpty(countryPrefix)
            ? formattedNational
            : $"{countryPrefix} {formattedNational}".Trim();
    }

    private static string FormatBelgianNumber(string digits)
    {
        if (string.IsNullOrWhiteSpace(digits))
        {
            return string.Empty;
        }

        if (digits.Length == 10 && digits.StartsWith("0"))
        {
            return $"{digits.Substring(0, 4)} {digits.Substring(4, 2)} {digits.Substring(6, 2)} {digits.Substring(8, 2)}";
        }

        if (digits.Length == 9 && digits.StartsWith("0"))
        {
            return $"{digits.Substring(0, 2)} {digits.Substring(2, 3)} {digits.Substring(5, 2)} {digits.Substring(7, 2)}";
        }

        if (digits.Length == 9)
        {
            return $"{digits.Substring(0, 3)} {digits.Substring(3, 2)} {digits.Substring(5, 2)} {digits.Substring(7, 2)}";
        }

        if (digits.Length == 8)
        {
            return $"{digits.Substring(0, 2)} {digits.Substring(2, 2)} {digits.Substring(4, 2)} {digits.Substring(6, 2)}";
        }

        return string.Empty;
    }

    private static string GroupPhoneDigits(string digits)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < digits.Length; i++)
        {
            if (i > 0 && i % 2 == 0)
            {
                builder.Append(' ');
            }

            builder.Append(digits[i]);
        }

        return builder.ToString();
    }
}

public class ActivityFilterItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? GroupName { get; init; }
}
