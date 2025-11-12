using System;
using System.Globalization;
using System.Linq;
using BOCore;
using ServiceCore.Invoicing.Pdf;
using CPMCore.Models.Invoicing;

namespace CPMCore.Extensions;

public static class InvoiceDetailExtensions
{
    public static InvoiceDto ToInvoiceDto(this InvoiceDetailBO bo)
    {
        if (bo == null) throw new ArgumentNullException(nameof(bo));

        var issuerAddress = CombineAddress(bo.IssuerAddressLine1, bo.IssuerAddressLine2);

        return new InvoiceDto
        {
            Id = bo.Id,
            PublicId = string.IsNullOrWhiteSpace(bo.PublicId) ? null : bo.PublicId,
            IssueDate = bo.InvoiceDate,
            DueDate = bo.ExpirationDate,
            Status = InvoiceStatusExtensions.FromCode(bo.StatusName).GetDisplayName(),
            BankAccount = bo.BankAccount,
            StructuredMessage = bo.StructuredMessage,
            ExtraInfo = bo?.ExtraInfo,
            QrPayload = bo.QrPayLoad,
            HeaderDescription = bo.HeaderText,
            DetailDescription = bo.DetailText,
            Issuer = new PartyDto
            {
                Name = bo.IssuerName,
                LegalName = bo.IssuerLegalName,
                VatNumber = bo.IssuerVatNumber,
                AddressLine1 = issuerAddress,
                PostalCode = bo.IssuerPostalCode,
                City = bo.IssuerCity,
                Country = bo.IssuerCountryCode,
                Email = bo.IssuerEmail,
                Phone = bo.IssuerPhone,
                BankAccount = bo.IssuerDefaultIban,
                Bic = bo.IssuerDefaultBic,
                LegalFormAbbreviation = bo.IssuerLegalFormAbbreviation
            },
            Client = new PartyDto
            {
                Name = bo.ClientName,
                VatNumber = bo.ClientVatNumber,
                AddressLine1 = bo.ClientAddress,
                PostalCode = bo.ClientPostalCode,
                City = bo.ClientCity,
                Country = bo.ClientCountryName,
                Email = bo.ClientEmail
            },
            Totals = new TotalsDto
            {
                Excl = bo.TotalExclVat,
                Vat = bo.TotalVat,
                Incl = bo.TotalInclVat
            },
            Project = new ProjectInfoDto(),
            Unit = new UnitInfoDto(),
            Lines = bo.Lines.Select(ToInvoiceLineDto).ToList()
        };
    }

    private static InvoiceLineDto ToInvoiceLineDto(InvoiceLineBO line)
    {
        if (line == null) throw new ArgumentNullException(nameof(line));

        var discount = line.DiscountAmount
            ?? (line.DiscountPercent.HasValue
                ? Math.Round(line.Price * (line.DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
                : 0m);

        var net = line.Price - discount;
        var vatAmount = Math.Round(net * (line.VatPercentage / 100m), 2, MidpointRounding.AwayFromZero);
        var total = net + vatAmount;

        return new InvoiceLineDto
        {
            Key = line.Id.ToString(CultureInfo.InvariantCulture),
            Description = line.Text ?? string.Empty,
            LineType = line.LineType,
            GroupName = line.GroupName,
            UnitId = line.UnitId,
            PaymentStageId = line.PaymentStageId,
            ChangeOrderDetailId = line.ChangeOrderDetailId,
            Quantity = 1,
            UnitPrice = RoundCurrency(net),
            Vat = line.VatPercentage,
            Total = RoundCurrency(total)
        };
    }

    private static decimal RoundCurrency(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string? CombineAddress(string? line1, string? line2)
    {
        if (string.IsNullOrWhiteSpace(line1))
            return line2;
        if (string.IsNullOrWhiteSpace(line2))
            return line1;
        return $"{line1}, {line2}";
    }
}