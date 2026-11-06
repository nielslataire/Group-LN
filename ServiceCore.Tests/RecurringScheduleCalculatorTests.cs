using DALCore.Models;
using ServiceCore.Invoicing;
using Xunit;

public class RecurringScheduleCalculatorTests
{
    private static RecurringInvoiceSchedule Monthly(int firstY, int firstM, int firstD, int day, byte weekend = 3) => new()
    {
        Frequency = 2, DayMode = 1, DayOfMonth = (byte)day, FirstDate = new DateOnly(firstY, firstM, firstD), WeekendRule = weekend
    };

    [Fact]
    public void Maandelijks_volgt_de_vaste_dag_na_de_eerste_datum()
    {
        var s = Monthly(2026, 12, 5, 5);
        Assert.Equal(new DateOnly(2026, 12, 5), RecurringScheduleCalculator.OccurrenceDate(s, 0));
        Assert.Equal(new DateOnly(2027, 1, 5), RecurringScheduleCalculator.OccurrenceDate(s, 1));
        Assert.Equal(new DateOnly(2027, 3, 5), RecurringScheduleCalculator.OccurrenceDate(s, 3));
    }

    [Fact]
    public void Dag_31_wordt_de_laatste_dag_van_een_kortere_maand()
    {
        var s = Monthly(2026, 12, 31, 31);
        Assert.Equal(new DateOnly(2027, 2, 28), RecurringScheduleCalculator.OccurrenceDate(s, 2));
    }

    [Fact]
    public void Laatste_dag_van_de_maand()
    {
        var s = Monthly(2026, 11, 30, 1);
        s.DayMode = 2;
        Assert.Equal(new DateOnly(2027, 2, 28), RecurringScheduleCalculator.OccurrenceDate(s, 3));
    }

    [Fact]
    public void Kwartaal_springt_drie_maanden()
    {
        var s = Monthly(2027, 1, 1, 1);
        s.Frequency = 3;
        Assert.Equal(new DateOnly(2027, 4, 1), RecurringScheduleCalculator.OccurrenceDate(s, 1));
        Assert.Equal(new DateOnly(2028, 1, 1), RecurringScheduleCalculator.OccurrenceDate(s, 4));
    }

    [Fact]
    public void Wekelijks_telt_zeven_dagen()
    {
        var s = new RecurringInvoiceSchedule { Frequency = 1, FirstDate = new DateOnly(2026, 11, 30), WeekendRule = 3 };
        Assert.Equal(new DateOnly(2026, 12, 14), RecurringScheduleCalculator.OccurrenceDate(s, 2));
    }

    [Fact]
    public void Weekend_schuift_naar_de_volgende_werkdag()
    {
        // 01/05/2027 is een zaterdag en 1 mei is bovendien een feestdag; maandag 3 mei is de eerstvolgende werkdag
        var s = Monthly(2027, 5, 1, 1, weekend: 1);
        Assert.Equal(new DateOnly(2027, 5, 3), RecurringScheduleCalculator.OccurrenceDate(s, 0));
    }

    [Fact]
    public void Weekend_schuift_naar_de_vorige_werkdag()
    {
        var s = Monthly(2027, 5, 1, 1, weekend: 2);
        Assert.Equal(new DateOnly(2027, 4, 30), RecurringScheduleCalculator.OccurrenceDate(s, 0));
    }

    [Fact]
    public void Paasmaandag_is_een_feestdag()
    {
        Assert.Equal(new DateOnly(2027, 3, 28), RecurringScheduleCalculator.EasterSunday(2027));
        Assert.True(RecurringScheduleCalculator.IsBelgianHoliday(new DateOnly(2027, 3, 29)));
        Assert.False(RecurringScheduleCalculator.IsWorkingDay(new DateOnly(2027, 3, 29)));
    }

    [Fact]
    public void Variabelen_volgen_de_gekozen_periode()
    {
        var lopend = Monthly(2026, 12, 5, 5);
        var vorig = Monthly(2026, 12, 5, 5);
        vorig.PeriodMode = 2;
        var occ = new DateOnly(2027, 1, 5);
        Assert.Equal("Huur januari 2027", RecurringScheduleCalculator.Interpolate("Huur {maand} {jaar}", lopend, occ, 2));
        Assert.Equal("Huur december 2026", RecurringScheduleCalculator.Interpolate("Huur {maand} {jaar}", vorig, occ, 2));
        Assert.Equal("01/01 – 31/01/2027", RecurringScheduleCalculator.Interpolate("{periode}", lopend, occ, 2));
        Assert.Equal("Q4 3", RecurringScheduleCalculator.Interpolate("{kwartaal} {volgnr}", vorig, occ, 3));
    }

    [Fact]
    public void Indexering_op_verjaardag_telt_per_jaar_en_rekent_samengesteld()
    {
        var s = Monthly(2026, 12, 5, 5);
        s.IndexationEnabled = true;
        s.IndexationWhen = 2;
        s.IndexationBaseDate = new DateOnly(2026, 9, 1);
        s.IndexationPercent = 2m;
        Assert.Equal(0, RecurringScheduleCalculator.IndexationSteps(s, new DateOnly(2027, 8, 31)));
        Assert.Equal(1, RecurringScheduleCalculator.IndexationSteps(s, new DateOnly(2027, 9, 1)));
        Assert.Equal(2, RecurringScheduleCalculator.IndexationSteps(s, new DateOnly(2028, 12, 5)));
        Assert.Equal(1.0404m, RecurringScheduleCalculator.IndexationFactor(s, 2));
    }

    [Fact]
    public void Indexering_op_1_januari_begint_het_jaar_na_de_start()
    {
        var s = Monthly(2026, 12, 5, 5);
        s.IndexationEnabled = true;
        s.IndexationWhen = 1;
        Assert.Equal(0, RecurringScheduleCalculator.IndexationSteps(s, new DateOnly(2026, 12, 31)));
        Assert.Equal(1, RecurringScheduleCalculator.IndexationSteps(s, new DateOnly(2027, 1, 5)));
    }

    [Fact]
    public void Enkel_geindexeerde_lijnen_volgen_de_index()
    {
        var s = Monthly(2026, 12, 5, 5);
        s.IndexationEnabled = true;
        s.IndexationType = 3;   // vast percentage (de gezondheidsindex rekent met Statbel-indexen)
        s.IndexationPercent = 2.1m;
        s.IndexationBaseDate = new DateOnly(2026, 12, 5);
        var geindexeerd = new RecurringInvoiceScheduleLine { Quantity = 1, UnitPrice = 1850m, Indexable = true };
        var vast = new RecurringInvoiceScheduleLine { Quantity = 1, UnitPrice = 145m, Indexable = false };
        var occ = new DateOnly(2027, 3, 5);
        Assert.Equal(1888.85m, RecurringScheduleCalculator.LineUnitPrice(s, geindexeerd, occ));
        Assert.Equal(145m, RecurringScheduleCalculator.LineUnitPrice(s, vast, occ));
    }

    [Fact]
    public void Eerste_factuur_telt_de_lijnen_enkel_de_eerste_mee()
    {
        var s = Monthly(2026, 12, 5, 5);
        s.Lines.Add(new RecurringInvoiceScheduleLine { Quantity = 1, UnitPrice = 1000m });
        s.Lines.Add(new RecurringInvoiceScheduleLine { Quantity = 1, UnitPrice = 2000m, FirstOnly = true });
        Assert.Equal(3000m, RecurringScheduleCalculator.InvoiceNet(s, new DateOnly(2026, 12, 5), isFirst: true));
        Assert.Equal(1000m, RecurringScheduleCalculator.InvoiceNet(s, new DateOnly(2027, 1, 5), isFirst: false));
        Assert.Equal(1000m, RecurringScheduleCalculator.MonthlyAverage(s));
    }

    [Fact]
    public void Einde_op_datum_stopt_de_planning()
    {
        var s = Monthly(2026, 12, 5, 5);
        s.EndMode = 2;
        s.EndDate = new DateOnly(2027, 2, 28);
        Assert.Equal(3, RecurringScheduleCalculator.TotalPlanned(s));
        Assert.Equal(3, RecurringScheduleCalculator.Upcoming(s, 0, 10, 0).Count);
    }

    [Fact]
    public void Label_in_de_lijst_is_kort()
    {
        var s = Monthly(2026, 12, 5, 5);
        Assert.Equal("Coördinatie · dec", RecurringScheduleCalculator.ListLabel(s, "Coördinatievergoeding {maand}", new DateOnly(2026, 12, 1)));
    }

    private static readonly Dictionary<DateOnly, decimal> Hi = new()
    {
        [new DateOnly(2025, 8, 1)] = 135.00m, [new DateOnly(2026, 8, 1)] = 140.30m, [new DateOnly(2026, 9, 1)] = 140.67m
    };

    private static RecurringInvoiceSchedule HealthSchedule()
    {
        var s = Monthly(2025, 9, 1, 1);
        s.IndexationEnabled = true; s.IndexationType = 1; s.IndexationWhen = 2; s.IndexationBaseDate = new DateOnly(2025, 9, 1);
        return s;
    }

    [Fact]
    public void Gezondheidsindex_gebruikt_de_maand_voor_de_verjaardag_gedeeld_door_de_maand_voor_de_start()
    {
        var info = RecurringScheduleCalculator.IndexationAt(HealthSchedule(), new DateOnly(2026, 9, 1), Hi);
        Assert.True(info.Ready);
        Assert.Equal(new DateOnly(2026, 8, 1), info.NewMonth);   // augustus 2026, voor de verjaardag 1/9/2026
        Assert.Equal(new DateOnly(2025, 8, 1), info.BaseMonth);  // augustus 2025, voor de start 1/9/2025
        Assert.Equal(140.30m / 135.00m, info.Factor);
    }

    [Fact]
    public void Gezondheidsindex_nog_niet_gepubliceerd_wacht()
    {
        var info = RecurringScheduleCalculator.IndexationAt(HealthSchedule(), new DateOnly(2026, 10, 1), new Dictionary<DateOnly, decimal> { [new DateOnly(2025, 8, 1)] = 135.00m });
        Assert.False(info.Ready);
        Assert.Contains("gepubliceerd", info.Missing);
        Assert.Equal(1m, info.Factor);
    }

    [Fact]
    public void Basisindex_zelf_ingevuld_vervangt_de_bewaarde_index()
    {
        var s = HealthSchedule();
        s.IndexationBaseIndex = 130m;
        var info = RecurringScheduleCalculator.IndexationAt(s, new DateOnly(2026, 9, 1), new Dictionary<DateOnly, decimal> { [new DateOnly(2026, 8, 1)] = 140.30m });
        Assert.True(info.Ready);
        Assert.Equal(140.30m / 130m, info.Factor);
    }

    [Fact]
    public void Geindexeerde_lijn_volgt_de_verhouding_en_rondt_af()
    {
        var s = HealthSchedule();
        var line = new RecurringInvoiceScheduleLine { Quantity = 1, UnitPrice = 1000m, Indexable = true };
        Assert.Equal(1039.26m, RecurringScheduleCalculator.LineUnitPrice(s, line, new DateOnly(2026, 9, 1), Hi));
    }

    [Fact]
    public void Statbel_json_wordt_gelezen()
    {
        var json = "{\"facts\":[{\"Year\":\"2025\",\"Month\":\"October 2025\",\"Level 1\":\"Health index\",\"Base year\":\"2013 = 100\",\"Consumer price index\":135.76000000000047}]}";
        var rows = ServiceCore.Invoicing.HealthIndexService.Parse(json);
        Assert.Single(rows);
        Assert.Equal((new DateOnly(2025, 10, 1), (short)2013, 135.76m), rows[0]);
    }
}
