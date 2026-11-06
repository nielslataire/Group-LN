using DALCore.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ServiceCore.Invoicing
{
    /// <summary>
    /// Alle rekenwerk van terugkerende facturen (design-handoff 49/50) op één plaats, zonder database: de k-de factuurdatum van een schema, weekend en feestdagen,
    /// de periode op de factuur, de variabelen in een omschrijving ({maand}, {jaar}, {kwartaal}, {periode}, {volgnr}) en de indexering.
    /// De planning in het formulier (JS) rekent dit na voor het voorbeeld; de server beslist.
    /// </summary>
    public static class RecurringScheduleCalculator
    {
        private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-BE");
        private const int MaxScan = 1200;

        // ───────────────────────── factuurdatums ─────────────────────────

        /// <summary>De k-de factuurdatum (k begint bij 0 = de eerste factuur) na toepassing van de weekend- en feestdagregel.</summary>
        public static DateOnly OccurrenceDate(RecurringInvoiceSchedule s, int k)
        {
            if (k < 0) k = 0;
            DateOnly raw;
            if (k == 0)
                raw = s.FirstDate;
            else if (s.Frequency == 1)
                raw = s.FirstDate.AddDays(7 * k);
            else
            {
                var step = s.Frequency switch { 3 => 3, 4 => 12, _ => 1 };
                var monthStart = new DateOnly(s.FirstDate.Year, s.FirstDate.Month, 1).AddMonths(k * step);
                var dim = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
                var day = s.DayMode == 2 ? dim : Math.Min(Math.Max((int)s.DayOfMonth, 1), dim);
                raw = new DateOnly(monthStart.Year, monthStart.Month, day);
            }
            return AdjustForWeekend(raw, s.WeekendRule);
        }

        /// <summary>Regel 1: volgende werkdag, 2: vorige werkdag, 3: ongewijzigd. Een werkdag is geen weekend en geen Belgische feestdag.</summary>
        public static DateOnly AdjustForWeekend(DateOnly d, byte rule)
        {
            if (rule == 3) return d;
            var step = rule == 2 ? -1 : 1;
            var guard = 0;
            while (!IsWorkingDay(d) && guard++ < 14)
                d = d.AddDays(step);
            return d;
        }

        public static bool IsWorkingDay(DateOnly d) => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday && !IsBelgianHoliday(d);

        public static bool IsBelgianHoliday(DateOnly d)
        {
            if ((d.Month == 1 && d.Day == 1) || (d.Month == 5 && d.Day == 1) || (d.Month == 7 && d.Day == 21) || (d.Month == 8 && d.Day == 15)
                || (d.Month == 11 && d.Day == 1) || (d.Month == 11 && d.Day == 11) || (d.Month == 12 && d.Day == 25))
                return true;
            var easter = EasterSunday(d.Year);
            return d == easter.AddDays(1) || d == easter.AddDays(39) || d == easter.AddDays(50);
        }

        /// <summary>Paaszondag (Gregoriaans, anonieme methode).</summary>
        public static DateOnly EasterSunday(int year)
        {
            int a = year % 19, b = year / 100, c = year % 100, d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7, m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31, day = (h + l - 7 * m + 114) % 31 + 1;
            return new DateOnly(year, month, day);
        }

        /// <summary>De eerstvolgende factuurdatum die op of na <paramref name="notBefore"/> valt, met zijn index in het patroon. Null als het schema dan afgelopen is.</summary>
        public static (int Index, DateOnly? Date) FirstOnOrAfter(RecurringInvoiceSchedule s, DateOnly notBefore, int startIndex = 0)
        {
            for (var k = Math.Max(startIndex, 0); k < Math.Max(startIndex, 0) + MaxScan; k++)
            {
                var d = OccurrenceDate(s, k);
                if (d >= notBefore) return (k, WithinEnd(s, d) ? d : null);
            }
            return (startIndex, null);
        }

        /// <summary>De eerstvolgende factuurdatum strikt na <paramref name="after"/> (bv. de laatste gemaakte factuur).</summary>
        public static (int Index, DateOnly? Date) FirstAfter(RecurringInvoiceSchedule s, DateOnly after) => FirstOnOrAfter(s, after.AddDays(1));

        /// <summary>Valt de datum nog binnen het einde van het schema? Het einde na aantal facturen volgt uit <see cref="RecurringInvoiceSchedule.CreatedCount"/> en wordt door de aanroeper bewaakt.</summary>
        public static bool WithinEnd(RecurringInvoiceSchedule s, DateOnly date) => s.EndMode != 2 || !s.EndDate.HasValue || date <= s.EndDate.Value;

        public static bool CountReached(RecurringInvoiceSchedule s) => s.EndMode == 1 && s.EndCount.HasValue && s.CreatedCount >= s.EndCount.Value;

        /// <summary>De komende <paramref name="n"/> datums vanaf index <paramref name="fromIndex"/>, voor de planning en het blok Komende.</summary>
        public static List<(int Index, DateOnly Date)> Upcoming(RecurringInvoiceSchedule s, int fromIndex, int n, int alreadyCreated)
        {
            var list = new List<(int, DateOnly)>();
            for (var k = fromIndex; list.Count < n && k < fromIndex + MaxScan; k++)
            {
                var d = OccurrenceDate(s, k);
                if (!WithinEnd(s, d)) break;
                if (s.EndMode == 1 && s.EndCount.HasValue && alreadyCreated + list.Count >= s.EndCount.Value) break;
                list.Add((k, d));
            }
            return list;
        }

        /// <summary>Het totaal aantal facturen van het schema als dat vastligt (na aantal), anders null.</summary>
        public static int? TotalPlanned(RecurringInvoiceSchedule s)
        {
            if (s.EndMode == 1) return s.EndCount;
            if (s.EndMode == 2 && s.EndDate.HasValue)
            {
                var n = 0;
                for (var k = 0; k < MaxScan; k++)
                {
                    if (OccurrenceDate(s, k) > s.EndDate.Value) break;
                    n++;
                }
                return n;
            }
            return null;
        }

        // ───────────────────────── periode en variabelen ─────────────────────────

        /// <summary>De periode waarop de factuur van <paramref name="occurrence"/> slaat: de lopende of de vorige week, maand, kwartaal of jaar.</summary>
        public static (DateOnly From, DateOnly To) Period(RecurringInvoiceSchedule s, DateOnly occurrence)
        {
            var back = s.PeriodMode == 2 ? 1 : 0;
            switch (s.Frequency)
            {
                case 1:
                    var monday = occurrence.AddDays(-(((int)occurrence.DayOfWeek + 6) % 7)).AddDays(-7 * back);
                    return (monday, monday.AddDays(6));
                case 3:
                    var q = new DateOnly(occurrence.Year, ((occurrence.Month - 1) / 3) * 3 + 1, 1).AddMonths(-3 * back);
                    return (q, q.AddMonths(3).AddDays(-1));
                case 4:
                    var y = new DateOnly(occurrence.Year - back, 1, 1);
                    return (y, new DateOnly(y.Year, 12, 31));
                default:
                    var m = new DateOnly(occurrence.Year, occurrence.Month, 1).AddMonths(-back);
                    return (m, m.AddMonths(1).AddDays(-1));
            }
        }

        public static string MonthName(int month) => Nl.DateTimeFormat.GetMonthName(month).ToLowerInvariant();

        /// <summary>"December 2026", "Q4 2026", "2026", "Week 49 2026".</summary>
        public static string PeriodText(RecurringInvoiceSchedule s, DateOnly occurrence)
        {
            var (from, _) = Period(s, occurrence);
            return s.Frequency switch
            {
                1 => $"Week {ISOWeek.GetWeekOfYear(from.ToDateTime(TimeOnly.MinValue))} {ISOWeek.GetYear(from.ToDateTime(TimeOnly.MinValue))}",
                3 => $"Q{(from.Month - 1) / 3 + 1} {from.Year}",
                4 => from.Year.ToString(Nl),
                _ => Capitalize(MonthName(from.Month)) + " " + from.Year
            };
        }

        /// <summary>"dec", "Q4", "2026", "wk 49": het stukje naast het ↻-label in de lijst met facturen.</summary>
        public static string PeriodShort(RecurringInvoiceSchedule s, DateOnly occurrence)
        {
            var (from, _) = Period(s, occurrence);
            return s.Frequency switch
            {
                1 => "wk " + ISOWeek.GetWeekOfYear(from.ToDateTime(TimeOnly.MinValue)),
                3 => "Q" + ((from.Month - 1) / 3 + 1),
                4 => from.Year.ToString(Nl),
                _ => MonthName(from.Month).Substring(0, Math.Min(3, MonthName(from.Month).Length))
            };
        }

        private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], Nl) + s.Substring(1);

        public static readonly string[] Variables = { "{maand}", "{jaar}", "{kwartaal}", "{periode}", "{volgnr}" };

        /// <summary>Vult de variabelen in een tekst in voor de factuur van <paramref name="occurrence"/>. Onbekende accolades blijven staan.</summary>
        public static string Interpolate(string text, RecurringInvoiceSchedule s, DateOnly occurrence, int sequenceNumber)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            var (from, to) = Period(s, occurrence);
            string period = from.Year == to.Year
                ? $"{from:dd/MM} – {to:dd/MM/yyyy}"
                : $"{from:dd/MM/yyyy} – {to:dd/MM/yyyy}";
            return Regex.Replace(text, @"\{(maand|jaar|kwartaal|periode|volgnr)\}", m => m.Groups[1].Value switch
            {
                "maand" => MonthName(from.Month),
                "jaar" => from.Year.ToString(Nl),
                "kwartaal" => "Q" + ((from.Month - 1) / 3 + 1),
                "periode" => period,
                _ => sequenceNumber.ToString(Nl)
            }, RegexOptions.IgnoreCase);
        }

        /// <summary>Het label in de lijst met facturen: de eerste woorden van de eerste lijn, zonder variabelen, plus de korte periode ("Coördinatie · dec").</summary>
        public static string ListLabel(RecurringInvoiceSchedule s, string firstLineText, DateOnly occurrence)
        {
            var clean = Regex.Replace(firstLineText ?? "", @"\{[^}]*\}", " ");
            clean = Regex.Replace(clean, @"[—–-]\s*$", "").Trim();
            clean = Regex.Replace(clean, @"\s+", " ");
            var word = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Schema";
            // "Coördinatievergoeding" wordt "Coördinatie": een label hoort kort te blijven
            word = Regex.Replace(word, @"(vergoeding|kosten)$", "", RegexOptions.IgnoreCase);
            if (word.Length == 0) word = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Schema";
            if (word.Length > 18) word = word.Substring(0, 17) + "…";
            return word + " · " + PeriodShort(s, occurrence);
        }

        // ───────────────────────── indexering ─────────────────────────

        /// <summary>De datums waarop het schema geïndexeerd wordt, strikt na de startdatum voor indexering (of de eerste factuur) en tot en met <paramref name="upTo"/>.</summary>
        public static List<DateOnly> IndexationDates(RecurringInvoiceSchedule s, DateOnly upTo)
        {
            var list = new List<DateOnly>();
            if (!s.IndexationEnabled) return list;
            var baseDate = s.IndexationBaseDate ?? s.FirstDate;
            if (s.IndexationWhen == 2)
            {
                for (var n = 1; n < 100; n++)
                {
                    var d = baseDate.AddYears(n);
                    if (d > upTo) break;
                    list.Add(d);
                }
                return list;
            }
            var month = s.IndexationWhen == 3 ? Math.Clamp((int)(s.IndexationMonth ?? 1), 1, 12) : 1;
            var day = s.IndexationWhen == 3 ? Math.Clamp((int)(s.IndexationDay ?? 1), 1, 31) : 1;
            for (var y = baseDate.Year; y <= upTo.Year; y++)
            {
                var d = new DateOnly(y, month, Math.Min(day, DateTime.DaysInMonth(y, month)));
                if (d > baseDate && d <= upTo) list.Add(d);
            }
            return list;
        }

        /// <summary>Hoeveel indexaties al gebeurd zijn op de factuurdatum van <paramref name="occurrence"/>.</summary>
        public static int IndexationSteps(RecurringInvoiceSchedule s, DateOnly occurrence) => IndexationDates(s, occurrence).Count;

        public static decimal IndexationFactor(RecurringInvoiceSchedule s, int steps)
        {
            if (!s.IndexationEnabled || steps <= 0 || !s.IndexationPercent.HasValue || s.IndexationPercent.Value == 0m) return 1m;
            var f = 1m;
            for (var i = 0; i < steps; i++) f *= 1m + s.IndexationPercent.Value / 100m;
            return f;
        }

        /// <summary>Uitkomst van de indexering op een factuurdatum. <see cref="Ready"/> = false: de gezondheidsindex die nodig is, is nog niet (of niet meer) bekend.</summary>
        public sealed record IndexationInfo(int Steps, decimal Factor, bool Ready, string? Missing = null, decimal? NewIndex = null, decimal? BaseIndex = null, DateOnly? NewMonth = null, DateOnly? BaseMonth = null);

        public static DateOnly MonthBefore(DateOnly d) => new DateOnly(d.Year, d.Month, 1).AddMonths(-1);

        /// <summary>
        /// De indexering op de factuur van <paramref name="occurrence"/>. Gezondheidsindex (type 1): nieuwe prijs = basisprijs × index van de maand vóór de laatste indexatiedatum
        /// (de verjaardag) ÷ index van de maand vóór de startdatum, beide in basis 2013 = 100. Andere types: een percentage per jaar, samengesteld.
        /// <paramref name="healthIndex"/> = de bewaarde indexen per maand (eerste dag van de maand).
        /// </summary>
        public static IndexationInfo IndexationAt(RecurringInvoiceSchedule s, DateOnly occurrence, IReadOnlyDictionary<DateOnly, decimal>? healthIndex = null)
        {
            var steps = IndexationSteps(s, occurrence);
            if (!s.IndexationEnabled || steps == 0) return new IndexationInfo(steps, 1m, true);
            if (s.IndexationType != 1) return new IndexationInfo(steps, IndexationFactor(s, steps), true);

            var last = IndexationDates(s, occurrence).Last();
            var newMonth = MonthBefore(last);
            var baseMonth = MonthBefore(s.IndexationBaseDate ?? s.FirstDate);
            decimal newIndex = 0m, baseIndex = s.IndexationBaseIndex ?? 0m;
            var haveNew = healthIndex != null && healthIndex.TryGetValue(newMonth, out newIndex);
            var haveBase = baseIndex > 0m || (healthIndex != null && healthIndex.TryGetValue(baseMonth, out baseIndex));
            if (!haveNew)
                return new IndexationInfo(steps, 1m, false, $"de gezondheidsindex van {MonthName(newMonth.Month)} {newMonth.Year} is nog niet gepubliceerd", null, haveBase ? baseIndex : null, newMonth, baseMonth);
            if (!haveBase || baseIndex <= 0m)
                return new IndexationInfo(steps, 1m, false, $"de basisindex ({MonthName(baseMonth.Month)} {baseMonth.Year}) is niet bekend: vul ze in bij het schema", newIndex, null, newMonth, baseMonth);
            return new IndexationInfo(steps, newIndex / baseIndex, true, null, newIndex, baseIndex, newMonth, baseMonth);
        }

        /// <summary>Het bedrag per eenheid van een lijn op de factuur van <paramref name="occurrence"/>: geïndexeerd als de lijn dat vraagt.</summary>
        public static decimal LineUnitPrice(RecurringInvoiceSchedule s, RecurringInvoiceScheduleLine line, DateOnly occurrence, IReadOnlyDictionary<DateOnly, decimal>? healthIndex = null)
        {
            var factor = line.Indexable ? IndexationAt(s, occurrence, healthIndex).Factor : 1m;
            return Math.Round(line.UnitPrice * factor, 2, MidpointRounding.AwayFromZero);
        }

        // ───────────────────────── bedragen ─────────────────────────

        /// <summary>Het bedrag excl. btw van één factuur van het schema. De eerste factuur telt ook de lijnen "enkel de eerste" mee.</summary>
        public static decimal InvoiceNet(RecurringInvoiceSchedule s, DateOnly occurrence, bool isFirst, IReadOnlyDictionary<DateOnly, decimal>? healthIndex = null)
            => s.Lines.Where(l => isFirst || !l.FirstOnly)
                .Sum(l => Math.Round(l.Quantity * LineUnitPrice(s, l, occurrence, healthIndex), 2, MidpointRounding.AwayFromZero));

        /// <summary>Wat het schema gemiddeld per maand opbrengt (excl. btw), zonder lijnen die enkel op de eerste factuur komen.</summary>
        public static decimal MonthlyAverage(RecurringInvoiceSchedule s)
        {
            var per = s.Lines.Where(l => !l.FirstOnly).Sum(l => Math.Round(l.Quantity * l.UnitPrice, 2, MidpointRounding.AwayFromZero));
            return s.Frequency switch { 1 => per * 52m / 12m, 3 => per / 3m, 4 => per / 12m, _ => per };
        }

        // ───────────────────────── teksten ─────────────────────────

        public static string FrequencyText(RecurringInvoiceSchedule s)
            => s.Frequency switch
            {
                1 => "Wekelijks",
                3 => "Per kwartaal",
                4 => "Jaarlijks",
                _ => "Maandelijks"
            };

        /// <summary>"Maandelijks · 1ste", "Per kwartaal", "Wekelijks · maandag".</summary>
        public static string RhythmText(RecurringInvoiceSchedule s)
        {
            if (s.Frequency == 1)
                return "Wekelijks · " + new[] { "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag" }[Math.Clamp(s.DayOfMonth - 1, 0, 4)];
            var baseText = FrequencyText(s);
            if (s.Frequency == 4) return baseText;
            return baseText + " · " + (s.DayMode == 2 ? "laatste dag" : Ordinal(s.DayOfMonth));
        }

        public static string Ordinal(int day) => day == 1 || day == 8 || day >= 20 ? day + "ste" : day + "de";
    }
}
