using BOCore;

namespace CPMCore.Models.Instellingen
{
    public class CookieConsentStatsVM
    {
        public CookieConsentStatsPeriodVM Last30Days { get; set; }
        public CookieConsentStatsPeriodVM AllTime { get; set; }
    }

    public class CookieConsentStatsPeriodVM
    {
        public string Titel { get; set; }
        public string Subtitel { get; set; }
        public CookieConsentStatsBO Stats { get; set; }
    }
}
