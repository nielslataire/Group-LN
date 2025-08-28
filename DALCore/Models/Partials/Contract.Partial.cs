using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;              // of: using DALCore.BO;
namespace DALCore.Models
{
    public partial class Contract
    {
        public IdNameBO GetIdName()
        {
            // ⚠️ Pas aan naar ContractId als jouw sleutel zo heet
            var id = this.Id;

            var companyName = this.Company?.BedrijfsNaam;

            var activityNames = (this.ContractActivity ?? Enumerable.Empty<ContractActivity>())
                .Select(ca => ca.Activity?.Omschrijving)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(companyName))
                parts.Add(companyName!.Trim());
            else
                parts.Add($"Contract {id}");

            if (activityNames.Count > 0)
                parts.AddRange(activityNames!);

            var display = string.Join(" - ", parts);

            return new IdNameBO
            {
                ID = id,
                Display = display
                // Group = ... (optioneel)
            };
        }
    }
}