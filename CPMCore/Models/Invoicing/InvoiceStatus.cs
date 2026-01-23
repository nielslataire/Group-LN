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

            return status.Trim() switch
            {
                "Draft" => InvoiceStatus.Draft,
                "Issued" or "Numbered" => InvoiceStatus.Issued,
                "Sent" => InvoiceStatus.Sent,
                "PartiallyPaid" => InvoiceStatus.PartiallyPaid,
                "Paid" => InvoiceStatus.Paid,
                "Overdue" => InvoiceStatus.Overdue,
                "Cancelled" => InvoiceStatus.Cancelled,
                "Booked" => InvoiceStatus.Booked,
                "Generating" => InvoiceStatus.Generating,
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