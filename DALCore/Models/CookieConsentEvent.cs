#nullable disable
using System;

namespace DALCore.Models;

public partial class CookieConsentEvent
{
    public int Id { get; set; }

    public string EventType { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
