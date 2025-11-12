using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace CPMCore.Models.Invoicing
{
    public enum InvoiceStatus
    {
        [Display(Name = "Onbekend")]
        Unknown = 0,

        [Display(Name = "Concept")]
        Draft,

        [Display(Name = "Genummerd")]
        Issued,

        [Display(Name = "Verzonden")]
        Sent,

        [Display(Name = "Deels betaald")]
        PartiallyPaid,

        [Display(Name = "Betaald")]
        Paid,

        [Display(Name = "Vervallen")]
        Overdue,

        [Display(Name = "Geannuleerd")]
        Cancelled
    }

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
                _ => InvoiceStatus.Unknown
            };
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