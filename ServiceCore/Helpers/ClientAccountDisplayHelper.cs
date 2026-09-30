using System.Collections.Generic;
using System.Linq;

namespace ServiceCore.Helpers
{
    /// <summary>UI-weergavenaam voor een klantenaccount met mede-eigenaars — schermen/lijsten/kaarten
    /// ALLEEN (Niels, 2026-09-30). <c>ClientAccountBO.DisplayName</c> zelf blijft ongemoeid: die is de
    /// "akte"-naam (enkel eigenaar 1, achternaam eerst) en wordt ook op facturen/officiële documenten
    /// gebruikt, waar een stille naamswijziging niet gepast is. Met mede-eigenaars worden hier ALLE
    /// achternamen samengevoegd ("Van Laere – Van Laeken", design-handoff 20a); zonder mede-eigenaars
    /// valt dit terug op de gewone achternaam + voornaam.</summary>
    public static class ClientAccountDisplayHelper
    {
        public static string WithCoOwners(string? mainSurname, string? mainForename, IEnumerable<string?> coOwnerSurnames)
        {
            var surnames = new List<string>();
            if (!string.IsNullOrWhiteSpace(mainSurname)) surnames.Add(mainSurname!.Trim());
            foreach (var s in coOwnerSurnames)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                var trimmed = s!.Trim();
                if (!surnames.Contains(trimmed, System.StringComparer.OrdinalIgnoreCase))
                    surnames.Add(trimmed);
            }

            if (surnames.Count <= 1)
                return string.IsNullOrWhiteSpace(mainForename) ? (mainSurname ?? "") : $"{mainSurname} {mainForename}".Trim();

            return string.Join(" – ", surnames);
        }
    }
}
