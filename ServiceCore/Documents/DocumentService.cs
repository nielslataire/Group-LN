using System.Text.RegularExpressions;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Documents;

/// <summary>Lezen: overzicht, detail, kaart en portaalweergave. Schrijven staat in DocumentService.Write.cs.</summary>
public partial class DocumentService : IDocumentService
{
    private readonly cpmRunningContext _db;

    public DocumentService(cpmRunningContext db) { _db = db; }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    private static readonly System.Globalization.CultureInfo Nl = System.Globalization.CultureInfo.GetCultureInfo("nl-BE");

    private static string NaturalKey(string? s) =>
        Regex.Replace(s ?? "", @"\d+", m => m.Value.PadLeft(8, '0')).ToLowerInvariant();

    // =====================================================================
    // Lezen
    // =====================================================================

    public async Task<List<DocFolderNav>> GetFolders() =>
        await _db.DocumentFolders.AsNoTracking().Where(f => f.IsActive).OrderBy(f => f.SortOrder)
            .Select(f => new DocFolderNav { Id = f.Id, Code = f.Code, Name = f.Name, Icon = f.Icon, ViewKind = f.ViewKind })
            .ToListAsync();

    /// <summary>Documenten die de oude pagina/import zonder map of revisie aanmaakte (of die bij een migratie ontbraken)
    /// krijgen alsnog een map en een revisie A — zo blijft "eerst oud, dan nieuw" altijd consistent.</summary>
    private async Task EnsureLegacyRows(int projectId)
    {
        var loose = await _db.ProjectDocs
            .Where(d => d.ProjectId == projectId && (d.FolderId == null || d.CurrentRevisionId == null))
            .Include(d => d.Revisions).Include(d => d.Links)
            .ToListAsync();
        if (loose.Count == 0) return;

        var folders = await _db.DocumentFolders.AsNoTracking().ToDictionaryAsync(f => f.Code, f => f.Id);
        var now = DateTime.UtcNow;
        foreach (var d in loose)
        {
            d.FolderId ??= folders.TryGetValue(DocumentRules.FolderCodeForLegacyType(d.Type), out var fid) ? fid : folders.GetValueOrDefault("overige");
            // Klantdocument uit de oude pagina → klantkoppeling (anders zou het bij een latere sync zijn klant "verliezen")
            if (d.ClientAccountId.HasValue && !d.Links.Any(l => l.ClientAccountId == d.ClientAccountId))
                d.Links.Add(new DocumentLink { ClientAccountId = d.ClientAccountId, CreatedDate = now });
            if (!d.Revisions.Any())
            {
                d.Revisions.Add(new DocumentRevision
                {
                    Filename = d.Filename,
                    RevisionNo = 1,
                    Status = 2,
                    UploadedByKind = DocumentUploaderKind.Intern,
                    UploadedDate = d.Date.HasValue ? d.Date.Value.ToDateTime(TimeOnly.MinValue) : now,
                    ApprovedDate = d.Date.HasValue ? d.Date.Value.ToDateTime(TimeOnly.MinValue) : now
                });
            }
        }
        await _db.SaveChangesAsync();
        foreach (var d in loose.Where(x => x.CurrentRevisionId == null))
            d.CurrentRevisionId = d.Revisions.OrderByDescending(r => r.RevisionNo).First().Id;
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private IQueryable<ProjectDocs> DocsWithGraph() =>
        _db.ProjectDocs.AsNoTracking().AsSplitQuery()
            .Include(d => d.Folder)
            .Include(d => d.Revisions)
            .Include(d => d.Signatures)
            .Include(d => d.Links).ThenInclude(l => l.Unit)
            .Include(d => d.Links).ThenInclude(l => l.ClientAccount)
            .Include(d => d.Links).ThenInclude(l => l.Company);

    private static DocRevisionDto ToRevDto(DocumentRevision r, int? currentId, decimal? previousAmount) => new()
    {
        Id = r.Id,
        No = r.RevisionNo,
        Label = DocumentRules.RevisionLabel(r.RevisionNo),
        Status = r.Status,
        IsCurrent = r.Id == currentId,
        Note = r.Note,
        Amount = r.Amount,
        AmountDelta = r.Amount.HasValue && previousAmount.HasValue ? r.Amount - previousAmount : null,
        UploadedByName = r.UploadedByName,
        UploadedByKind = r.UploadedByKind,
        UploadedDate = r.UploadedDate,
        ApprovedByName = r.ApprovedByName,
        ApprovedDate = r.ApprovedDate,
        SizeBytes = r.SizeBytes,
        OriginalFilename = r.OriginalFilename
    };

    private static List<DocSignatureDto> ToSigDtos(IEnumerable<DocumentSignature> sigs) =>
        sigs.OrderBy(s => s.SignOrder).Select(s => new DocSignatureDto
        {
            Id = s.Id, Name = s.Name, Initials = DocumentRules.Initials(s.Name), Role = s.Role, Status = s.Status,
            Method = s.Method, SentDate = s.SentDate, OpenedDate = s.OpenedDate, SignedDate = s.SignedDate, ReminderDate = s.ReminderDate
        }).ToList();

    private static List<DocChip> ChipsFor(ProjectDocs d)
    {
        var chips = new List<DocChip> { new() { Type = "project", Label = "Project" } };
        foreach (var l in d.Links.OrderBy(x => x.UnitId.HasValue ? 0 : x.ClientAccountId.HasValue ? 1 : 2).ThenBy(x => NaturalKey(x.Unit?.Name ?? x.ClientAccount?.Name ?? x.Company?.BedrijfsNaam)))
        {
            if (l.UnitId.HasValue) chips.Add(new DocChip { Type = "unit", Id = l.UnitId, Label = l.Unit?.Name ?? "Eenheid", Shared = l.SharedInPortal, LinkId = l.Id });
            else if (l.ClientAccountId.HasValue) chips.Add(new DocChip { Type = "client", Id = l.ClientAccountId, Label = l.ClientAccount?.Name ?? "Klant", Shared = l.SharedInPortal, LinkId = l.Id });
            else if (l.CompanyId.HasValue) chips.Add(new DocChip { Type = "company", Id = l.CompanyId, Label = l.Company?.BedrijfsNaam ?? "Leverancier", Shared = l.SharedInPortal, LinkId = l.Id });
        }
        return chips;
    }

    private DocRow ToDocRow(ProjectDocs d, DateOnly today)
    {
        var cur = d.Revisions.FirstOrDefault(r => r.Id == d.CurrentRevisionId) ?? d.Revisions.OrderByDescending(r => r.RevisionNo).FirstOrDefault();
        var pending = d.Revisions.Count(r => r.Status == 1);
        var sigs = ToSigDtos(d.Signatures);
        var (signed, total) = DocumentRules.SignatureProgress(d.Signatures.Select(s => s.Status));
        var viewKind = d.Folder?.ViewKind ?? "default";

        var statusKey = DocumentRules.StatusKey(d.Status);
        var statusLabel = DocumentRules.StatusLabel(d.Status);
        if (d.Status == DocumentStatus.TerOndertekening && total > 0)
        {
            statusLabel = $"{signed} / {total} getekend";
        }
        else if (d.Status is DocumentStatus.Goedgekeurd or DocumentStatus.Getekend)
        {
            var ex = DocumentRules.ExpiryState(d.ExpiresOn, today);
            if (ex == "expired") { statusKey = "expired"; statusLabel = "Vervallen"; }
            else if (ex == "expiring") { statusKey = "expiring"; statusLabel = "Vervalt " + d.ExpiresOn!.Value.ToString("dd/MM", Nl); }
            else if (ex == "ok" && d.Status == DocumentStatus.Goedgekeurd && viewKind == "keuringen") { statusLabel = "Geldig"; }
        }

        var shareClient = d.ShareAllBuyers || d.Links.Any(l => (l.UnitId.HasValue || l.ClientAccountId.HasValue) && l.SharedInPortal);
        var shareSupplier = d.Links.Any(l => l.CompanyId.HasValue && l.SharedInPortal);
        var audiences = new List<string> { "intern" };
        if (shareClient) audiences.Add("klant");
        if (shareSupplier) audiences.Add("leverancier");

        var scopes = new List<string>();
        if (!d.Links.Any()) scopes.Add("project");
        if (d.Links.Any(l => l.UnitId.HasValue)) scopes.Add("units");
        if (d.Links.Any(l => l.ClientAccountId.HasValue)) scopes.Add("clients");
        if (d.Links.Any(l => l.CompanyId.HasValue)) scopes.Add("suppliers");

        var subParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(d.AuthoredBy)) subParts.Add(d.AuthoredBy!);
        if (cur?.UploadedByKind == DocumentUploaderKind.Leverancier) subParts.Add("ingediend via portaal");
        else if (cur?.UploadedByKind == DocumentUploaderKind.Klant) subParts.Add("aangeleverd via portaal");
        var size = DocumentRules.SizeText(cur?.SizeBytes);
        if (size != "") subParts.Add(size);

        var supplier = d.Links.FirstOrDefault(l => l.CompanyId.HasValue)?.Company?.BedrijfsNaam;

        return new DocRow
        {
            Kind = "doc",
            Id = d.Id,
            Name = d.Name ?? d.Filename,
            SubLine = subParts.Count == 0 ? null : string.Join(" · ", subParts),
            FolderCode = d.Folder?.Code ?? "overige",
            FolderName = d.Folder?.Name ?? "Overige",
            Ext = DocumentRules.Ext(cur?.OriginalFilename ?? cur?.Filename ?? d.Filename),
            Chips = ChipsFor(d),
            RevLabel = cur == null ? "—" : (d.Revisions.Count <= 1 && cur.RevisionNo == 1 && viewKind is "contracten" ? "—" : DocumentRules.RevisionLabel(cur.RevisionNo)),
            PendingRevisions = pending,
            RevisionId = cur?.Id,
            StatusKey = statusKey,
            StatusLabel = statusLabel,
            ExpiresOn = d.ExpiresOn,
            UpdatedDate = d.ModifiedDate ?? cur?.UploadedDate ?? d.CreatedDate ?? d.Date?.ToDateTime(TimeOnly.MinValue),
            ShareClient = shareClient,
            ShareSupplier = shareSupplier,
            Amount = cur?.Amount,
            Perceel = d.Perceel,
            Responsible = supplier,
            SignedCount = signed,
            SignTotal = total,
            Signatures = sigs,
            IsDraft = d.Status == DocumentStatus.Concept,
            Audiences = string.Join(' ', audiences),
            Scopes = string.Join(' ', scopes),
            SearchText = string.Join(' ', new[] { d.Name, d.DocumentNumber, d.AuthoredBy, d.Perceel, d.Folder?.Name, string.Join(' ', d.Links.Select(l => l.Unit?.Name ?? l.ClientAccount?.Name ?? l.Company?.BedrijfsNaam)), string.Join(' ', d.Revisions.Select(r => r.Note)) }.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant()
        };
    }

    private static DocRow ToRequestRow(DocumentRequest r)
    {
        var (key, label) = r.Status switch
        {
            DocumentRequestStatus.Aangevraagd => ("requested", "Aangevraagd"),
            DocumentRequestStatus.NietVereist => ("notrequired", "Niet vereist"),
            DocumentRequestStatus.Ontvangen => ("ok", "Ontvangen"),
            _ => ("missing", "Ontbreekt")
        };
        var who = r.ResponsibleName ?? r.ResponsibleCompany?.BedrijfsNaam ?? r.ResponsibleClientAccount?.Name ?? r.ResponsibleRole;
        var chips = new List<DocChip> { new() { Type = "project", Label = "Project" } };
        if (r.UnitId.HasValue) chips.Add(new DocChip { Type = "unit", Id = r.UnitId, Label = r.Unit?.Name ?? "Eenheid" });
        if (r.ResponsibleCompanyId.HasValue) chips.Add(new DocChip { Type = "company", Id = r.ResponsibleCompanyId, Label = r.ResponsibleCompany?.BedrijfsNaam ?? "Leverancier" });
        if (r.ResponsibleClientAccountId.HasValue) chips.Add(new DocChip { Type = "client", Id = r.ResponsibleClientAccountId, Label = r.ResponsibleClientAccount?.Name ?? "Klant" });
        var scopes = new List<string>();
        if (!r.UnitId.HasValue && !r.ResponsibleCompanyId.HasValue && !r.ResponsibleClientAccountId.HasValue) scopes.Add("project");
        if (r.UnitId.HasValue) scopes.Add("units");
        if (r.ResponsibleClientAccountId.HasValue) scopes.Add("clients");
        if (r.ResponsibleCompanyId.HasValue) scopes.Add("suppliers");
        var due = r.DueDate.HasValue ? "tegen " + r.DueDate.Value.ToString("dd/MM/yyyy", Nl) : r.DueLabel;
        return new DocRow
        {
            Kind = "request",
            Id = r.Id,
            Name = r.Name,
            SubLine = string.Join(" · ", new[] { due, who != null ? "verantwoordelijke " + who : null }.Where(x => !string.IsNullOrWhiteSpace(x))),
            FolderCode = r.Folder?.Code ?? "overige",
            FolderName = r.Folder?.Name ?? "Overige",
            Chips = chips,
            StatusKey = key,
            StatusLabel = label,
            IsMissing = r.Status is DocumentRequestStatus.Ontbreekt or DocumentRequestStatus.Aangevraagd,
            Perceel = r.Perceel,
            Responsible = who,
            DueText = due,
            UnitId = r.UnitId,
            ClientAccountId = r.ResponsibleClientAccountId,
            CompanyId = r.ResponsibleCompanyId,
            Audiences = r.ResponsibleKind == 1 ? "intern leverancier" : "intern",
            Scopes = string.Join(' ', scopes),
            SearchText = string.Join(' ', new[] { r.Name, r.Perceel, who, r.Unit?.Name, r.Folder?.Name }.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant()
        };
    }

    public async Task<DocOverview> GetOverview(int projectId, DocFilter f)
    {
        await EnsureLegacyRows(projectId);
        var today = Today;

        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => new { p.ProjectName }).FirstOrDefaultAsync();
        var folders = await _db.DocumentFolders.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync();
        var docs = await DocsWithGraph().Where(d => d.ProjectId == projectId).ToListAsync();
        var reqs = await _db.DocumentRequests.AsNoTracking()
            .Include(r => r.Unit).Include(r => r.ResponsibleCompany).Include(r => r.ResponsibleClientAccount).Include(r => r.Folder)
            .Where(r => r.ProjectId == projectId && r.Status != DocumentRequestStatus.Ontvangen)
            .ToListAsync();

        var buyerByUnit = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId && u.ClientAccountId != null)
            .Select(u => new { u.Id, u.ClientAccountId }).ToDictionaryAsync(x => x.Id, x => x.ClientAccountId!.Value);

        bool MatchesEntity(DocRow row, ProjectDocs? d)
        {
            if (f.UnitId.HasValue && !(row.Chips.Any(c => c.Type == "unit" && c.Id == f.UnitId))) return false;
            if (f.CompanyId.HasValue && !(row.Chips.Any(c => c.Type == "company" && c.Id == f.CompanyId))) return false;
            if (f.ClientAccountId.HasValue && !(row.Chips.Any(c => c.Type == "client" && c.Id == f.ClientAccountId)
                    || row.Chips.Any(c => c.Type == "unit" && c.Id.HasValue && buyerByUnit.TryGetValue(c.Id.Value, out var b) && b == f.ClientAccountId))) return false;
            return true;
        }

        var docRows = docs.Select(d => (Row: ToDocRow(d, today), Doc: d)).Where(x => MatchesEntity(x.Row, x.Doc)).ToList();
        var reqRows = reqs.Select(r => ToRequestRow(r)).Where(r => MatchesEntity(r, null)).ToList();
        var all = docRows.Select(x => x.Row).Concat(reqRows).ToList();

        var vo = new DocOverview
        {
            ProjectId = projectId,
            ProjectName = project?.ProjectName ?? "",
            Total = docRows.Count,
            ActiveFolder = f.Folder,
            ActiveSmart = f.Smart,
            HasTemplates = await _db.DocumentTemplates.AsNoTracking().AnyAsync(t => t.IsActive)
        };

        foreach (var fo in folders)
        {
            vo.Folders.Add(new DocFolderNav
            {
                Id = fo.Id, Code = fo.Code, Name = fo.Name, Icon = fo.Icon, ViewKind = fo.ViewKind,
                Count = docRows.Count(x => x.Row.FolderCode == fo.Code),
                Missing = reqRows.Count(r => r.FolderCode == fo.Code && r.IsMissing)
            });
        }
        // "Overige" enkel tonen als er iets in zit
        vo.Folders.RemoveAll(x => x.Code == "overige" && x.Count == 0 && x.Missing == 0 && f.Folder != "overige");

        bool IsPending(DocRow r) => r.Kind == "doc" && (r.PendingRevisions > 0 || r.StatusKey == "pending");
        bool IsExpiring(DocRow r) => r.Kind == "doc" && r.StatusKey is "expiring" or "expired";
        bool IsShared(DocRow r) => r.Kind == "doc" && (r.ShareClient || r.ShareSupplier);
        bool IsMissingRow(DocRow r) => r.Kind == "request" && r.IsMissing;

        vo.Smart.Add(new DocSmartNav { Key = "goedkeuring", Label = "Ter goedkeuring", Count = all.Count(IsPending), Tone = "attention" });
        vo.Smart.Add(new DocSmartNav { Key = "vervalt", Label = "Vervalt binnen 90 d", Count = all.Count(IsExpiring), Tone = "attention" });
        vo.Smart.Add(new DocSmartNav { Key = "ontbreekt", Label = "Ontbreekt", Count = all.Count(IsMissingRow), Tone = "danger" });
        vo.Smart.Add(new DocSmartNav { Key = "gedeeld", Label = "Gedeeld in portaal", Count = all.Count(IsShared), Tone = "neutral" });
        vo.SharedCount = all.Count(IsShared);

        var active = vo.Folders.FirstOrDefault(x => x.Code == f.Folder);
        vo.ActiveFolderNav = active;
        vo.ViewKind = active?.ViewKind ?? "default";

        IEnumerable<DocRow> rows = all;
        if (active != null) rows = rows.Where(r => r.FolderCode == active.Code);
        rows = f.Smart switch
        {
            "goedkeuring" => rows.Where(IsPending),
            "vervalt" => rows.Where(IsExpiring),
            "ontbreekt" => rows.Where(IsMissingRow),
            "gedeeld" => rows.Where(IsShared),
            _ => rows
        };
        var rowList = rows.ToList();
        vo.RowCount = rowList.Count;
        vo.Scope = new DocScopeCounts
        {
            All = rowList.Count,
            Project = rowList.Count(r => r.Scopes.Contains("project")),
            Units = rowList.Count(r => r.Scopes.Contains("units")),
            Clients = rowList.Count(r => r.Scopes.Contains("clients")),
            Suppliers = rowList.Count(r => r.Scopes.Contains("suppliers"))
        };

        // ---- Opties voor filters/modals ----
        var units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId && !u.IsLink).Select(u => new { u.Id, u.Name, u.ClientAccountId, BuyerName = u.ClientAccount != null ? u.ClientAccount.Name : null }).ToListAsync();
        vo.Units = units.OrderBy(u => NaturalKey(u.Name)).Select(u => new DocOption { Id = u.Id, Name = u.Name ?? "Eenheid", Sub = u.BuyerName, Ref = u.ClientAccountId }).ToList();
        var clientIds = units.Where(u => u.ClientAccountId.HasValue).Select(u => u.ClientAccountId!.Value)
            .Concat(docs.SelectMany(d => d.Links).Where(l => l.ClientAccountId.HasValue).Select(l => l.ClientAccountId!.Value)).Distinct().ToList();
        vo.Clients = (await _db.ClientAccount.AsNoTracking().Where(c => clientIds.Contains(c.Id)).Select(c => new { c.Id, c.Name }).ToListAsync())
            .OrderBy(c => c.Name).Select(c => new DocOption { Id = c.Id, Name = c.Name ?? "Klant" }).ToList();
        var companyIds = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId).Select(c => c.CompanyId).Distinct().ToListAsync();
        companyIds = companyIds.Concat(docs.SelectMany(d => d.Links).Where(l => l.CompanyId.HasValue).Select(l => l.CompanyId!.Value)).Distinct().ToList();
        vo.Companies = (await _db.CompanyInfo.AsNoTracking().Where(c => companyIds.Contains(c.CompanyId)).Select(c => new { c.CompanyId, c.BedrijfsNaam }).ToListAsync())
            .OrderBy(c => c.BedrijfsNaam).Select(c => new DocOption { Id = c.CompanyId, Name = c.BedrijfsNaam ?? "Leverancier" }).ToList();
        vo.Percelen = all.Select(r => r.Perceel).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!).Distinct().OrderBy(p => p).ToList();

        // ---- Groepering ----
        vo.Groups = BuildGroups(vo.ViewKind, active != null, rowList, folders, units.ToDictionary(u => u.Id, u => (u.Name ?? "", u.BuyerName)), docs);
        return vo;
    }

    private static void SortRows(List<DocRow> rows)
    {
        var sorted = rows
            .OrderBy(r => r.Kind == "request" ? 1 : 0)
            .ThenByDescending(r => r.UpdatedDate ?? DateTime.MinValue)
            .ThenBy(r => r.Name)
            .ToList();
        rows.Clear();
        rows.AddRange(sorted);
    }

    private static void Coverage(DocGroup g)
    {
        var relevant = g.Rows.ToList();
        g.CoverageTotal = relevant.Count;
        g.Covered = relevant.Count(r => r.Kind == "doc"
            ? r.StatusKey is "ok" or "signed" or "expiring"
            : r.StatusKey == "notrequired");
        g.MissingCount = relevant.Count(r => r.IsMissing);
    }

    private List<DocGroup> BuildGroups(string viewKind, bool folderSelected, List<DocRow> rows, List<DocumentFolder> folders,
        Dictionary<int, (string Name, string? Buyer)> unitInfo, List<ProjectDocs> docs)
    {
        var groups = new List<DocGroup>();

        if (viewKind == "keuringen" && folderSelected)
        {
            var byUnit = new Dictionary<int, DocGroup>();
            var projectGroup = new DocGroup { Key = "project", Kind = "project", Label = "Project", Sub = "geldt voor alle eenheden", ShowCoverage = true };
            foreach (var r in rows)
            {
                var unitIds = r.Chips.Where(c => c.Type == "unit" && c.Id.HasValue).Select(c => c.Id!.Value).Distinct().ToList();
                if (unitIds.Count == 0) { projectGroup.Rows.Add(r); continue; }
                foreach (var uid in unitIds)
                {
                    if (!byUnit.TryGetValue(uid, out var g))
                    {
                        var info = unitInfo.TryGetValue(uid, out var i) ? i : ("Eenheid", (string?)null);
                        g = new DocGroup { Key = "unit-" + uid, Kind = "unit", Label = info.Item1, Sub = info.Item2 != null ? info.Item2 : "nog niet verkocht", ShowCoverage = true };
                        byUnit[uid] = g;
                    }
                    g.Rows.Add(r);
                }
            }
            groups.AddRange(byUnit.Values.OrderBy(g => NaturalKey(g.Label)));
            if (projectGroup.Rows.Count > 0) groups.Add(projectGroup);
        }
        else if (viewKind == "contracten" && folderSelected)
        {
            var buyerOfUnit = unitInfo; // eenheid → koper (naam)
            var byClient = new Dictionary<string, DocGroup>();
            foreach (var r in rows)
            {
                var clientChips = r.Chips.Where(c => c.Type == "client").ToList();
                string key, label; string? sub = null;
                if (clientChips.Count > 0) { key = "client-" + clientChips[0].Id; label = clientChips[0].Label; }
                else
                {
                    var uc = r.Chips.FirstOrDefault(c => c.Type == "unit" && c.Id.HasValue && buyerOfUnit.TryGetValue(c.Id.Value, out var i) && i.Buyer != null);
                    if (uc != null) { key = "buyer-" + buyerOfUnit[uc.Id!.Value].Buyer; label = buyerOfUnit[uc.Id!.Value].Buyer!; }
                    else { key = "none"; label = "Zonder klant"; sub = "niet aan een klant gekoppeld"; }
                }
                if (!byClient.TryGetValue(key, out var g)) { g = new DocGroup { Key = key, Kind = "client", Label = label, Sub = sub }; byClient[key] = g; }
                g.Rows.Add(r);
            }
            foreach (var g in byClient.Values)
            {
                var units = g.Rows.SelectMany(r => r.Chips.Where(c => c.Type == "unit").Select(c => c.Label)).Distinct().OrderBy(NaturalKey).ToList();
                if (g.Sub == null) g.Sub = units.Count > 0 ? "koper " + string.Join(", ", units) : "kandidaat-koper";
            }
            groups.AddRange(byClient.Values.OrderBy(g => g.Key == "none" ? 1 : 0).ThenBy(g => g.Label));
        }
        else if (viewKind == "offertes" && folderSelected)
        {
            var byCompany = new Dictionary<string, DocGroup>();
            foreach (var r in rows)
            {
                var c = r.Chips.FirstOrDefault(x => x.Type == "company");
                var key = c != null ? "company-" + c.Id : "none";
                if (!byCompany.TryGetValue(key, out var g))
                {
                    g = new DocGroup { Key = key, Kind = "company", Label = c?.Label ?? "Zonder leverancier" };
                    byCompany[key] = g;
                }
                g.Rows.Add(r);
            }
            foreach (var g in byCompany.Values)
            {
                var percelen = g.Rows.Select(r => r.Perceel).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
                var offers = g.Rows.Count(r => r.Kind == "doc" && r.StatusKey is "submitted" or "awarded" or "notawarded" or "concept");
                g.Sub = string.Join(" · ", new[] { percelen.Count > 0 ? string.Join(", ", percelen) : null, offers > 0 ? $"{offers} {(offers == 1 ? "offerte" : "offertes")}" : null }.Where(x => x != null));
            }
            groups.AddRange(byCompany.Values.OrderBy(g => g.Key == "none" ? 1 : 0).ThenBy(g => g.Label));
        }
        else if (folderSelected)
        {
            groups.Add(new DocGroup { Key = "flat", Kind = "flat", Label = "" }); // één groep zonder kop
            groups[0].Rows.AddRange(rows);
        }
        else
        {
            foreach (var fo in folders)
            {
                var fr = rows.Where(r => r.FolderCode == fo.Code).ToList();
                if (fr.Count == 0) continue;
                groups.Add(new DocGroup
                {
                    Key = "folder-" + fo.Code, Kind = "folder", Label = fo.Name, Icon = fo.Icon,
                    Sub = null, Rows = fr, ShowCoverage = false,
                    MissingCount = fr.Count(r => r.IsMissing)
                });
            }
        }

        foreach (var g in groups)
        {
            SortRows(g.Rows);
            if (g.ShowCoverage) Coverage(g);
        }
        return groups;
    }

    // ---------------------------------------------------------------------

    public async Task<DocDetail?> GetDetail(int projectId, int documentId)
    {
        var d = await DocsWithGraph().FirstOrDefaultAsync(x => x.Id == documentId && x.ProjectId == projectId);
        if (d == null) return null;
        var today = Today;
        var row = ToDocRow(d, today);
        var viewKind = d.Folder?.ViewKind ?? "default";
        var revs = d.Revisions.OrderBy(r => r.RevisionNo).ToList();
        var dtos = new List<DocRevisionDto>();
        decimal? prev = null;
        foreach (var r in revs)
        {
            dtos.Add(ToRevDto(r, d.CurrentRevisionId, prev));
            if (r.Amount.HasValue) prev = r.Amount;
        }
        dtos.Reverse();

        var previewRev = revs.FirstOrDefault(r => r.Id == d.CurrentRevisionId) ?? revs.LastOrDefault();
        var pending = revs.LastOrDefault(r => r.Status == 1);
        var cur = revs.FirstOrDefault(r => r.Id == d.CurrentRevisionId);

        var detail = new DocDetail
        {
            ProjectId = projectId,
            Id = d.Id,
            Name = d.Name ?? d.Filename,
            Ext = row.Ext,
            FolderCode = d.Folder?.Code ?? "overige",
            FolderName = d.Folder?.Name ?? "Overige",
            FolderId = d.FolderId ?? 0,
            ViewKind = viewKind,
            SubLine = row.SubLine,
            StatusKey = row.StatusKey,
            StatusLabel = row.StatusLabel,
            Status = d.Status,
            DocumentNumber = d.DocumentNumber,
            AuthoredBy = d.AuthoredBy,
            DocumentDate = d.Date,
            ExpiresOn = d.ExpiresOn,
            Perceel = d.Perceel,
            ShareAllBuyers = d.ShareAllBuyers,
            IsFrozen = DocumentRules.IsFrozen(d.Status, d.Signatures.Select(s => s.Status)),
            CurrentRevisionId = d.CurrentRevisionId,
            PreviewRevisionId = previewRev?.Id,
            PreviewLabel = previewRev == null ? null : DocumentRules.RevisionLabel(previewRev.RevisionNo),
            Chips = row.Chips,
            Revisions = dtos,
            PendingRevisionId = pending?.Id ?? 0,
            Signatures = row.Signatures,
            ShareClientOn = row.ShareClient,
            HasClientAudience = d.Links.Any(l => l.UnitId.HasValue || l.ClientAccountId.HasValue) || d.ShareAllBuyers || d.Links.Count == 0,
            CurrentAmount = cur?.Amount ?? previewRev?.Amount,
            SupplierName = d.Links.FirstOrDefault(l => l.CompanyId.HasValue)?.Company?.BedrijfsNaam
        };

        if (viewKind == "offertes" && !string.IsNullOrWhiteSpace(d.Perceel))
        {
            var others = await DocsWithGraph()
                .Where(x => x.ProjectId == projectId && x.Perceel == d.Perceel && x.Folder!.ViewKind == "offertes"
                            && (x.Status == DocumentStatus.Ingediend || x.Status == DocumentStatus.Gegund || x.Status == DocumentStatus.NietGegund))
                .ToListAsync();
            foreach (var o in others)
            {
                var oc = o.Revisions.FirstOrDefault(r => r.Id == o.CurrentRevisionId) ?? o.Revisions.OrderByDescending(r => r.RevisionNo).FirstOrDefault();
                if (oc?.Amount == null) continue;
                detail.Offers.Add(new DocOfferCompare
                {
                    DocumentId = o.Id,
                    Company = o.Links.FirstOrDefault(l => l.CompanyId.HasValue)?.Company?.BedrijfsNaam ?? o.Name ?? "Offerte",
                    Amount = oc.Amount.Value,
                    IsThis = o.Id == d.Id,
                    Status = o.Status
                });
            }
            detail.Offers = detail.Offers.OrderBy(o => o.Amount).ToList();
        }

        if (viewKind == "contracten" && d.Status != DocumentStatus.Getekend)
        {
            var unitLinks = d.Links.Where(l => l.UnitId.HasValue).ToList();
            if (unitLinks.Count == 1)
                detail.UnitEffectHint = $"Zodra alle partijen tekenden, gaat {unitLinks[0].Unit?.Name ?? "de eenheid"} naar Verkocht.";
        }
        return detail;
    }

    public async Task<DocRequestDetail?> GetRequestDetail(int projectId, int requestId)
    {
        var r = await _db.DocumentRequests.AsNoTracking()
            .Include(x => x.Folder).Include(x => x.Unit).ThenInclude(u => u.ClientAccount)
            .Include(x => x.ResponsibleCompany).Include(x => x.ResponsibleClientAccount)
            .FirstOrDefaultAsync(x => x.Id == requestId && x.ProjectId == projectId);
        if (r == null) return null;
        var (key, label) = r.Status switch
        {
            DocumentRequestStatus.Aangevraagd => ("requested", "Aangevraagd"),
            DocumentRequestStatus.NietVereist => ("notrequired", "Niet vereist"),
            DocumentRequestStatus.Ontvangen => ("ok", "Ontvangen"),
            _ => ("missing", "Ontbreekt")
        };
        return new DocRequestDetail
        {
            ProjectId = projectId, Id = r.Id, Name = r.Name, FolderId = r.FolderId, FolderName = r.Folder?.Name ?? "",
            UnitId = r.UnitId, UnitLabel = r.Unit?.Name, StatusKey = key, StatusLabel = label, Status = r.Status,
            ResponsibleName = r.ResponsibleName ?? r.ResponsibleCompany?.BedrijfsNaam ?? r.ResponsibleClientAccount?.Name,
            ResponsibleEmail = r.ResponsibleCompany?.Email ?? r.ResponsibleClientAccount?.Email,
            ResponsibleKind = r.ResponsibleKind switch { 1 => "leverancier", 2 => "klant", 3 => "extern", _ => "intern" },
            ResponsibleCompanyId = r.ResponsibleCompanyId, ResponsibleClientAccountId = r.ResponsibleClientAccountId, ShareAfter = r.ShareWithBuyerAfterApproval,
            RequestedDate = r.RequestedDate, RequestedByName = r.RequestedByName, DueDate = r.DueDate, DueLabel = r.DueLabel,
            ReminderDaysBefore = r.ReminderDaysBefore, ExpiryYears = r.ExpiryYears,
            SeenInPortalDate = r.SeenInPortalDate, SeenByName = r.SeenByName, LastReminderDate = r.LastReminderDate,
            BuyerName = r.Unit?.ClientAccount?.Name, Note = r.Note, Perceel = r.Perceel, FulfilledDocumentId = r.FulfilledDocumentId
        };
    }

    public async Task<DocRevisionFile?> GetRevisionFile(int projectId, int revisionId) =>
        await _db.DocumentRevisions.AsNoTracking()
            .Where(r => r.Id == revisionId && r.Document.ProjectId == projectId)
            .Select(r => new DocRevisionFile { RevisionId = r.Id, DocumentId = r.DocumentId, Filename = r.Filename, OriginalFilename = r.OriginalFilename })
            .FirstOrDefaultAsync();

    // =====================================================================
    // Herbruikbare kaart (project / eenheid / klant / leverancier)
    // =====================================================================

    public async Task<DocCard> GetCard(DocCardScope s)
    {
        var card = new DocCard();
        var today = Today;
        // Eenheden die deze klant kocht (voor "via eenheid") en de koper/het project van een eenheid (voor "via klant/project")
        var clientUnitIds = new List<int>();
        int? unitBuyer = null; int? unitProject = null;
        if (s.ClientAccountId.HasValue)
            clientUnitIds = await _db.Units.AsNoTracking().Where(u => u.ClientAccountId == s.ClientAccountId && (s.ProjectId == 0 || u.ProjectId == s.ProjectId)).Select(u => u.Id).ToListAsync();
        if (s.UnitId.HasValue)
        {
            var u = await _db.Units.AsNoTracking().Where(x => x.Id == s.UnitId).Select(x => new { x.ClientAccountId, x.ProjectId }).FirstOrDefaultAsync();
            unitBuyer = u?.ClientAccountId; unitProject = u?.ProjectId;
        }

        var q = DocsWithGraph().AsQueryable();
        if (s.ProjectId > 0) q = q.Where(d => d.ProjectId == s.ProjectId);
        var docs = await q.Where(d =>
                d.Links.Any(l =>
                    (s.UnitId != null && l.UnitId == s.UnitId)
                    || (s.CompanyId != null && l.CompanyId == s.CompanyId)
                    || (s.ClientAccountId != null && (l.ClientAccountId == s.ClientAccountId || (l.UnitId != null && clientUnitIds.Contains(l.UnitId.Value))))
                    || (unitBuyer != null && l.ClientAccountId == unitBuyer))
                || (s.UnitId != null && !d.Links.Any() && d.ProjectId == unitProject))
            .ToListAsync();

        var projectNames = await _db.Project.AsNoTracking().Select(p => new { p.ProjectId, p.ProjectName }).ToDictionaryAsync(p => p.ProjectId, p => p.ProjectName);
        string PName(int id) => projectNames.TryGetValue(id, out var n) ? n ?? "" : "";

        foreach (var d in docs)
        {
            var row = ToDocRow(d, today);
            bool direct = false; string? via = null;
            if (s.UnitId.HasValue)
            {
                if (d.Links.Any(l => l.UnitId == s.UnitId)) direct = true;
                else if (d.ProjectId == unitProject && !d.Links.Any() && d.Folder?.Code is "verkoop" or "keuringen" && d.Status is DocumentStatus.Goedgekeurd or DocumentStatus.Getekend) { via = "project"; }
                else if (unitBuyer.HasValue && d.Links.Any(l => l.ClientAccountId == unitBuyer)) { via = "klant"; }
                else continue;
            }
            else if (s.ClientAccountId.HasValue)
            {
                if (d.Links.Any(l => l.ClientAccountId == s.ClientAccountId)) direct = true;
                else if (d.Links.Any(l => l.UnitId.HasValue && clientUnitIds.Contains(l.UnitId.Value))) via = "eenheid";
                else continue;
            }
            else if (s.CompanyId.HasValue)
            {
                if (d.Links.Any(l => l.CompanyId == s.CompanyId)) direct = true; else continue;
            }
            else continue;

            card.Items.Add(new DocCardItem
            {
                Id = d.Id, ProjectId = d.ProjectId, ProjectName = PName(d.ProjectId), Name = row.Name, Ext = row.Ext,
                SubLine = row.SubLine == null ? row.FolderName : row.FolderName + " · " + row.SubLine,
                StatusKey = row.StatusKey, StatusLabel = row.StatusLabel, Direct = direct, ViaLabel = via
            });
        }

        // Verwachte documenten
        var rq = _db.DocumentRequests.AsNoTracking().Include(r => r.Folder).Include(r => r.Unit)
            .Where(r => r.Status == DocumentRequestStatus.Ontbreekt || r.Status == DocumentRequestStatus.Aangevraagd);
        if (s.ProjectId > 0) rq = rq.Where(r => r.ProjectId == s.ProjectId);
        if (s.UnitId.HasValue) rq = rq.Where(r => r.UnitId == s.UnitId);
        else if (s.CompanyId.HasValue) rq = rq.Where(r => r.ResponsibleCompanyId == s.CompanyId);
        else if (s.ClientAccountId.HasValue) rq = rq.Where(r => r.ResponsibleClientAccountId == s.ClientAccountId);
        foreach (var r in await rq.ToListAsync())
        {
            var rr = ToRequestRow(r);
            card.Items.Add(new DocCardItem
            {
                Kind = "request", Id = r.Id, ProjectId = r.ProjectId, ProjectName = PName(r.ProjectId), Name = r.Name,
                SubLine = r.Folder?.Name + (rr.SubLine != null ? " · " + rr.SubLine : ""), StatusKey = rr.StatusKey, StatusLabel = rr.StatusLabel, Direct = true
            });
        }

        card.Items = card.Items.OrderByDescending(i => i.Direct).ThenBy(i => i.Kind == "request" ? 0 : 1).ThenBy(i => i.Name).ToList();
        card.DirectCount = card.Items.Count(i => i.Direct);
        card.ViaCount = card.Items.Count(i => !i.Direct);
        card.ShowProjectColumn = card.Items.Select(i => i.ProjectId).Distinct().Count() > 1 || (s.ProjectId == 0 && card.Items.Count > 0);
        return card;
    }

    // =====================================================================
    // Portaalweergave (voor het latere klant-/leveranciersportaal)
    // =====================================================================

    public async Task<DocPortalView> GetPortalDocuments(DocPortalAudience a)
    {
        var view = new DocPortalView();
        var projectNames = await _db.Project.AsNoTracking().Select(p => new { p.ProjectId, p.ProjectName }).ToDictionaryAsync(p => p.ProjectId, p => p.ProjectName);
        string PName(int id) => projectNames.TryGetValue(id, out var n) ? n ?? "" : "";
        var now = DateTime.UtcNow;

        if (a.Kind == "klant" && a.ClientAccountId.HasValue)
        {
            var cid = a.ClientAccountId.Value;
            var myUnits = await _db.Units.AsNoTracking().Where(u => u.ClientAccountId == cid).Select(u => new { u.Id, u.ProjectId }).ToListAsync();
            var myUnitIds = myUnits.Select(u => u.Id).ToList();
            var myProjects = myUnits.Select(u => u.ProjectId).Distinct().ToList();
            var docs = await _db.ProjectDocs.AsNoTracking().AsSplitQuery().Include(d => d.Links).Include(d => d.Revisions)
                .Where(d => (d.Status == DocumentStatus.Goedgekeurd || d.Status == DocumentStatus.Getekend) && d.CurrentRevisionId != null &&
                    (d.Links.Any(l => l.SharedInPortal && (l.ClientAccountId == cid || (l.UnitId != null && myUnitIds.Contains(l.UnitId.Value))))
                     || (d.ShareAllBuyers && myProjects.Contains(d.ProjectId) && !d.Links.Any())))
                .ToListAsync();
            foreach (var d in docs)
            {
                var rev = d.Revisions.First(r => r.Id == d.CurrentRevisionId);
                var mine = d.Links.Any(l => l.ClientAccountId == cid || (l.UnitId != null && myUnitIds.Contains(l.UnitId.Value)));
                view.Items.Add(new DocPortalItem
                {
                    DocumentId = d.Id, RevisionId = rev.Id, ProjectId = d.ProjectId, ProjectName = PName(d.ProjectId), Name = d.Name ?? d.Filename,
                    Group = mine ? "Mijn woning" : "Het project", Date = rev.ApprovedDate ?? rev.UploadedDate,
                    IsSigned = d.Status == DocumentStatus.Getekend, IsNew = (rev.ApprovedDate ?? rev.UploadedDate) > now.AddDays(-14), Filename = rev.Filename
                });
            }
        }
        else if (a.Kind == "leverancier" && a.CompanyId.HasValue)
        {
            var coid = a.CompanyId.Value;
            var docs = await _db.ProjectDocs.AsNoTracking().AsSplitQuery().Include(d => d.Links).Include(d => d.Revisions)
                .Where(d => d.CurrentRevisionId != null && d.Status != DocumentStatus.Concept && d.Links.Any(l => l.SharedInPortal && l.CompanyId == coid))
                .ToListAsync();
            foreach (var d in docs)
            {
                var rev = d.Revisions.First(r => r.Id == d.CurrentRevisionId);
                view.Items.Add(new DocPortalItem
                {
                    DocumentId = d.Id, RevisionId = rev.Id, ProjectId = d.ProjectId, ProjectName = PName(d.ProjectId), Name = d.Name ?? d.Filename,
                    Group = PName(d.ProjectId), RevisionLabel = DocumentRules.RevisionLabel(rev.RevisionNo), Date = rev.ApprovedDate ?? rev.UploadedDate,
                    IsSigned = d.Status == DocumentStatus.Getekend, IsNew = (rev.ApprovedDate ?? rev.UploadedDate) > now.AddDays(-14), Filename = rev.Filename
                });
            }
            var reqs = await _db.DocumentRequests.AsNoTracking().Include(r => r.Unit)
                .Where(r => r.ResponsibleCompanyId == coid && r.Status == DocumentRequestStatus.Aangevraagd).ToListAsync();
            view.Requests = reqs.Select(r => new DocPortalRequestItem
            {
                RequestId = r.Id, ProjectId = r.ProjectId, ProjectName = PName(r.ProjectId), Name = r.Name, DueDate = r.DueDate, UnitLabel = r.Unit?.Name
            }).ToList();
        }
        view.Items = view.Items.OrderBy(i => i.Group).ThenByDescending(i => i.Date).ToList();
        return view;
    }
}
