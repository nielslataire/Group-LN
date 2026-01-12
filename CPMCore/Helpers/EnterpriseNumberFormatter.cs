using System;
using System.Linq;

namespace CPMCore.Helpers;

public static class EnterpriseNumberFormatter
{
    public static string? Format(string? enterpriseNumber, string? landCode = null)
    {
        if (string.IsNullOrWhiteSpace(enterpriseNumber))
        {
            return null;
        }

        var trimmed = enterpriseNumber.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        var normalizedCode = NormalizeLandCode(landCode);
        if (string.IsNullOrEmpty(normalizedCode))
        {
            normalizedCode = DetectLandCode(trimmed);
        }

        if (!string.IsNullOrEmpty(normalizedCode) && !normalizedCode.Equals("BE", StringComparison.OrdinalIgnoreCase))
        {
            return $"{normalizedCode} {digits}";
        }

        return digits.Length == 10
            ? $"{digits.Substring(0, 4)}.{digits.Substring(4, 3)}.{digits.Substring(7, 3)}"
            : digits;
    }

    private static string? NormalizeLandCode(string? landCode)
    {
        if (string.IsNullOrWhiteSpace(landCode))
        {
            return null;
        }

        return landCode.Trim().ToUpperInvariant();
    }

    private static string? DetectLandCode(string value)
    {
        if (value.Length < 2)
        {
            return null;
        }

        var first = value[0];
        var second = value[1];
        if (!char.IsLetter(first) || !char.IsLetter(second))
        {
            return null;
        }

        return string.Concat(char.ToUpperInvariant(first), char.ToUpperInvariant(second));
    }
}