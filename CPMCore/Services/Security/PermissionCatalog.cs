using CPMCore.Models.Security;

namespace CPMCore.Services.Security;

public static class PermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } = new List<PermissionDefinition>
    {
        new(PermissionCodes.Dashboard, "Dashboard", null, 10),

        new(PermissionCodes.Suppliers, "Leveranciers", null, 20),
        new(PermissionCodes.SuppliersAll, "Alle leveranciers", PermissionCodes.Suppliers, 21),
        new(PermissionCodes.SuppliersByBillingCompany, "Per facturatiebedrijf", PermissionCodes.Suppliers, 22),

        new(PermissionCodes.Customers, "Klanten", null, 30),
        new(PermissionCodes.CustomersAll, "Alle klanten", PermissionCodes.Customers, 31),
        new(PermissionCodes.CustomersByBillingCompany, "Per facturatiebedrijf", PermissionCodes.Customers, 32),

        new(PermissionCodes.Projects, "Projecten", null, 40),
        new(PermissionCodes.ProjectsWeatherDelay, "Weerverlet", PermissionCodes.Projects, 41),
        new(PermissionCodes.ProjectsDetail, "Detail", PermissionCodes.Projects, 42),
        new(PermissionCodes.ProjectsCustomers, "Klanten", PermissionCodes.Projects, 43),
        new(PermissionCodes.ProjectsSuppliers, "Leveranciers", PermissionCodes.Projects, 44),
        new(PermissionCodes.ProjectsPhotos, "Foto's", PermissionCodes.Projects, 45),
        new(PermissionCodes.ProjectsNews, "Nieuws", PermissionCodes.Projects, 46),
        new(PermissionCodes.ProjectsUnits, "Eenheden", PermissionCodes.Projects, 47),
        new(PermissionCodes.ProjectsIssues, "Punten", PermissionCodes.Projects, 48),
        new(PermissionCodes.ProjectsDocuments, "Documenten", PermissionCodes.Projects, 49),
        new(PermissionCodes.ProjectsInsurances, "Verzekeringen", PermissionCodes.Projects, 50),
        new(PermissionCodes.ProjectsInvoicing, "Facturatie", PermissionCodes.Projects, 51),
        new(PermissionCodes.ProjectsPostCalculation, "Nacalculatie", PermissionCodes.Projects, 52),
        new(PermissionCodes.ProjectsChangeOrders, "Wijzigingsopdrachten", PermissionCodes.Projects, 53),
        new(PermissionCodes.ProjectsForSale, "Te Koop", PermissionCodes.Projects, 54),
        new(PermissionCodes.ProjectsContacts, "Contacten", PermissionCodes.Projects, 55),

        new(PermissionCodes.Invoicing, "Facturatie", null, 60),
        new(PermissionCodes.InvoicingByBillingCompany, "Per facturatiebedrijf", PermissionCodes.Invoicing, 61),

        new(PermissionCodes.Settings, "Instellingen", null, 70),
        new(PermissionCodes.SettingsHolidays, "Vakantiedagen", PermissionCodes.Settings, 71),
        new(PermissionCodes.SettingsBillingCompanies, "Facturatiebedrijven", PermissionCodes.Settings, 72),
        new(PermissionCodes.SettingsUsers, "Gebruikers", PermissionCodes.Settings, 73),
        new(PermissionCodes.SettingsInvoiceTemplates, "Factuurtemplates", PermissionCodes.Settings, 74),
        new(PermissionCodes.SettingsActivities, "Activiteiten", PermissionCodes.Settings, 75),
    };
}