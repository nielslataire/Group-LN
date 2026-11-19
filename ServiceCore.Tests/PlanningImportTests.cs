using System.Text;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Planning importeren uit MS Project-XML: inlezen (parser) en omzetten naar fases, taken en voorgangers.</summary>
public class PlanningImportTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private const int ProjectId = 1;

    private const string Xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Project xmlns=""http://schemas.microsoft.com/project"">
  <Name>Villa</Name><StartDate>2026-12-01T08:00:00</StartDate><MinutesPerDay>480</MinutesPerDay>
  <Tasks>
    <Task><UID>0</UID><ID>0</ID><Name>Villa</Name><OutlineLevel>0</OutlineLevel><Summary>1</Summary></Task>
    <Task><UID>1</UID><ID>1</ID><Name>Voorbereiding</Name><WBS>1</WBS><OutlineLevel>1</OutlineLevel><Summary>1</Summary></Task>
    <Task><UID>2</UID><ID>2</ID><Name>Werfinrichting</Name><WBS>1.1</WBS><OutlineLevel>2</OutlineLevel><Duration>PT24H0M0S</Duration>
      <ConstraintType>4</ConstraintType><ConstraintDate>2026-12-07T08:00:00</ConstraintDate><Notes>Afsluiting plaatsen</Notes></Task>
    <Task><UID>3</UID><ID>3</ID><Name>Uitzetten door landmeter</Name><WBS>1.2</WBS><OutlineLevel>2</OutlineLevel><Duration>PT6H0M0S</Duration>
      <PredecessorLink><PredecessorUID>2</PredecessorUID><Type>1</Type></PredecessorLink></Task>
    <Task><UID>4</UID><ID>4</ID><Name>Ruwbouw</Name><WBS>2</WBS><OutlineLevel>1</OutlineLevel><Summary>1</Summary></Task>
    <Task><UID>5</UID><ID>5</ID><Name>Funderingen</Name><WBS>2.1</WBS><OutlineLevel>2</OutlineLevel><Duration>PT40H0M0S</Duration>
      <PredecessorLink><PredecessorUID>3</PredecessorUID><Type>1</Type><LinkLag>4800</LinkLag><LagFormat>7</LagFormat></PredecessorLink>
      <PredecessorLink><PredecessorUID>99</PredecessorUID><Type>1</Type></PredecessorLink></Task>
    <Task><UID>6</UID><ID>6</ID><Name>Vloerplaat</Name><WBS>2.2</WBS><OutlineLevel>2</OutlineLevel><Duration>PT80H0M0S</Duration><PercentComplete>100</PercentComplete>
      <ActualFinish>2026-12-20T17:00:00</ActualFinish>
      <PredecessorLink><PredecessorUID>5</PredecessorUID><Type>3</Type><LinkLag>-4800</LinkLag></PredecessorLink>
      <PredecessorLink><PredecessorUID>5</PredecessorUID><Type>1</Type></PredecessorLink></Task>
    <Task><UID>7</UID><ID>7</ID><Name>Oplevering</Name><WBS>3</WBS><OutlineLevel>1</OutlineLevel><Milestone>1</Milestone><Duration>PT0H0M0S</Duration>
      <PredecessorLink><PredecessorUID>4</PredecessorUID><Type>1</Type></PredecessorLink></Task>
    <Task><UID>8</UID><ID>8</ID><Name>Inactief</Name><OutlineLevel>1</OutlineLevel><Active>0</Active><Duration>PT8H0M0S</Duration></Task>
  </Tasks>
</Project>";

    private static PlanningImportBestand Lees(string xml = Xml)
        => MsProjectXmlParser.Lees(new MemoryStream(Encoding.UTF8.GetBytes(xml)), "villa.xml");

    private static (PlanningService Svc, TestDb Db) Maak()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling", StartDateConstruction = new DateOnly(2026, 11, 16) });
        db.Units.Add(new Units { Id = 11, Name = "Woning 1", ProjectId = ProjectId });
        db.Units.Add(new Units { Id = 12, Name = "Woning 2", ProjectId = ProjectId });
        db.SaveChanges();
        return (new PlanningService(db), db);
    }

    private static PlanningCommando Opdracht(PlanningImportBestand f, bool nieuw = true, string naam = "Woning 1", int[]? eenheden = null, string modus = "toevoegen", string startModus = "bestand", int? planningId = null)
        => new()
        {
            Type = "importeren", PlanningId = planningId,
            Import = new PlanningImportOpdracht { Rijen = f.Rijen, Nieuw = nieuw, Naam = naam, EenheidIds = (eenheden ?? new[] { 11 }).ToList(), Modus = modus, StartModus = startModus, BestandStart = f.ProjectStart },
        };

    [Fact]
    public void De_parser_leest_fases_taken_mijlpalen_en_voorgangers_en_rondt_duren_af()
    {
        var f = Lees();
        Assert.True(f.Ok, f.Fout);
        Assert.Equal("2026-12-01", f.ProjectStart);
        Assert.Equal(2, f.AantalFases);
        Assert.Equal(4, f.AantalTaken);
        Assert.Equal(1, f.AantalMijlpalen);
        Assert.Equal(7, f.Rijen.Count);                         // project-samenvatting (UID 0) en inactieve taak vallen weg
        Assert.Contains("1 inactieve taak is overgeslagen.", f.Meldingen);

        var werf = f.Rijen.Single(r => r.Uid == 2);
        Assert.Equal(3, werf.Duur);
        Assert.Equal(2, werf.Beperking);                        // niet eerder beginnen dan
        Assert.Equal("2026-12-07", werf.BeperkingDatum);
        Assert.Equal("Afsluiting plaatsen", werf.Notitie);

        var landmeter = f.Rijen.Single(r => r.Uid == 3);
        Assert.Equal(1, landmeter.Duur);                        // 6 uur → 1 werkdag
        Assert.Equal("waarschuwing", landmeter.Status);
        Assert.Contains("afgerond", landmeter.Melding);

        var fundering = f.Rijen.Single(r => r.Uid == 5);
        var voor = Assert.Single(fundering.Voorgangers);        // voorganger 99 bestaat niet
        Assert.Equal((3, "FS", 1), (voor.Uid, voor.Type, voor.Wacht));
        Assert.Contains("bestaat niet", fundering.Melding);

        var vloer = f.Rijen.Single(r => r.Uid == 6);
        var vv = Assert.Single(vloer.Voorgangers);              // dubbele koppeling naar dezelfde taak: één blijft over
        Assert.Equal("SS", vv.Type);
        Assert.Equal(-1, vv.Wacht);

        // Een voorganger naar een fase wordt wachten op al haar taken.
        var oplevering = f.Rijen.Single(r => r.Uid == 7);
        Assert.Equal(new[] { 5, 6 }, oplevering.Voorgangers.Select(v => v.Uid).OrderBy(x => x).ToArray());
        Assert.Equal("mijlpaal", oplevering.Soort);
        Assert.Equal(0, oplevering.Duur);
    }

    [Fact]
    public void De_parser_weigert_geen_project_en_laat_kringen_weg()
    {
        Assert.False(Lees("<html/>").Ok);
        Assert.False(Lees("geen xml").Ok);

        var kring = @"<Project xmlns=""http://schemas.microsoft.com/project""><Tasks>
          <Task><UID>1</UID><Name>A</Name><OutlineLevel>1</OutlineLevel><Duration>PT8H0M0S</Duration><PredecessorLink><PredecessorUID>2</PredecessorUID></PredecessorLink></Task>
          <Task><UID>2</UID><Name>B</Name><OutlineLevel>1</OutlineLevel><Duration>PT8H0M0S</Duration><PredecessorLink><PredecessorUID>1</PredecessorUID></PredecessorLink></Task>
        </Tasks></Project>";
        var f = Lees(kring);
        Assert.True(f.Ok);
        Assert.Equal(1, f.AantalAfhankelijkheden);
        Assert.Contains(f.Rijen, r => r.Melding != null && r.Melding.Contains("kring"));
    }

    [Fact]
    public async Task Importeren_in_een_nieuwe_planning_maakt_fases_taken_en_voorgangers_en_koppelt_de_eenheid()
    {
        var (svc, db) = Maak();
        var f = Lees();
        var u = await svc.Voer(ProjectId, Opdracht(f), "u");
        Assert.True(u.Ok, u.Fout);

        var bp = await db.BouwPlanning.SingleAsync(p => !p.IsHoofd);
        Assert.Equal("Woning 1", bp.Naam);
        Assert.Equal(new DateOnly(2026, 12, 1), bp.StartDatum);
        Assert.Equal(new[] { 11 }, (await db.BouwPlanningEenheid.Where(e => e.BouwPlanningId == bp.Id).Select(e => e.UnitId).ToListAsync()).ToArray());

        var secties = await db.PlanningSectie.Where(s => s.BouwPlanningId == bp.Id).OrderBy(s => s.Volgorde).ToListAsync();
        Assert.Equal(new[] { "Voorbereiding", "Ruwbouw", "Taken" }, secties.Select(s => s.Naam).ToArray());   // de mijlpaal op niveau 1 komt in "Taken"
        var taken = await db.PlanningTaak.Where(t => secties.Select(s => s.Id).Contains(t.PlanningSectieId)).ToListAsync();
        Assert.Equal(5, taken.Count);
        var werf = taken.Single(t => t.Naam == "Werfinrichting");
        Assert.Equal(3, werf.Duur);
        Assert.Equal(2, werf.BeperkingType);
        var vloer = taken.Single(t => t.Naam == "Vloerplaat");
        Assert.True(vloer.IsAfgerond);
        Assert.Equal(100, vloer.VoortgangPct);
        Assert.Equal(0, taken.Single(t => t.Naam == "Oplevering").Duur);

        var links = await db.PlanningAfhankelijkheid.ToListAsync();
        Assert.Equal(f.AantalAfhankelijkheden, links.Count);
        var fundering = taken.Single(t => t.Naam == "Funderingen");
        var l = links.Single(x => x.OpvolgerTaakId == fundering.Id);
        Assert.Equal(1, l.WachtDagen);

        // De staat toont de nieuwe planning met de eenheid en rekent alles door.
        Assert.Contains(u.Staat!.Planningen, p => p.Naam == "Woning 1" && p.AantalTaken == 5 && p.EenheidIds.Contains(11));
    }

    [Fact]
    public async Task Importeren_in_een_bestaande_planning_voegt_toe_of_vervangt_en_weigert_foute_keuzes()
    {
        var (svc, db) = Maak();
        var f = Lees();
        var eerste = await svc.Voer(ProjectId, Opdracht(f), "u");
        Assert.True(eerste.Ok, eerste.Fout);
        var planningId = eerste.NieuwePlanningId!.Value;
        var aantal = await db.PlanningTaak.CountAsync();
        var versie = eerste.Staat!.Versie;

        var toevoegen = Opdracht(f, nieuw: false, planningId: planningId, startModus: "geen");
        toevoegen.Versie = versie;
        var t = await svc.Voer(ProjectId, toevoegen, "u");
        Assert.True(t.Ok, t.Fout);
        Assert.Equal(aantal * 2, await db.PlanningTaak.CountAsync());

        var vervangen = Opdracht(f, nieuw: false, planningId: planningId, modus: "vervangen", startModus: "geen");
        vervangen.Versie = t.Staat!.Versie;
        var v = await svc.Voer(ProjectId, vervangen, "u");
        Assert.True(v.Ok, v.Fout);
        Assert.Equal(aantal, await db.PlanningTaak.CountAsync());

        // Dezelfde naam of een eenheid die al in een planning zit, wordt geweigerd.
        var dubbel = Opdracht(f, naam: "Woning 1", eenheden: new[] { 12 });
        dubbel.Versie = v.Staat!.Versie;
        var d = await svc.Voer(ProjectId, dubbel, "u");
        Assert.False(d.Ok);
        Assert.Contains("al een planning met deze naam", d.Fout);

        var bezet = Opdracht(f, naam: "Woning 2", eenheden: new[] { 11 });
        bezet.Versie = v.Staat.Versie;
        var b = await svc.Voer(ProjectId, bezet, "u");
        Assert.False(b.Ok);
        Assert.Contains("al in een andere planning", b.Fout);
        Assert.Equal(aantal, await db.PlanningTaak.CountAsync());   // niets half bewaard
    }

    [Fact]
    public async Task Een_uitvoerder_op_een_fase_geldt_voor_haar_taken_tenzij_een_taak_een_eigen_heeft()
    {
        var (svc, db) = Maak();
        db.CompanyInfo.Add(new CompanyInfo { CompanyId = 5, BedrijfsNaam = "Demeyer Dakwerken" });
        db.CompanyInfo.Add(new CompanyInfo { CompanyId = 6, BedrijfsNaam = "Elektro Verstraete" });
        db.Contract.Add(new Contract { Id = 1, ProjectId = ProjectId, CompanyId = 5 });
        await db.SaveChangesAsync();
        var f = Lees();
        var u = await svc.Voer(ProjectId, Opdracht(f), "u");
        Assert.True(u.Ok, u.Fout);
        Assert.Contains(u.Staat!.Uitvoerders, x => x.Id == 5 && x.Naam == "Demeyer Dakwerken" && x.InProject);

        var fase = u.Staat.Rijen.First(r => r.Soort == "fase" && r.Naam == "Voorbereiding");
        var zetFase = new PlanningCommando { Type = "wijzig", Soort = "fase", Id = fase.Id, PlanningId = u.NieuwePlanningId, Versie = u.Staat.Versie, Velden = new() { ["uitvoerder"] = System.Text.Json.JsonSerializer.SerializeToElement(5) } };
        var a = await svc.Voer(ProjectId, zetFase, "u");
        Assert.True(a.Ok, a.Fout);
        var kinderen = a.Staat!.Rijen.Where(r => r.Soort == "taak" && r.OuderFaseId == fase.Id).ToList();
        Assert.NotEmpty(kinderen);
        Assert.All(kinderen, r => { Assert.Equal(5, r.UitvoerderEffectiefId); Assert.True(r.UitvoerderGeerfd); Assert.Null(r.UitvoerderId); });

        var taak = kinderen[0];
        var zetTaak = new PlanningCommando { Type = "wijzig", Soort = "taak", Id = taak.Id, PlanningId = u.NieuwePlanningId, Versie = a.Staat.Versie, Velden = new() { ["uitvoerder"] = System.Text.Json.JsonSerializer.SerializeToElement(6) } };
        var b = await svc.Voer(ProjectId, zetTaak, "u");
        Assert.True(b.Ok, b.Fout);
        var eigen = b.Staat!.Rijen.Single(r => r.Id == taak.Id && r.Soort == "taak");
        Assert.Equal(6, eigen.UitvoerderId);
        Assert.Equal("Elektro Verstraete", eigen.UitvoerderNaam);
        Assert.False(eigen.UitvoerderGeerfd);
    }

    private const string ResXml = @"<Project xmlns=""http://schemas.microsoft.com/project""><Tasks>
      <Task><UID>1</UID><Name>Dak</Name><OutlineLevel>1</OutlineLevel><Duration>PT16H0M0S</Duration></Task>
      <Task><UID>2</UID><Name>Elektriciteit</Name><OutlineLevel>1</OutlineLevel><Duration>PT8H0M0S</Duration></Task>
      <Task><UID>3</UID><Name>Opkuis</Name><OutlineLevel>1</OutlineLevel><Duration>PT8H0M0S</Duration></Task>
    </Tasks><Resources>
      <Resource><UID>0</UID></Resource>
      <Resource><UID>1</UID><Name>Dakw. Demeyer</Name></Resource>
      <Resource><UID>2</UID><Name>Elektro Verstraete</Name></Resource>
      <Resource><UID>3</UID><Name>Niet gebruikt</Name></Resource>
    </Resources><Assignments>
      <Assignment><TaskUID>1</TaskUID><ResourceUID>1</ResourceUID></Assignment>
      <Assignment><TaskUID>2</TaskUID><ResourceUID>2</ResourceUID></Assignment>
      <Assignment><TaskUID>3</TaskUID><ResourceUID>2</ResourceUID></Assignment>
      <Assignment><TaskUID>3</TaskUID><ResourceUID>1</ResourceUID></Assignment>
    </Assignments></Project>";

    [Fact]
    public async Task Resources_worden_gelezen_en_gekoppeld_aan_een_uitvoerder_of_als_tekst_in_de_notitie_bewaard()
    {
        var f = Lees(ResXml);
        Assert.True(f.Ok, f.Fout);
        Assert.Equal(new[] { "Dakw. Demeyer", "Elektro Verstraete" }, f.Resources.Select(r => r.Naam).ToArray());   // enkel resources die aan een taak hangen
        Assert.Equal(2, f.Resources.Single(r => r.Naam == "Dakw. Demeyer").AantalTaken);
        Assert.Equal(new[] { "Elektro Verstraete", "Dakw. Demeyer" }, f.Rijen.Single(r => r.Uid == 3).Resources.ToArray());

        var (svc, db) = Maak();
        db.CompanyInfo.Add(new CompanyInfo { CompanyId = 5, BedrijfsNaam = "Demeyer Dakwerken" });
        db.Contract.Add(new Contract { Id = 1, ProjectId = ProjectId, CompanyId = 5 });   // enkel aannemers met een contract in het project
        await db.SaveChangesAsync();
        var opdracht = Opdracht(f);
        opdracht.Import!.Uitvoerders["Dakw. Demeyer"] = 5;          // Elektro Verstraete blijft ongekoppeld
        var u = await svc.Voer(ProjectId, opdracht, "u");
        Assert.True(u.Ok, u.Fout);

        var dak = await db.PlanningTaak.SingleAsync(t => t.Naam == "Dak");
        Assert.Equal(5, dak.UitvoerderCompanyId);
        Assert.Null(dak.Beschrijving);
        var elek = await db.PlanningTaak.SingleAsync(t => t.Naam == "Elektriciteit");
        Assert.Null(elek.UitvoerderCompanyId);
        Assert.Equal("Uitvoerder (uit MS Project): Elektro Verstraete", elek.Beschrijving);
        var kuis = await db.PlanningTaak.SingleAsync(t => t.Naam == "Opkuis");
        Assert.Equal(5, kuis.UitvoerderCompanyId);                    // de gekoppelde resource wint
        Assert.Equal("Uitvoerder (uit MS Project): Elektro Verstraete", kuis.Beschrijving);
    }
}
