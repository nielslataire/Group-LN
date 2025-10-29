using BOCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCore.Helpers
{
    internal static class SalutationDisplayHelper
    {

        private static readonly IReadOnlyDictionary<string, string> DisplayByName;
        private static readonly IReadOnlyDictionary<string, string> DisplayByValue;

        static SalutationDisplayHelper()
        {
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var byValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Salutation salutation in Enum.GetValues(typeof(Salutation)).Cast<Salutation>())
            {
                var display = ResolveDisplayName(salutation);
                byName[salutation.ToString()] = display;
                byValue[Convert.ToInt32(salutation).ToString()] = display;
            }

            DisplayByName = byName;
            DisplayByValue = byValue;
        }

        public static string? ToDisplayString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var key = value.Trim();

            if (DisplayByName.TryGetValue(key, out var display))
                return display;

            if (DisplayByValue.TryGetValue(key, out display))
                return display;

            if (Enum.TryParse<Salutation>(key, true, out var parsed))
                return DisplayByName[parsed.ToString()];

            return key;
        }

        private static string ResolveDisplayName(Salutation value)
        {
            var member = typeof(Salutation).GetMember(value.ToString()).FirstOrDefault();
            var display = member?.GetCustomAttribute<DisplayAttribute>()?.GetName();
            return string.IsNullOrWhiteSpace(display) ? value.ToString() : display;
        }

    }
}
