namespace ServiceCore.Invoicing.Pdf.Templates;

public static class DefaultLayouts
{
    public static string Get(string key) => key?.ToLowerInvariant() switch
    {
        "layoutHE" => LayoutHE,
        "layoutb" => LayoutB,
        _ => LayoutA
    };

    public const string LayoutA = @"{
  ""version"": 1,
  ""page"": {
    ""margin"": 36,
    ""bands"": {
      ""top"": {
        ""height"": 24,
        ""color"": ""#01532d""
      },
      ""bottom"": {
        ""height"": 24,
        ""color"": ""#01532d""
      }
    }
  },
  ""theme"": {
    ""primary"": ""#01532d"",
    ""secondary"": ""#111111"",
    ""fontFamily"": ""Avenir"",
    ""logoSource"": ""db:IssuerCompany.LogoBytes""
  },
  ""sections"": [
    {
     ""type"": ""defaultHeader"",
      ""visible"": true
    },
    {
      ""type"": ""parties"",
      ""visible"": false,
      ""columns"": [
        {
          ""title"": ""Klant"",
          ""lines"": [
            ""{{Client.Name}}"",
            ""{{Client.AddressLine}}"",
            ""{{Client.Postal}} {{Client.City}}"",
            ""BTW: {{Client.VAT}}""
          ]
        },
        {
          ""title"": ""Project"",
          ""lines"": [
            ""{{Project.Name}}"",
            ""{{Unit.Name}}"",
            ""{{Unit.Address}}""
          ]
        }
      ]
    },
    {
      ""type"": ""linesTable"",
      ""visible"": true,
      ""showVat"": true,
      ""columns"": [
        {
          ""key"": ""Description"",
          ""label"": ""Omschrijving"",
          ""width"": ""*""
        },
        {
          ""key"": ""Quantity"",
          ""label"": ""Aantal"",
          ""width"": ""60"",
          ""align"": ""left""
        },
        {
          ""key"": ""UnitPrice"",
          ""label"": ""Prijs"",
          ""width"": ""70"",
          ""align"": ""right"",
          ""format"": ""eur""
        },
        {
          ""key"": ""Vat"",
          ""label"": ""BTW"",
          ""width"": ""50"",
          ""align"": ""right"",
          ""format"": ""pct"",
          ""visible"": true
        },
        {
          ""key"": ""Total"",
          ""label"": ""Totaal"",
          ""width"": ""80"",
          ""align"": ""right"",
          ""format"": ""eur""
        }
      ],
      ""emptyText"": ""Geen lijnen""
    },
    {
      ""type"": ""totals"",
      ""visible"": false,
      ""layout"": ""right"",
      ""rows"": [
        {
          ""label"": ""Subtotaal"",
          ""value"": ""{{Totals.Excl|eur}}""
        },
        {
          ""label"": ""BTW"",
          ""value"": ""{{Totals.Vat|eur}}""
        },
        {
          ""label"": ""Totaal"",
          ""value"": ""{{Totals.Incl|eur}}"",
          ""bold"": true,
          ""size"": 12
        }
      ]
    },
    {
      ""type"": ""payment"",
      ""visible"": false,
      ""showStructuredMessage"": true,
      ""structuredLabel"": ""Gestructureerde mededeling"",
      ""structuredValue"": ""{{Payment.Structured}}"",
      ""showQr"": true,
      ""qrSize"": 130,
      ""note"": ""Gelieve te betalen v\u00f3\u00f3r {{Invoice.DueDate:dd/MM/yyyy}}""
    },
    {
      ""type"": ""legal"",
      ""visible"": false,
      ""text"": ""{{IssuerCompany.FooterLegalText}}"",
      ""size"": 8
    },
    {
      ""type"": ""defaultFooter"",
      ""visible"": true
    }
  ]
}";
    public const string LayoutHE = @"{
  ""version"": 1,
  ""page"": {
    ""margin"": 36,
    ""bands"": {
      ""top"": {
        ""height"": 24,
        ""color"": ""#f1971b""
      },
      ""bottom"": {
        ""height"": 24,
        ""color"": ""#f1971b""
      }
    }
  },
  ""theme"": {
    ""primary"": ""#f1971b"",
    ""secondary"": ""#111111"",
    ""fontFamily"": ""Avenir"",
    ""logoSource"": ""db:IssuerCompany.LogoBytes""
  },
  ""sections"": [
    {
     ""type"": ""defaultHeader"",
      ""visible"": true
    },
    {
      ""type"": ""parties"",
      ""visible"": false,
      ""columns"": [
        {
          ""title"": ""Klant"",
          ""lines"": [
            ""{{Client.Name}}"",
            ""{{Client.AddressLine}}"",
            ""{{Client.Postal}} {{Client.City}}"",
            ""BTW: {{Client.VAT}}""
          ]
        },
        {
          ""title"": ""Project"",
          ""lines"": [
            ""{{Project.Name}}"",
            ""{{Unit.Name}}"",
            ""{{Unit.Address}}""
          ]
        }
      ]
    },
    {
      ""type"": ""linesTable"",
      ""visible"": true,
      ""showVat"": true,
      ""columns"": [
        {
          ""key"": ""Description"",
          ""label"": ""Omschrijving"",
          ""width"": ""*""
        },
        {
          ""key"": ""Quantity"",
          ""label"": ""Aantal"",
          ""width"": ""60"",
          ""align"": ""left""
        },
        {
          ""key"": ""UnitPrice"",
          ""label"": ""Prijs"",
          ""width"": ""70"",
          ""align"": ""right"",
          ""format"": ""eur""
        },
        {
          ""key"": ""Vat"",
          ""label"": ""BTW"",
          ""width"": ""50"",
          ""align"": ""right"",
          ""format"": ""pct"",
          ""visible"": true
        },
        {
          ""key"": ""Total"",
          ""label"": ""Totaal"",
          ""width"": ""80"",
          ""align"": ""right"",
          ""format"": ""eur""
        }
      ],
      ""emptyText"": ""Geen lijnen""
    },
    {
      ""type"": ""totals"",
      ""visible"": false,
      ""layout"": ""right"",
      ""rows"": [
        {
          ""label"": ""Subtotaal"",
          ""value"": ""{{Totals.Excl|eur}}""
        },
        {
          ""label"": ""BTW"",
          ""value"": ""{{Totals.Vat|eur}}""
        },
        {
          ""label"": ""Totaal"",
          ""value"": ""{{Totals.Incl|eur}}"",
          ""bold"": true,
          ""size"": 12
        }
      ]
    },
    {
      ""type"": ""payment"",
      ""visible"": false,
      ""showStructuredMessage"": true,
      ""structuredLabel"": ""Gestructureerde mededeling"",
      ""structuredValue"": ""{{Payment.Structured}}"",
      ""showQr"": true,
      ""qrSize"": 130,
      ""note"": ""Gelieve te betalen v\u00f3\u00f3r {{Invoice.DueDate:dd/MM/yyyy}}""
    },
    {
      ""type"": ""legal"",
      ""visible"": false,
      ""text"": ""{{IssuerCompany.FooterLegalText}}"",
      ""size"": 8
    },
    {
      ""type"": ""defaultFooter"",
      ""visible"": true
    }
  ]
}";
    public const string LayoutB = @"{
  ""version"": 1,
  ""page"": {
    ""margin"": 30
  },
  ""theme"": {
    ""primary"": ""#f1971b"",
    ""secondary"": ""#4b5563"",
    ""fontFamily"": ""Avenir"",
    ""logoSource"": ""db:IssuerCompany.LogoBytes""
  },
  ""sections"": [
    {
      ""type"": ""header"",
      ""visible"": true,
      ""left"": [
        ""{{IssuerCompany.Name}}"",
        ""{{IssuerCompany.AddressLine}}"",
        ""{{IssuerCompany.Postal}} {{IssuerCompany.City}}""
      ],
      ""right"": {
        ""image"": ""theme:logo"",
        ""width"": 100
      }
    },
    {
      ""type"": ""headline"",
      ""visible"": true,
      ""title"": ""INVOICE {{Invoice.PublicId}}"",
      ""meta"": [
        ""Date: {{Invoice.IssueDate:dd/MM/yyyy}}""
      ]
    },
    {
      ""type"": ""linesTable"",
      ""visible"": true,
      ""showVat"": false,
      ""columns"": [
        {
          ""key"": ""Description"",
          ""label"": ""Description"",
          ""width"": ""*""
        },
        {
          ""key"": ""Quantity"",
          ""label"": ""Qty"",
          ""width"": ""60"",
          ""align"": ""right""
        },
        {
          ""key"": ""UnitPrice"",
          ""label"": ""Price"",
          ""width"": ""70"",
          ""align"": ""right"",
          ""format"": ""eur""
        },
        {
          ""key"": ""Total"",
          ""label"": ""Total"",
          ""width"": ""80"",
          ""align"": ""right"",
          ""format"": ""eur""
        }
      ]
    },
    {
      ""type"": ""totals"",
      ""visible"": true,
      ""layout"": ""right"",
      ""rows"": [
        {
          ""label"": ""Subtotal"",
          ""value"": ""{{Totals.Excl|eur}}""
        },
        {
          ""label"": ""VAT"",
          ""value"": ""{{Totals.Vat|eur}}""
        },
        {
          ""label"": ""Grand Total"",
          ""value"": ""{{Totals.Incl|eur}}"",
          ""bold"": true,
          ""size"": 12
        }
      ]
    },
    {
      ""type"": ""payment"",
      ""visible"": true,
      ""showStructuredMessage"": true,
      ""structuredLabel"": ""Communication"",
      ""structuredValue"": ""{{Payment.Structured}}"",
      ""showQr"": true,
      ""qrSize"": 120
    },
    {
      ""type"": ""legal"",
      ""visible"": true,
      ""text"": ""{{IssuerCompany.FooterLegalText}}"",
      ""size"": 8
    },
    {
      ""type"": ""footer"",
      ""visible"": true,
      ""center"": ""{{IssuerCompany.Website}} | {{IssuerCompany.Email}}"",
      ""size"": 8
    }
  ]
}";
}
