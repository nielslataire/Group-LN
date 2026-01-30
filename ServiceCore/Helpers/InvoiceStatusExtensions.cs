using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using BOCore;

namespace ServiceCore.Helpers
{
    public static class InvoiceStatusExtensions
    {
        public static string? GetDisplayName(int? statusId)
        {
            if (!statusId.HasValue)
                return null;

            if (statusId.Value < 0 || statusId.Value > byte.MaxValue)
                return null;

            return GetDisplayName((byte?)statusId.Value);
        }

        public static string? GetDisplayName(byte? statusId)
        {
            if (!statusId.HasValue)
                return null;

            var status = (InvoiceStatus)statusId.Value;
            if (!Enum.IsDefined(typeof(InvoiceStatus), status))
                return null;

            var member = typeof(InvoiceStatus).GetMember(status.ToString()).FirstOrDefault();
            var display = member?.GetCustomAttribute<DisplayAttribute>();
            return !string.IsNullOrWhiteSpace(display?.Name) ? display!.Name : status.ToString();
        }
    }
}