using System;
using System.Linq;
using System.Text;

namespace CPMCore.Helpers;

// Extractie van SupplierListItemViewModel se eigen private FormatPhoneNumber/FormatBelgianNumber/
// GroupPhoneDigits (Leveranciers/IndexV2 se eigen telefoonkolom) — enkel verplaatst zodat andere
// view models (bv. SupplierDetailViewModel voor DetailsV2) 'm ook kunnen gebruiken zonder de
// volledige BE-groepeerlogica een derde keer te herschrijven. Gedrag ongewijzigd.
public static class PhoneNumberFormatter
{
    public static string? Format(string? value)
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
            formattedNational = GroupDigits(nationalNumber);
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

    private static string GroupDigits(string digits)
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
