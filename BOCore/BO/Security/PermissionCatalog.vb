Public NotInheritable Class PermissionCatalog

    Public Shared ReadOnly Property All As IReadOnlyList(Of PermissionDefinition) =
        New List(Of PermissionDefinition) From {
            New PermissionDefinition(PermissionCodes.Dashboard, "Dashboard", Nothing, 10),
            New PermissionDefinition(PermissionCodes.Suppliers, "Leveranciers", Nothing, 20),
            New PermissionDefinition(PermissionCodes.SuppliersAll, "Alle leveranciers", PermissionCodes.Suppliers, 21),
            New PermissionDefinition(PermissionCodes.SuppliersByBillingCompany, "Per facturatiebedrijf", PermissionCodes.Suppliers, 22),
            New PermissionDefinition(PermissionCodes.Customers, "Klanten", Nothing, 30),
            New PermissionDefinition(PermissionCodes.CustomersAll, "Alle klanten", PermissionCodes.Customers, 31),
            New PermissionDefinition(PermissionCodes.CustomersByBillingCompany, "Per facturatiebedrijf", PermissionCodes.Customers, 32),
            New PermissionDefinition(PermissionCodes.Projects, "Projecten", Nothing, 40),
            New PermissionDefinition(PermissionCodes.ProjectsWeatherDelay, "Weerverlet", PermissionCodes.Projects, 41),
            New PermissionDefinition(PermissionCodes.ProjectsDetail, "Detail", PermissionCodes.Projects, 42),
            New PermissionDefinition(PermissionCodes.ProjectsCustomers, "Klanten", PermissionCodes.Projects, 43),
            New PermissionDefinition(PermissionCodes.ProjectsSuppliers, "Leveranciers", PermissionCodes.Projects, 44),
            New PermissionDefinition(PermissionCodes.ProjectsPhotos, "Foto's", PermissionCodes.Projects, 45),
            New PermissionDefinition(PermissionCodes.ProjectsNews, "Nieuws", PermissionCodes.Projects, 46),
            New PermissionDefinition(PermissionCodes.ProjectsUnits, "Eenheden", PermissionCodes.Projects, 47),
            New PermissionDefinition(PermissionCodes.ProjectsIssues, "Punten", PermissionCodes.Projects, 48),
            New PermissionDefinition(PermissionCodes.ProjectsDocuments, "Documenten", PermissionCodes.Projects, 49),
            New PermissionDefinition(PermissionCodes.ProjectsInsurances, "Verzekeringen", PermissionCodes.Projects, 50),
            New PermissionDefinition(PermissionCodes.ProjectsInvoicing, "Facturatie", PermissionCodes.Projects, 51),
            New PermissionDefinition(PermissionCodes.ProjectsPostCalculation, "Nacalculatie", PermissionCodes.Projects, 52),
            New PermissionDefinition(PermissionCodes.ProjectsChangeOrders, "Wijzigingsopdrachten", PermissionCodes.Projects, 53),
            New PermissionDefinition(PermissionCodes.ProjectsForSale, "Te Koop", PermissionCodes.Projects, 54),
            New PermissionDefinition(PermissionCodes.ProjectsContacts, "Contacten", PermissionCodes.Projects, 55),
            New PermissionDefinition(PermissionCodes.Invoicing, "Facturatie", Nothing, 60),
            New PermissionDefinition(PermissionCodes.InvoicingByBillingCompany, "Per facturatiebedrijf", PermissionCodes.Invoicing, 61),
            New PermissionDefinition(PermissionCodes.Settings, "Instellingen", Nothing, 70),
            New PermissionDefinition(PermissionCodes.SettingsHolidays, "Vakantiedagen", PermissionCodes.Settings, 71),
            New PermissionDefinition(PermissionCodes.SettingsBillingCompanies, "Facturatiebedrijven", PermissionCodes.Settings, 72),
            New PermissionDefinition(PermissionCodes.SettingsUsers, "Gebruikers", PermissionCodes.Settings, 73),
            New PermissionDefinition(PermissionCodes.SettingsInvoiceTemplates, "Factuurtemplates", PermissionCodes.Settings, 74),
            New PermissionDefinition(PermissionCodes.SettingsActivities, "Activiteiten", PermissionCodes.Settings, 75),
            New PermissionDefinition(PermissionCodes.SettingsIssueNotifications, "Puntmeldingen", PermissionCodes.Settings, 76),
            New PermissionDefinition(PermissionCodes.DocumentCenter, "Documentencentrum", Nothing, 80),
            New PermissionDefinition(PermissionCodes.DocumentCenterByBillingCompany, "Per facturatiebedrijf", PermissionCodes.DocumentCenter, 81)
        }

End Class