using System;
using System.Globalization;
using System.Text;

namespace ServiceCore.Invoicing.Pdf;

public sealed class StructuredReferenceService : IStructuredReferenceService
{
    public string CreateOgm(string base10Digits)
    {
        if (string.IsNullOrWhiteSpace(base10Digits))
            throw new ArgumentException("Base digits required", nameof(base10Digits));

        var digits = FilterDigits(base10Digits);
        if (digits.Length != 10)
            throw new ArgumentException("OGM requires exact 10 digits", nameof(base10Digits));

        var number = long.Parse(digits, CultureInfo.InvariantCulture);
        var checksum = (int)(number % 97);
        if (checksum == 0)
            checksum = 97;

        var combined = number.ToString("0000000000", CultureInfo.InvariantCulture) + checksum.ToString("00", CultureInfo.InvariantCulture);
        var formatted = $"{combined[..3]}/{combined.Substring(3, 4)}/{combined[^5..]}";
        return $"+++{formatted}+++";
    }

    public string CreateRf(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Reference required", nameof(raw));

        var reference = RemoveWhitespace(raw).ToUpperInvariant();
        var prepared = reference + "RF00";
        var numeric = new StringBuilder();
        foreach (var ch in prepared)
        {
            if (char.IsLetter(ch))
                numeric.Append((ch - 'A' + 10).ToString(CultureInfo.InvariantCulture));
            else if (char.IsDigit(ch))
                numeric.Append(ch);
        }

        var mod = Mod97(numeric.ToString());
        var checksum = 98 - mod;
        if (checksum == 0)
            checksum = 97;
        return $"RF{checksum:00}{reference}";
    }

    private static string FilterDigits(string input)
    {
        var sb = new StringBuilder();
        foreach (var ch in input)
        {
            if (char.IsDigit(ch))
                sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string RemoveWhitespace(string input)
    {
        var sb = new StringBuilder();
        foreach (var ch in input)
        {
            if (!char.IsWhiteSpace(ch))
                sb.Append(ch);
        }
        return sb.ToString();
    }

    private static int Mod97(string digits)
    {
        var remainder = 0;
        foreach (var ch in digits)
        {
            var value = ch - '0';
            if (value < 0 || value > 9)
                continue;
            remainder = (remainder * 10 + value) % 97;
        }
        return remainder;
    }
}