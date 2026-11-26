using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Helpers;
using ServiceCore.Invoicing;
using Xunit;

/// <summary>Aktefacturen klaarzetten (grond- en kostenfactuur): bedragen, btw, mede-eigenaars, datumregel, dubbele aanmaak en de instellingen van het facturatiebedrijf.</summary>
public class AkteFactuurServiceTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private sealed class CmdDubbel : IInvoiceCommandService
    {
        public Task<(int invoiceId, string? publicId)> CreateWithLinesAsync(BOCore.InvoiceDraftBO bo, bool issueNow, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CreateDraftAsync(BOCore.InvoiceDraftBO bo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string> IssueDraftAsync(int invoiceId, int? seriesId = null, DateOnly? issueDate = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EnsureDefaultBankAccountAsync(int issuerCompanyId, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(int invoiceId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task MarkAsSentAsync(int invoiceId, DateTime sentAtUtc, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(BOCore.InvoiceUpdateBO bo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateDraftAsync(int invoiceId, BOCore.InvoiceDraftBO bo, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private const int ProjectId = 1, IssuerId = 5, AccountId = 20;

    private static (AkteFactuurService Svc, TestDb Db) Maak(DateOnly? akte, decimal grond = 100000m, int? coOwnerPct = null, Action<IssuerCompany>? issuer = null)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        var uc = new IssuerCompany { Id = IssuerId, Name = "Grond BV", IsActive = true };
        issuer?.Invoke(uc);
        db.IssuerCompany.Add(uc);
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Wilgenhoek", IssuerCompanyIdLandOwner = IssuerId });
        db.ProjectSalesSettings.Add(new ProjectSalesSettings { Projectid = ProjectId, ParcelCost = 1000m, SurveyorCost = 500m });
        db.UnitTypes.Add(new UnitTypes { Id = 1, Name = "Lot", GroupId = 1 });
        db.UnitTypes.Add(new UnitTypes { Id = 2, Name = "Parking", GroupId = 2 });
        db.ClientAccount.Add(new ClientAccount { Id = AccountId, Name = "Janssens", DateDeedOfSale = akte, InvoiceExtra = "Extra vermelding van de klant" });
        db.Units.Add(new Units { Id = 11, Name = "01", ProjectId = ProjectId, TypeId = 1, ClientAccountId = AccountId, LandValueSold = grond });
        db.Units.Add(new Units { Id = 12, Name = "P1", ProjectId = ProjectId, TypeId = 2, ClientAccountId = AccountId, LandValueSold = 0m });
        if (coOwnerPct is int p) db.ClientContacts.Add(new ClientContacts { Id = 30, Name = "Peeters", Forename = "An", ClientAccountId = AccountId, IsCoOwner = true, CoOwnerPercentage = p });
        db.SaveChanges();
        return (new AkteFactuurService(db, new CmdDubbel()), db);
    }

    private static readonly DateOnly Morgen = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Fact]
    public async Task Grond_zonder_btw_en_kosten_met_21_procent_zonder_extra_vermelding_van_de_klant()
    {
        var (svc, _) = Maak(Morgen);
        var plan = await svc.BerekenAsync(ProjectId, new[] { AccountId });

        var grond = Assert.Single(plan.Facturen, f => f.Soort == "Grond");
        var gl = Assert.Single(grond.Draft.Lines);
        Assert.Equal(100000m, gl.Price);
        Assert.Equal(0m, gl.VatPercentage);
        Assert.Equal(IssuerId, grond.Draft.IssuerCompanyId);

        var kosten = Assert.Single(plan.Facturen, f => f.Soort == "Kosten");
        Assert.Equal(new[] { 500m, 1000m }, kosten.Draft.Lines.Select(l => l.Price).OrderBy(x => x).ToArray());
        Assert.All(kosten.Draft.Lines, l => Assert.Equal(21m, l.VatPercentage));

        // de extra vermelding van de klantenaccount komt niet op deze facturen (ook niet via de terugval van de factuurservice)
        Assert.All(plan.Facturen, f => { Assert.True(f.Draft.SkipPartyExtraInfo); Assert.True(string.IsNullOrEmpty(f.Draft.FooterDescription)); });
    }

    [Fact]
    public async Task Kosten_staan_per_hoofdeenheid_niet_per_parking()
    {
        var (svc, _) = Maak(Morgen);
        var plan = await svc.BerekenAsync(ProjectId, new[] { AccountId });
        var kosten = plan.Facturen.Single(f => f.Soort == "Kosten");
        Assert.Equal(new[] { 11 }, kosten.UnitIds);
    }

    [Fact]
    public async Task Een_verleden_akte_of_een_ontbrekende_aktedatum_geeft_geen_facturen_maar_een_opmerking()
    {
        var (svc1, _) = Maak(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));
        var verleden = await svc1.BerekenAsync(ProjectId, new[] { AccountId });
        Assert.Empty(verleden.Facturen);
        Assert.Contains(verleden.Opmerkingen, o => o.Contains("verleden"));

        var (svc2, _) = Maak(null);
        var leeg = await svc2.BerekenAsync(ProjectId, new[] { AccountId });
        Assert.Empty(leeg.Facturen);
        Assert.Contains(leeg.Opmerkingen, o => o.Contains("geen datum"));
    }

    [Fact]
    public async Task De_akte_van_vandaag_kan_nog_klaargezet_worden()
    {
        var (svc, _) = Maak(DateOnly.FromDateTime(DateTime.Today));
        var plan = await svc.BerekenAsync(ProjectId, new[] { AccountId });
        Assert.Equal(2, plan.Facturen.Count);
    }

    [Fact]
    public async Task Een_mede_eigenaar_krijgt_een_eigen_factuur_voor_zijn_aandeel()
    {
        var (svc, _) = Maak(Morgen, coOwnerPct: 40);
        var plan = await svc.BerekenAsync(ProjectId, new[] { AccountId });
        var gronden = plan.Facturen.Where(f => f.Soort == "Grond").OrderByDescending(f => f.Aandeel).ToList();
        Assert.Equal(2, gronden.Count);
        Assert.Equal(60000m, gronden[0].Draft.Lines.Single().Price);
        Assert.True(gronden[0].IsHoofdeigenaar);
        Assert.Equal(40000m, gronden[1].Draft.Lines.Single().Price);
        Assert.Equal(2, gronden[1].Draft.ClientType);   // 2 = mede-eigenaar
        Assert.Equal(4, plan.Facturen.Count);
    }

    [Fact]
    public async Task Een_al_aangemaakte_grondfactuur_komt_er_niet_nog_eens_bij_tenzij_ze_geannuleerd_is()
    {
        var (svc, db) = Maak(Morgen);
        db.Invoices.Add(new Invoices { Id = 900, StatusId = (byte)InvoiceStatusId.Draft });
        db.InvoicesDetails.Add(new InvoicesDetails { Id = 901, InvoiceId = 900, LineType = AkteFactuurService.LineTypeGrond, UnitId = 11, Text = "Grond" });
        db.SaveChanges();
        var plan = await svc.BerekenAsync(ProjectId, new[] { AccountId });
        Assert.DoesNotContain(plan.Facturen, f => f.Soort == "Grond");
        Assert.Contains(plan.Facturen, f => f.Soort == "Kosten");

        db.Invoices.Single(i => i.Id == 900).StatusId = (byte)InvoiceStatusId.Cancelled;
        db.SaveChanges();
        var opnieuw = await svc.BerekenAsync(ProjectId, new[] { AccountId });
        Assert.Contains(opnieuw.Facturen, f => f.Soort == "Grond");
    }

    [Fact]
    public async Task De_btw_en_betaaltermijn_komen_uit_de_instellingen_van_het_facturatiebedrijf()
    {
        var (svc, db) = Maak(Morgen, issuer: i => { i.AkteGrondPaymentTermId = 7; i.AkteKostenVatTypeId = 3; i.AkteKostenPaymentTermId = 8; i.AkteGrondVatTypeId = 4; });
        db.Vattype.Add(new Vattype { Id = 3, IssuerCompanyId = IssuerId, Code = "B6", Description = "6 %", BasePercentage = 6m });
        db.Vattype.Add(new Vattype { Id = 4, IssuerCompanyId = IssuerId, Code = "B0", Description = "0 %", BasePercentage = 0m });
        db.SaveChanges();

        var plan = await svc.BerekenAsync(ProjectId, new[] { AccountId });
        var grond = plan.Facturen.Single(f => f.Soort == "Grond").Draft;
        var kosten = plan.Facturen.Single(f => f.Soort == "Kosten").Draft;
        Assert.Equal(7, grond.PaymentTermId);
        Assert.Equal(4, grond.Lines.Single().VatTypeId);
        Assert.Equal(8, kosten.PaymentTermId);
        Assert.Equal(3, kosten.SelectedVatTypeId);
        Assert.All(kosten.Lines, l => Assert.Equal(6m, l.VatPercentage));
    }

    [Fact]
    public async Task Zonder_facturatiebedrijf_van_de_grondeigenaar_komt_er_een_opmerking()
    {
        var (svc, db) = Maak(Morgen);
        db.Project.Single().IssuerCompanyIdLandOwner = null;
        db.SaveChanges();
        var plan = await svc.BerekenAsync(ProjectId, new[] { AccountId });
        Assert.Empty(plan.Facturen);
        Assert.Contains(plan.Opmerkingen, o => o.Contains("facturatiebedrijf"));
    }
}
