using System;
using BOCore;

namespace ServiceCore.Invoicing
{
    public interface IInvoiceUblBuilder
    {
        InvoiceUblDocument Build(InvoiceDetailBO detail, IssuerCompanyBO issuer);
    }

    public sealed class InvoiceUblDocument
    {
        public string Xml { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public string UblVersion { get; init; } = "2.1";
        public string Profile { get; init; } = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    }
}