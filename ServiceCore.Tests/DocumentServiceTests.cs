using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Documents;
using Xunit;

namespace ServiceCore.Tests;

public class DocumentRulesTests
{
    [Theory]
    [InlineData(1, "A")]
    [InlineData(3, "C")]
    [InlineData(26, "Z")]
    [InlineData(27, "AA")]
    [InlineData(0, "—")]
    public void RevisionLabel_IsLetter(int no, string expected) => Assert.Equal(expected, DocumentRules.RevisionLabel(no));

    [Fact]
    public void Ext_IsUpperCaseWithoutDot()
    {
        Assert.Equal("DWG", DocumentRules.Ext("plan.fundering.dwg"));
        Assert.Equal("", DocumentRules.Ext("geen-extensie"));
    }

    [Fact]
    public void LegacyType_OnlyApprovedUnlinkedSalesDocsGoToTheWebsite()
    {
        Assert.Equal(1, DocumentRules.LegacyTypeAfterChange(null, "verkoop", DocumentStatus.Goedgekeurd, 0, false));
        Assert.Null(DocumentRules.LegacyTypeAfterChange(null, "verkoop", DocumentStatus.Concept, 0, false));
        Assert.Null(DocumentRules.LegacyTypeAfterChange(null, "verkoop", DocumentStatus.Goedgekeurd, 1, false));
        Assert.Null(DocumentRules.LegacyTypeAfterChange(null, "verkoop", DocumentStatus.Goedgekeurd, 0, true));
        Assert.Null(DocumentRules.LegacyTypeAfterChange(null, "contracten", DocumentStatus.Goedgekeurd, 0, false));
        Assert.Equal(6, DocumentRules.LegacyTypeAfterChange(6, "keuringen", DocumentStatus.Concept, 2, false));
    }

    [Fact]
    public void Expiry_States()
    {
        var today = new DateOnly(2026, 9, 25);
        Assert.Equal("none", DocumentRules.ExpiryState(null, today));
        Assert.Equal("expired", DocumentRules.ExpiryState(today.AddDays(-1), today));
        Assert.Equal("expiring", DocumentRules.ExpiryState(today.AddDays(90), today));
        Assert.Equal("ok", DocumentRules.ExpiryState(today.AddDays(91), today));
    }

    [Fact]
    public void Frozen_WhenSignedOrAnySignaturePlaced()
    {
        Assert.True(DocumentRules.IsFrozen(DocumentStatus.Getekend, new byte[0]));
        Assert.True(DocumentRules.IsFrozen(DocumentStatus.TerOndertekening, new byte[] { 0, 2 }));
        Assert.False(DocumentRules.IsFrozen(DocumentStatus.TerOndertekening, new byte[] { 0, 1 }));
    }
}

public class DocumentServiceTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name);
    }

    private static (TestDb Db, DocumentService Svc) Create()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.DocumentFolders.AddRange(
            new DocumentFolder { Code = "plannen", Name = "Plannen", SortOrder = 10 },
            new DocumentFolder { Code = "verkoop", Name = "Verkoop", SortOrder = 20 },
            new DocumentFolder { Code = "contracten", Name = "Contracten", SortOrder = 30, ViewKind = "contracten" },
            new DocumentFolder { Code = "offertes", Name = "Offertes", SortOrder = 40, ViewKind = "offertes" },
            new DocumentFolder { Code = "keuringen", Name = "Keuringen", SortOrder = 50, ViewKind = "keuringen" },
            new DocumentFolder { Code = "overige", Name = "Overige", SortOrder = 99 });
        db.Project.Add(new Project { ProjectId = 1, ProjectName = "Villa Cauxyde", ProjectType = 1 });
        db.ClientAccount.Add(new ClientAccount { Id = 10, Name = "Fam. Vermeulen" });
        db.CompanyInfo.AddRange(new CompanyInfo { CompanyId = 20, BedrijfsNaam = "Elektro Pauwels" }, new CompanyInfo { CompanyId = 21, BedrijfsNaam = "Sanitair Decuyper" });
        db.Units.AddRange(new Units { Id = 100, Name = "Lot 1", ProjectId = 1 }, new Units { Id = 101, Name = "Lot 2", ProjectId = 1 },
            new Units { Id = 102, Name = "Berging 1", ProjectId = 1, AttachedUnitId = 100 });
        db.DocumentTemplates.AddRange(
            new DocumentTemplate { Code = "epc", Name = "EPC-certificaat", FolderId = 5, ProjectType = 1, PerUnit = true, LegacyDocType = 5 },
            new DocumentTemplate { Code = "epbstart", Name = "EPB-startverklaring", FolderId = 5, ProjectType = 1, PerUnit = false, LegacyDocType = 10 });
        db.SaveChanges();
        return (db, new DocumentService(db));
    }

    private static int Folder(TestDb db, string code) => db.DocumentFolders.Single(f => f.Code == code).Id;

    private static DocUploadDto Up(int folderId, string name, params DocLinkRef[] links) => new()
    {
        ProjectId = 1, Mode = "new", Name = name, FolderId = folderId, StoredFilename = name + ".pdf", OriginalFilename = name + ".pdf", SizeBytes = 2048,
        Links = links.ToList(), UserId = "u1", UserName = "Niels"
    };

    [Fact]
    public async Task NewApprovedUnlinkedSalesDoc_IsPublicOnLegacyColumns()
    {
        var (db, svc) = Create();
        var res = await svc.Upload(Up(Folder(db, "verkoop"), "Brochure"));
        Assert.True(res.Ok);
        var d = await db.ProjectDocs.Include(x => x.Revisions).SingleAsync();
        Assert.Equal(1, d.Type);
        Assert.Null(d.ClientAccountId);
        Assert.Equal(DocumentStatus.Goedgekeurd, d.Status);
        Assert.Equal(d.Revisions.Single().Id, d.CurrentRevisionId);
    }

    [Fact]
    public async Task ConceptOrClientLinkedDoc_NeverGetsPublicSalesType()
    {
        var (db, svc) = Create();
        var concept = Up(Folder(db, "verkoop"), "Concept-brochure"); concept.Status = DocumentStatus.Concept;
        await svc.Upload(concept);
        await svc.Upload(Up(Folder(db, "verkoop"), "Klantdoc", new DocLinkRef { Type = "client", Id = 10 }));
        var docs = await db.ProjectDocs.ToListAsync();
        Assert.All(docs, d => Assert.Null(d.Type));
        Assert.Equal(10, docs.Single(d => d.Name == "Klantdoc").ClientAccountId);
        Assert.Equal(DocumentStatus.Concept, docs.Single(d => d.Name == "Concept-brochure").Status);
    }

    [Fact]
    public async Task InternalRevision_ReplacesCurrentFile_ButPortalRevisionWaitsForApproval()
    {
        var (db, svc) = Create();
        var first = await svc.Upload(Up(Folder(db, "plannen"), "Plan"));
        var id = first.Id!.Value;

        var b = Up(Folder(db, "plannen"), "Plan-b"); b.Mode = "revision"; b.DocumentId = id;
        await svc.Upload(b);
        var d = await db.ProjectDocs.Include(x => x.Revisions).SingleAsync();
        Assert.Equal("Plan-b.pdf", d.Filename);
        Assert.Equal(new byte[] { 3, 2 }, d.Revisions.OrderBy(r => r.RevisionNo).Select(r => r.Status).ToArray());

        var c = Up(Folder(db, "plannen"), "Plan-c"); c.Mode = "revision"; c.DocumentId = id; c.UploaderKind = DocumentUploaderKind.Leverancier;
        await svc.Upload(c);
        db.ChangeTracker.Clear();
        d = await db.ProjectDocs.Include(x => x.Revisions).SingleAsync();
        Assert.Equal("Plan-b.pdf", d.Filename); // huidige revisie ongewijzigd
        var pending = d.Revisions.Single(r => r.Status == 1);
        Assert.Equal(3, pending.RevisionNo);

        var ok = await svc.ApproveRevision(1, pending.Id, "u1", "Niels");
        Assert.True(ok.Ok);
        db.ChangeTracker.Clear();
        d = await db.ProjectDocs.Include(x => x.Revisions).SingleAsync();
        Assert.Equal("Plan-c.pdf", d.Filename);
        Assert.Equal(pending.Id, d.CurrentRevisionId);
    }

    [Fact]
    public async Task SignedContract_IsFrozen_AndSetsUnitSold()
    {
        var (db, svc) = Create();
        var res = await svc.Upload(Up(Folder(db, "contracten"), "Compromis Lot 2",
            new DocLinkRef { Type = "unit", Id = 101 }, new DocLinkRef { Type = "client", Id = 10 }));
        var id = res.Id!.Value;
        db.Units.Single(u => u.Id == 101).IsOption = true;
        await db.SaveChangesAsync();

        var send = await svc.SendForSignature(1, id, new List<DocSignatoryDto> { new() { Name = "Kevin Claes", ClientAccountId = 10 }, new() { Name = "Niels Lataire" } });
        Assert.True(send.Ok);
        var sigs = await db.DocumentSignatures.OrderBy(s => s.SignOrder).ToListAsync();
        await svc.MarkSigned(1, sigs[0].Id, "itsme");
        Assert.True(db.Units.Single(u => u.Id == 101).IsOption); // nog niet iedereen tekende

        var rev = Up(Folder(db, "contracten"), "Compromis-v2"); rev.Mode = "revision"; rev.DocumentId = id;
        var blocked = await svc.Upload(rev);
        Assert.False(blocked.Ok); // na de eerste handtekening geen nieuwe revisie meer

        var last = await svc.MarkSigned(1, sigs[1].Id, "manueel");
        Assert.Contains("Verkocht", last.Message);
        db.ChangeTracker.Clear();
        Assert.Equal(DocumentStatus.Getekend, db.ProjectDocs.Single().Status);
        var unit = db.Units.Single(u => u.Id == 101);
        Assert.False(unit.IsOption);
        Assert.Equal(10, unit.ClientAccountId);
    }

    [Fact]
    public async Task GenerateExpected_IsPerMainUnitAndIdempotent()
    {
        var (db, svc) = Create();
        var created = await svc.GenerateExpected(1, "u1");
        Assert.Equal(3, created); // EPC × 2 hoofdeenheden (berging telt niet) + EPB-startverklaring × 1 project
        Assert.Equal(0, await svc.GenerateExpected(1, "u1"));
        Assert.Equal(2, db.DocumentRequests.Count(r => r.TemplateId != null && r.UnitId != null));
    }

    [Fact]
    public async Task UploadingAgainstRequest_FulfilsItAndKeepsItsLinks()
    {
        var (db, svc) = Create();
        var req = await svc.CreateRequest(new DocRequestDto
        {
            ProjectId = 1, Name = "Keuringsverslag elektriciteit", FolderId = Folder(db, "keuringen"), UnitId = 100,
            ResponsibleKind = "leverancier", CompanyId = 20, ExpiryYears = 25, SendNow = true
        }, "u1", "Niels");
        var dto = Up(0, ""); dto.FolderId = null; dto.Name = null; dto.RequestId = req.Id; dto.DocDate = new DateOnly(2026, 9, 19); dto.ShareSuppliers = true;
        var res = await svc.Upload(dto);
        Assert.True(res.Ok);
        db.ChangeTracker.Clear();
        var d = await db.ProjectDocs.Include(x => x.Links).SingleAsync();
        Assert.Equal("Keuringsverslag elektriciteit", d.Name);
        Assert.Equal(new DateOnly(2051, 9, 19), d.ExpiresOn);
        Assert.Contains(d.Links, l => l.UnitId == 100);
        Assert.Contains(d.Links, l => l.CompanyId == 20 && l.SharedInPortal);
        Assert.Equal(DocumentRequestStatus.Ontvangen, db.DocumentRequests.Single().Status);

        // Verwijderen zet de aanvraag terug op "aangevraagd"
        await svc.DeleteDocument(1, d.Id);
        Assert.Equal(DocumentRequestStatus.Aangevraagd, db.DocumentRequests.Single().Status);
        Assert.Empty(db.ProjectDocs);
    }

    [Fact]
    public async Task Award_MarksOthersNotAwardedAndCreatesOrderRequest()
    {
        var (db, svc) = Create();
        var f = Folder(db, "offertes");
        var a = Up(f, "Offerte Decuyper", new DocLinkRef { Type = "company", Id = 21 }); a.Perceel = "Sanitair"; a.Amount = 38420m;
        var b = Up(f, "Offerte Meeus", new DocLinkRef { Type = "company", Id = 20 }); b.Perceel = "Sanitair"; b.Amount = 41080m;
        var ra = await svc.Upload(a); await svc.Upload(b);

        var detail = await svc.GetDetail(1, ra.Id!.Value);
        Assert.Equal(2, detail!.Offers.Count);
        Assert.Equal(38420m, detail.Offers[0].Amount);

        var res = await svc.Award(1, ra.Id!.Value, "u1", "Niels");
        Assert.True(res.Ok);
        var docs = await db.ProjectDocs.ToListAsync();
        Assert.Equal(DocumentStatus.Gegund, docs.Single(d => d.Name == "Offerte Decuyper").Status);
        Assert.Equal(DocumentStatus.NietGegund, docs.Single(d => d.Name == "Offerte Meeus").Status);
        Assert.Contains(db.DocumentRequests, r => r.Name.StartsWith("Bestelbon"));
    }

    [Fact]
    public async Task Overview_KeuringenGroupsPerUnitWithCoverage()
    {
        var (db, svc) = Create();
        var k = Folder(db, "keuringen");
        await svc.Upload(Up(k, "EPC Lot 1", new DocLinkRef { Type = "unit", Id = 100 }));
        await svc.GenerateExpected(1, "u1");

        var ov = await svc.GetOverview(1, new DocFilter { Folder = "keuringen" });
        Assert.Equal("keuringen", ov.ViewKind);
        var lot1 = ov.Groups.Single(g => g.Label == "Lot 1");
        Assert.True(lot1.ShowCoverage);
        Assert.Equal(1, lot1.Covered);
        Assert.Equal(2, lot1.CoverageTotal); // EPC-bestand + verwacht EPC
        Assert.Equal(3, ov.Smart.Single(x => x.Key == "ontbreekt").Count);
    }

    [Fact]
    public async Task LegacyRowsWithoutFolderOrRevision_AreRepairedOnOverview()
    {
        var (db, svc) = Create();
        db.ProjectDocs.Add(new ProjectDocs { ProjectId = 1, Name = "Oud EPC", Filename = "oud.pdf", Type = 5, ClientAccountId = 10 });
        db.ProjectDocs.Add(new ProjectDocs { ProjectId = 1, Name = "Oude brochure", Filename = "brochure.pdf", Type = 1 });
        await db.SaveChangesAsync();

        var ov = await svc.GetOverview(1, new DocFilter());
        Assert.Equal(2, ov.Total);
        db.ChangeTracker.Clear();
        var docs = await db.ProjectDocs.Include(d => d.Links).Include(d => d.Revisions).ToListAsync();
        Assert.All(docs, d => { Assert.NotNull(d.FolderId); Assert.NotNull(d.CurrentRevisionId); Assert.Single(d.Revisions); });
        Assert.Single(docs.Single(d => d.Name == "Oud EPC").Links);
        Assert.Equal(10, docs.Single(d => d.Name == "Oud EPC").ClientAccountId);
    }

    [Fact]
    public async Task RemovingLastClientLink_OfLegacyClientSalesDoc_DoesNotExposeItPublicly()
    {
        var (db, svc) = Create();
        db.ProjectDocs.Add(new ProjectDocs { ProjectId = 1, Name = "Klant verkoopdoc", Filename = "k.pdf", Type = 1, ClientAccountId = 10 });
        await db.SaveChangesAsync();
        await svc.GetOverview(1, new DocFilter());
        db.ChangeTracker.Clear();
        var link = db.DocumentLinks.Single();
        var docId = link.DocumentId;
        await svc.RemoveLink(1, docId, link.Id);
        db.ChangeTracker.Clear();
        var d = db.ProjectDocs.Single();
        Assert.Null(d.ClientAccountId);
        Assert.Null(d.Type); // anders zou het als brochure op de publieke site verschijnen
    }

    [Fact]
    public async Task PortalView_ShowsOnlySharedApprovedCurrentRevisions()
    {
        var (db, svc) = Create();
        db.Units.Single(u => u.Id == 100).ClientAccountId = 10;
        await db.SaveChangesAsync();
        var shared = Up(Folder(db, "keuringen"), "Keuring elektriciteit", new DocLinkRef { Type = "unit", Id = 100 }); shared.ShareClients = true;
        var hidden = Up(Folder(db, "plannen"), "Technisch plan", new DocLinkRef { Type = "unit", Id = 100 });
        await svc.Upload(shared); await svc.Upload(hidden);

        var view = await svc.GetPortalDocuments(new DocPortalAudience { Kind = "klant", ClientAccountId = 10 });
        Assert.Single(view.Items);
        Assert.Equal("Keuring elektriciteit", view.Items[0].Name);
        Assert.Equal("Mijn woning", view.Items[0].Group);
    }
}

public class SigningServiceTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name);
    }

    private static (TestDb Db, SigningService Sign, DocumentService Docs, int CoId) Create()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.DocumentFolders.Add(new DocumentFolder { Code = "contracten", Name = "Contracten", ViewKind = "contracten" });
        db.Project.Add(new Project { ProjectId = 1, ProjectName = "Villa Cauxyde" });
        db.ClientAccount.Add(new ClientAccount { Id = 10, Name = "Fam. Vermeulen", Email = "v@example.com" });
        db.Units.Add(new Units { Id = 100, Name = "Lot 1", ProjectId = 1, ClientAccountId = 10, IsOption = true });
        db.Contract.Add(new Contract { Id = 5, ProjectId = 1, CompanyId = 1 });
        db.ContractActivity.Add(new ContractActivity { Id = 7, ContractId = 5, ActivityId = 1 });
        db.ChangeOrder.Add(new ChangeOrder { Id = 3, ClientAccountId = 10, Description = "Extra stopcontacten", Date = new DateOnly(2026, 9, 1), ExpirationDate = new DateOnly(2026, 10, 1), ContractActivityId = 7 });
        db.SaveChanges();
        var docs = new DocumentService(db);
        return (db, new SigningService(db, docs), docs, 3);
    }

    private static SigningCreateDto Dto(params (string name, string email)[] signers) => new()
    {
        StoredFilename = "wo3.pdf", OriginalFilename = "wo3.pdf", SizeBytes = 1000, PdfHash = SigningService.Hash("pdf"), NotifyEmail = "info@groupln.be", UserName = "Niels",
        Signers = signers.Select(s => new SigningSignerDto { Name = s.name, Email = s.email, ClientAccountId = 10 }).ToList()
    };

    [Fact]
    public async Task Create_StoresOnlyHashes_AndKeepsUnitStatusUntouched()
    {
        var (db, sign, _, co) = Create();
        var res = await sign.CreateForChangeOrder(co, Dto(("Kevin Claes", "kevin@example.com")));
        Assert.True(res.Ok, res.Message);
        var issued = Assert.Single(res.Issued);
        Assert.True(issued.Token.Length >= 40);
        var sig = db.DocumentSignatures.Single();
        Assert.Equal(SigningService.Hash(issued.Token), sig.TokenHash);
        Assert.DoesNotContain(issued.Token, sig.TokenHash);
        var doc = db.ProjectDocs.Single();
        Assert.Equal(3, doc.ChangeOrderId);
        Assert.Equal(DocumentStatus.TerOndertekening, doc.Status);
        Assert.NotNull(db.ChangeOrder.Single().DateSendToClient);
        Assert.True(db.Units.Single().IsOption); // wijzigingsopdracht verandert de verkoopstatus nooit
    }

    [Fact]
    public async Task FullFlow_CodeThenSign_FreezesDocumentAndSetsAgreementDate()
    {
        var (db, sign, _, co) = Create();
        var issued = (await sign.CreateForChangeOrder(co, Dto(("Kevin Claes", "kevin@example.com")))).Issued.Single();

        var info = await sign.GetInfo(issued.Token, markOpened: true);
        Assert.Equal("valid", info!.State);
        Assert.Equal(1, db.DocumentSignatures.Single().Status); // geopend

        var wrongEarly = await sign.Sign(issued.Token, "123456", "Kevin Claes", true, "1.2.3.4", "UA");
        Assert.False(wrongEarly.Ok); // nog geen code aangevraagd

        var code = await sign.RequestCode(issued.Token);
        Assert.True(code.Ok);
        Assert.Matches("^[0-9]{6}$", code.Code);
        Assert.NotEqual(code.Code, db.DocumentSignatures.Single().CodeHash); // enkel de hash wordt bewaard

        Assert.False((await sign.Sign(issued.Token, code.Code!, "Kevin Claes", false, "1.2.3.4", "UA")).Ok); // geen akkoord
        Assert.False((await sign.Sign(issued.Token, code.Code!, "K", true, "1.2.3.4", "UA")).Ok);          // naam te kort

        var ok = await sign.Sign(issued.Token, code.Code!, "Kevin Claes", true, "1.2.3.4", "UA");
        Assert.True(ok.Ok, ok.Message);
        Assert.True(ok.AllSigned);
        var s = db.DocumentSignatures.Single();
        Assert.Equal(2, s.Status);
        Assert.Equal("email-otp", s.Method);
        Assert.Equal("1.2.3.4", s.SignedIp);
        Assert.Equal(sign.ConsentText, s.ConsentText);
        Assert.StartsWith("WO-", s.EvidenceRef);
        Assert.Equal(DocumentStatus.Getekend, db.ProjectDocs.Single().Status);
        Assert.NotNull(db.ChangeOrder.Single().DateAgreement);
        Assert.True(db.Units.Single().IsOption);

        // opnieuw tekenen of een tweede revisie uploaden kan niet meer
        Assert.False((await sign.Sign(issued.Token, code.Code!, "Kevin Claes", true, null, null)).Ok);
        var docs = new DocumentService(db);
        var rev = await docs.Upload(new DocUploadDto { ProjectId = 1, Mode = "revision", DocumentId = db.ProjectDocs.Single().Id, StoredFilename = "x.pdf" });
        Assert.False(rev.Ok);

        // het ondertekende PDF komt wel als huidige revisie
        await sign.AttachSignedRevision(db.ProjectDocs.Single().Id, "wo3-signed.pdf", 2000, null);
        db.ChangeTracker.Clear();
        var doc = db.ProjectDocs.Include(d => d.Revisions).Single();
        Assert.Equal("wo3-signed.pdf", doc.Filename);
        Assert.Equal(2, doc.Revisions.Count);
        Assert.Single(await sign.GetEvidence(co));
    }

    [Fact]
    public async Task WrongCode_LocksAfterFiveAttempts_AndNewCodeWorks()
    {
        var (db, sign, _, co) = Create();
        var issued = (await sign.CreateForChangeOrder(co, Dto(("Kevin Claes", "kevin@example.com")))).Issued.Single();
        var c1 = await sign.RequestCode(issued.Token);
        var wrong = c1.Code == "000000" ? "000001" : "000000";
        for (var i = 0; i < 5; i++) Assert.False((await sign.Sign(issued.Token, wrong, "Kevin Claes", true, null, null)).Ok);
        Assert.False((await sign.Sign(issued.Token, c1.Code!, "Kevin Claes", true, null, null)).Ok); // code is vergrendeld

        var c2 = await sign.RequestCode(issued.Token);
        Assert.True((await sign.Sign(issued.Token, c2.Code!, "Kevin Claes", true, null, null)).Ok);
    }

    [Fact]
    public async Task CodeRequests_AreRateLimited()
    {
        var (_, sign, _, co) = Create();
        var issued = (await sign.CreateForChangeOrder(co, Dto(("Kevin Claes", "kevin@example.com")))).Issued.Single();
        for (var i = 0; i < SigningService.MaxCodesPerHour; i++) Assert.True((await sign.RequestCode(issued.Token)).Ok);
        Assert.False((await sign.RequestCode(issued.Token)).Ok);
    }

    [Fact]
    public async Task UnknownOrExpiredToken_IsRejected_AndReissueInvalidatesTheOldLink()
    {
        var (db, sign, _, co) = Create();
        Assert.Null(await sign.GetInfo("x", false));
        Assert.Null(await sign.GetInfo(new string('a', 43), false));

        var issued = (await sign.CreateForChangeOrder(co, Dto(("Kevin Claes", "kevin@example.com")))).Issued.Single();
        var sig = db.DocumentSignatures.Single();
        sig.TokenExpiresOn = DateTime.UtcNow.AddMinutes(-1);
        db.SaveChanges();
        Assert.Equal("expired", (await sign.GetInfo(issued.Token, false))!.State);
        Assert.False((await sign.RequestCode(issued.Token)).Ok);

        var again = await sign.ReissueLink(1, sig.Id);
        Assert.NotNull(again);
        Assert.Null(await sign.GetInfo(issued.Token, false));          // oude link is ongeldig
        Assert.Equal("valid", (await sign.GetInfo(again!.Token, false))!.State);
    }

    [Fact]
    public async Task TwoSigners_DocumentIsSignedOnlyWhenBothSigned_AndEmailsMustDiffer()
    {
        var (db, sign, _, co) = Create();
        Assert.False((await sign.CreateForChangeOrder(co, Dto(("A B", "same@example.com"), ("C D", "SAME@example.com")))).Ok);
        Assert.False((await sign.CreateForChangeOrder(co, Dto(("A B", "geen-mail")))).Ok);

        var res = await sign.CreateForChangeOrder(co, Dto(("Kevin Claes", "kevin@example.com"), ("Sarah Peeters", "sarah@example.com")));
        Assert.True(res.Ok, res.Message);
        var first = res.Issued[0]; var second = res.Issued[1];
        var c1 = await sign.RequestCode(first.Token);
        var r1 = await sign.Sign(first.Token, c1.Code!, "Kevin Claes", true, null, null);
        Assert.True(r1.Ok); Assert.False(r1.AllSigned);
        Assert.Equal(DocumentStatus.TerOndertekening, db.ProjectDocs.Single().Status);
        Assert.Null(db.ChangeOrder.Single().DateAgreement);

        var c2 = await sign.RequestCode(second.Token);
        var r2 = await sign.Sign(second.Token, c2.Code!, "Sarah Peeters", true, null, null);
        Assert.True(r2.AllSigned);
        Assert.Equal(DocumentStatus.Getekend, db.ProjectDocs.Single().Status);
    }

    [Fact]
    public async Task ResendBeforeSigning_ReplacesRevisionAndSigners_ButNotAfterSigning()
    {
        var (db, sign, _, co) = Create();
        var first = (await sign.CreateForChangeOrder(co, Dto(("Kevin Claes", "kevin@example.com")))).Issued.Single();
        var dto2 = Dto(("Kevin Claes", "kevin@example.com")); dto2.StoredFilename = "wo3-v2.pdf";
        var again = await sign.CreateForChangeOrder(co, dto2);
        Assert.True(again.Ok, again.Message);
        Assert.Single(db.ProjectDocs);
        Assert.Equal(2, db.DocumentRevisions.Count());
        Assert.Single(db.DocumentSignatures);
        Assert.Null(await sign.GetInfo(first.Token, false)); // oude link is weg

        var c = await sign.RequestCode(again.Issued[0].Token);
        await sign.Sign(again.Issued[0].Token, c.Code!, "Kevin Claes", true, null, null);
        Assert.False((await sign.CreateForChangeOrder(co, Dto(("Kevin Claes", "kevin@example.com")))).Ok);
    }
}
