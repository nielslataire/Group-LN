using System;
using System.Globalization;
using System.Text;
using ServiceCore.Invoicing.Pdf;
using QRCoder;

namespace ServiceCore.Invoicing.Pdf
{
    public sealed class EpcQrService : IEpcQrService
    {
        public byte[] CreatePng(string creditorName, string iban, string bic, decimal amount, string remittance)
        {
            if (string.IsNullOrWhiteSpace(iban))
                throw new ArgumentException("IBAN required", nameof(iban));
            if (string.IsNullOrWhiteSpace(creditorName))
                throw new ArgumentException("Creditor name required", nameof(creditorName));

            var payload = BuildPayload(creditorName, iban, bic, amount, remittance);
            return CreatePngFromPayload(payload);
        }

        public byte[] CreatePngFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                throw new ArgumentException("Payload required", nameof(payload));

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var qr = new PngByteQRCode(data);
            return qr.GetGraphic(20);
        }

        private static string BuildPayload(string creditorName, string iban, string bic, decimal amount, string remittance)
        {
            var builder = new StringBuilder();
            builder.AppendLine("BCD");
            builder.AppendLine("001");
            builder.AppendLine("1");
            builder.AppendLine("SCT");
            builder.AppendLine(bic?.Trim().ToUpperInvariant() ?? string.Empty);
            builder.AppendLine(Trim(creditorName, 70));
            builder.AppendLine(NormalizeIban(iban));
            builder.AppendLine($"EUR{amount.ToString("0.00", CultureInfo.InvariantCulture)}");
            builder.AppendLine();
            builder.AppendLine(Trim(remittance ?? string.Empty, 140));
            return builder.ToString();
        }

        private static string NormalizeIban(string iban)
        {
            var sb = new StringBuilder();
            foreach (var ch in iban)
            {
                if (!char.IsWhiteSpace(ch))
                    sb.Append(char.ToUpperInvariant(ch));
            }
            return sb.ToString();
        }

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var cleaned = value
                .Replace("\r", string.Empty)
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();

            return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
        }
    }
}
