using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using BOCore;

namespace CPMCore.Models.Invoicing
{


    public static class InvoiceStatusExtensions
    {
        public static InvoiceStatus FromCode(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return InvoiceStatus.Unknown;

            var normalized = status.Trim().ToLowerInvariant();
            return normalized switch
            {
                "draft" or "concept" => InvoiceStatus.Draft,
                "issued" or "numbered" or "genummerd" => InvoiceStatus.Issued,
                "sent" or "verzonden" => InvoiceStatus.Sent,
                "partiallypaid" or "deels betaald" => InvoiceStatus.PartiallyPaid,
                "paid" or "betaald" => InvoiceStatus.Paid,
                "overdue" or "vervallen" => InvoiceStatus.Overdue,
                "cancelled" or "geannuleerd" => InvoiceStatus.Cancelled,
                "booked" or "geboekt" => InvoiceStatus.Booked,
                "generating" or "bezig met genereren" => InvoiceStatus.Generating,
                _ => InvoiceStatus.Unknown
            };
        }
        public static InvoiceStatus FromId(byte? statusId)
        {
            if (!statusId.HasValue)
                return InvoiceStatus.Unknown;

            var status = (InvoiceStatus)statusId.Value;
            return Enum.IsDefined(typeof(InvoiceStatus), status) ? status : InvoiceStatus.Unknown;
        }

        public static InvoiceStatus FromId(int? statusId)
        {
            if (!statusId.HasValue)
                return InvoiceStatus.Unknown;

            if (statusId.Value < 0 || statusId.Value > byte.MaxValue)
                return InvoiceStatus.Unknown;

            return FromId((byte?)statusId.Value);
        }
        public static string GetDisplayName(this InvoiceStatus status)
        {
            var member = typeof(InvoiceStatus).GetMember(status.ToString()).FirstOrDefault();
            if (member != null)
            {
                var display = member.GetCustomAttribute<DisplayAttribute>();
                if (display != null && !string.IsNullOrWhiteSpace(display.Name))
                    return display.Name!;
            }

            return status.ToString();
        }
    }
}