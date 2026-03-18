namespace CPMCore.Services.Security;

public interface IPermissionResolver
{
    string Resolve(string? controller, string? action);
    PermissionAccessType ResolveAccessType(string? httpMethod, string? action);
}

public class PermissionResolver : IPermissionResolver
{
    public string Resolve(string? controller, string? action)
    {
        controller = controller?.Trim();
        action = action?.Trim();

        if (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase)) return PermissionCodes.Dashboard;
        if (string.Equals(controller, "Leveranciers", StringComparison.OrdinalIgnoreCase)) return PermissionCodes.SuppliersAll;
        if (string.Equals(controller, "Klanten", StringComparison.OrdinalIgnoreCase)) return PermissionCodes.CustomersAll;
        if (string.Equals(controller, "Invoices", StringComparison.OrdinalIgnoreCase)) return PermissionCodes.InvoicingByBillingCompany;
        if (string.Equals(controller, "UserAdmin", StringComparison.OrdinalIgnoreCase) || string.Equals(controller, "AppRoles", StringComparison.OrdinalIgnoreCase)) return PermissionCodes.SettingsUsers;

        if (string.Equals(controller, "Instellingen", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(action, "VacationDays", StringComparison.OrdinalIgnoreCase)) return PermissionCodes.SettingsHolidays;
            if (action?.StartsWith("IssuerCompanies", StringComparison.OrdinalIgnoreCase) == true || action?.StartsWith("Series", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.SettingsBillingCompanies;
            if (action?.StartsWith("InvoiceTemplates", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.SettingsInvoiceTemplates;
            if (action?.StartsWith("Activity", StringComparison.OrdinalIgnoreCase) == true || string.Equals(action, "Activities", StringComparison.OrdinalIgnoreCase)) return PermissionCodes.SettingsActivities;
            return PermissionCodes.Settings;
        }

        if (string.Equals(controller, "Projecten", StringComparison.OrdinalIgnoreCase) || string.Equals(controller, "ProjectIssues", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(action, "Weather", StringComparison.OrdinalIgnoreCase)) return PermissionCodes.ProjectsWeatherDelay;
            if (action?.Contains("Document", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsDocuments;
            if (action?.Contains("Photo", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsPhotos;
            if (action?.Contains("News", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsNews;
            if (action?.Contains("Unit", StringComparison.OrdinalIgnoreCase) == true || action?.Contains("Eenheid", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsUnits;
            if (action?.Contains("Issue", StringComparison.OrdinalIgnoreCase) == true || action?.Contains("Punt", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsIssues;
            if (action?.Contains("Insurance", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsInsurances;
            if (action?.Contains("Sales", StringComparison.OrdinalIgnoreCase) == true || action?.Contains("Fact", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsInvoicing;
            if (action?.Contains("Nacal", StringComparison.OrdinalIgnoreCase) == true || action?.Contains("CostCalculation", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsPostCalculation;
            if (action?.Contains("ChangeOrder", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsChangeOrders;
            if (action?.Contains("TeKoop", StringComparison.OrdinalIgnoreCase) == true || action?.Contains("Sale", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsForSale;
            if (action?.Contains("Contact", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsContacts;
            if (action?.Contains("Client", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsCustomers;
            if (action?.Contains("Supplier", StringComparison.OrdinalIgnoreCase) == true) return PermissionCodes.ProjectsSuppliers;
            return PermissionCodes.ProjectsDetail;
        }

        return PermissionCodes.Dashboard;
    }

    public PermissionAccessType ResolveAccessType(string? httpMethod, string? action)
    {
        if (string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            return PermissionAccessType.Read;

        if (action?.Contains("Delete", StringComparison.OrdinalIgnoreCase) == true
            || action?.Contains("Remove", StringComparison.OrdinalIgnoreCase) == true)
            return PermissionAccessType.Delete;

        return PermissionAccessType.Write;
    }
}