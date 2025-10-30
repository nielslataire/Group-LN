using System;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using CPMCore.Models.Invoicing;

namespace CPMCore.Documents
{
    public class InvoiceDocument : IDocument
    {
        private readonly InvoiceDetailVM _invoice;
        private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("nl-BE");

        public InvoiceDocument(InvoiceDetailVM invoice)
        {
            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Pagina ");
                    txt.CurrentPageNumber();
                    txt.Span(" / ");
                    txt.TotalPages();
                });
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(_invoice.Issuer.LegalName ?? _invoice.Issuer.Name ?? string.Empty)
                        .SemiBold().FontSize(16);
                    if (!string.IsNullOrWhiteSpace(_invoice.Issuer.VatNumber))
                        col.Item().Text($"BTW: {_invoice.Issuer.VatNumber}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(_invoice.BankAccount))
                        col.Item().Text($"Rekening: {_invoice.BankAccount}").FontSize(9);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    var title = string.IsNullOrWhiteSpace(_invoice.PublicId)
                        ? $"Factuur #{_invoice.Id}"
                        : _invoice.PublicId;
                    col.Item().Text(title).SemiBold().FontSize(16);
                    col.Item().Text($"Factuurdatum: {_invoice.InvoiceDate:dd/MM/yyyy}").FontSize(9);
                    if (_invoice.ExpirationDate.HasValue)
                        col.Item().Text($"Vervaldatum: {_invoice.ExpirationDate:dd/MM/yyyy}").FontSize(9);
                    col.Item().Text($"Status: {_invoice.Status}").FontSize(9);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().PaddingVertical(10).Element(ComposePartySection);
                if (!string.IsNullOrWhiteSpace(_invoice.HeaderText) || !string.IsNullOrWhiteSpace(_invoice.DetailText))
                    column.Item().PaddingVertical(5).Element(ComposeDescription);
                column.Item().PaddingTop(10).Element(ComposeLinesTable);
                column.Item().PaddingTop(10).Element(ComposeTotals);
                if (!string.IsNullOrWhiteSpace(_invoice.ExtraInfo))
                    column.Item().PaddingTop(10).Element(ComposeFooterNotes);
            });
        }

        private void ComposePartySection(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().PaddingRight(10).Column(col =>
                {
                    col.Item().Text("Factuurverstrekker").SemiBold().FontSize(12);
                    ComposeParty(col, _invoice.Issuer);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Geadresseerde").SemiBold().FontSize(12);
                    ComposeParty(col, _invoice.Client);
                });
            });
        }

        private static void ComposeParty(ColumnDescriptor column, InvoicePartyVM party)
        {
            column.Item().Text(party.Name ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(party.AddressLine1))
                column.Item().Text(party.AddressLine1);
            if (!string.IsNullOrWhiteSpace(party.AddressLine2))
                column.Item().Text(party.AddressLine2);
            if (!string.IsNullOrWhiteSpace(party.PostalCode) || !string.IsNullOrWhiteSpace(party.City))
                column.Item().Text($"{party.PostalCode} {party.City}".Trim());
            if (!string.IsNullOrWhiteSpace(party.Country))
                column.Item().Text(party.Country);
            if (!string.IsNullOrWhiteSpace(party.VatNumber))
                column.Item().Text($"BTW: {party.VatNumber}");
            if (!string.IsNullOrWhiteSpace(party.Email))
                column.Item().Text($"Email: {party.Email}");
            if (!string.IsNullOrWhiteSpace(party.Phone))
                column.Item().Text($"Tel: {party.Phone}");
        }

        private void ComposeDescription(IContainer container)
        {
            container.Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(_invoice.HeaderText))
                    col.Item().Text(_invoice.HeaderText);
                if (!string.IsNullOrWhiteSpace(_invoice.DetailText))
                    col.Item().Text(_invoice.DetailText).LineHeight(1.2f);
            });
        }

        private void ComposeLinesTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(6);
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Omschrijving").SemiBold();
                    header.Cell().Text("Groep").SemiBold();
                    header.Cell().AlignRight().Text("Excl. btw").SemiBold();
                    header.Cell().AlignRight().Text("BTW %").SemiBold();
                    header.Cell().AlignRight().Text("BTW bedrag").SemiBold();
                    header.Cell().AlignRight().Text("Totaal").SemiBold();
                });

                foreach (var line in _invoice.Lines)
                {
                    table.Cell().Text(line.Text);
                    table.Cell().Text(line.GroupName ?? string.Empty);
                    table.Cell().AlignRight().Text(FormatCurrency(line.NetAmount));
                    table.Cell().AlignRight().Text($"{line.VatRate:0.##}%");
                    table.Cell().AlignRight().Text(FormatCurrency(line.VatAmount));
                    table.Cell().AlignRight().Text(FormatCurrency(line.GrossAmount));
                }
            });
        }

        private void ComposeTotals(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(200).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Totaal excl. btw");
                        r.RelativeItem().AlignRight().Text(FormatCurrency(_invoice.TotalExclVat));
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("BTW");
                        r.RelativeItem().AlignRight().Text(FormatCurrency(_invoice.TotalVat));
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Totaal incl. btw").SemiBold();
                        r.RelativeItem().AlignRight().Text(FormatCurrency(_invoice.TotalInclVat)).SemiBold();
                    });
                    if (_invoice.PaidAmount.HasValue)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Reeds betaald");
                            r.RelativeItem().AlignRight().Text(FormatCurrency(_invoice.PaidAmount.Value));
                        });
                    }
                    if (_invoice.Balance.HasValue)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Openstaand saldo");
                            r.RelativeItem().AlignRight().Text(FormatCurrency(_invoice.Balance.Value));
                        });
                    }
                });
            });
        }

        private void ComposeFooterNotes(IContainer container)
        {
            container.Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(_invoice.BankAccount))
                    col.Item().Text($"Rekeningnummer: {_invoice.BankAccount}");
                col.Item().Text(_invoice.ExtraInfo).LineHeight(1.2f);
            });
        }

        private string FormatCurrency(decimal value) => value.ToString("C", _culture);
    }
}