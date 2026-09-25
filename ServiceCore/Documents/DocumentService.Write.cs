using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Documents;

/// <summary>Schrijven: uploads/revisies, koppelingen, delen, aanvragen, ondertekening en gunning.</summary>
public partial class DocumentService
{
    private async Task<int> FolderIdByCode(string code) =>
        await _db.DocumentFolders.Where(f => f.Code == code).Select(f => f.Id).FirstOrDefaultAsync();

    private static bool HasClientAudienceLink(ProjectDocs d) => d.Links.Any(l => l.UnitId.HasValue || l.ClientAccountId.HasValue);

    /// <summary>Houdt de oude kolommen in sync die de publieke site en oude pagina's lezen (zie DocumentRules.LegacyTypeAfterChange):
    /// ClientAccountId = eerste klantkoppeling (een klantdocument mag NOOIT op de website belanden); Type enkel voor een
    /// goedgekeurd, ongekoppeld verkoopdocument.</summary>
    private static void SyncLegacyColumns(ProjectDocs d, string folderCode)
    {
        var clientLink = d.Links.FirstOrDefault(l => l.ClientAccountId.HasValue);
        if (clientLink != null) d.ClientAccountId = clientLink.ClientAccountId;
        d.Type = DocumentRules.LegacyTypeAfterChange(d.Type, folderCode, d.Status, d.Links.Count, clientLink != null);
    }

    private static void ApplyShares(ProjectDocs d, bool shareClients, bool shareSuppliers)
    {
        if (shareClients)
        {
            var clientLinks = d.Links.Where(l => l.UnitId.HasValue || l.ClientAccountId.HasValue).ToList();
            if (clientLinks.Count == 0) d.ShareAllBuyers = true;
            foreach (var l in clientLinks) l.SharedInPortal = true;
        }
        if (shareSuppliers)
            foreach (var l in d.Links.Where(l => l.CompanyId.HasValue)) l.SharedInPortal = true;
    }

    private static void Promote(ProjectDocs d, DocumentRevision rev, string? userId, string? userName)
    {
        foreach (var r in d.Revisions.Where(r => r.Status == 2 && r.Id != rev.Id)) r.Status = 3;
        rev.Status = 2;
        rev.ApprovedByUserId = userId;
        rev.ApprovedByName = userName;
        rev.ApprovedDate = DateTime.UtcNow;
        d.CurrentRevisionId = rev.Id;
        d.Filename = rev.Filename;
        if (d.Status is DocumentStatus.Concept or DocumentStatus.TerGoedkeuring) d.Status = DocumentStatus.Goedgekeurd;
        d.ModifiedDate = DateTime.UtcNow;
    }

    private async Task<ProjectDocs?> LoadTracked(int projectId, int documentId) =>
        await _db.ProjectDocs.AsSplitQuery()
            .Include(d => d.Folder).Include(d => d.Revisions).Include(d => d.Signatures).Include(d => d.Links)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.ProjectId == projectId);

    private void AddLinkRow(ProjectDocs d, string type, int id)
    {
        var exists = d.Links.Any(l => (type == "unit" && l.UnitId == id) || (type == "client" && l.ClientAccountId == id) || (type == "company" && l.CompanyId == id));
        if (exists) return;
        d.Links.Add(new DocumentLink
        {
            UnitId = type == "unit" ? id : null,
            ClientAccountId = type == "client" ? id : null,
            CompanyId = type == "company" ? id : null,
            CreatedDate = DateTime.UtcNow
        });
    }

    // =====================================================================
    // Upload
    // =====================================================================

    public async Task<DocResult> Upload(DocUploadDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StoredFilename)) return DocResult.Fail("Er is geen bestand opgeladen.");
        var now = DateTime.UtcNow;
        var isPortal = dto.UploaderKind != DocumentUploaderKind.Intern;

        DocumentRequest? req = null;
        if (dto.RequestId.HasValue)
        {
            req = await _db.DocumentRequests.FirstOrDefaultAsync(r => r.Id == dto.RequestId && r.ProjectId == dto.ProjectId);
            if (req == null) return DocResult.Fail("Het verwachte document bestaat niet meer.");
        }

        ProjectDocs doc;
        DocumentRevision rev;
        var isNew = dto.Mode != "revision" || !dto.DocumentId.HasValue;

        if (!isNew)
        {
            doc = (await LoadTracked(dto.ProjectId, dto.DocumentId!.Value))!;
            if (doc == null) return DocResult.Fail("Het document bestaat niet meer.");
            if (DocumentRules.IsFrozen(doc.Status, doc.Signatures.Select(s => s.Status)))
                return DocResult.Fail("Dit document is getekend en bevroren: maak een nieuw document dat het vervangt.");
        }
        else
        {
            var folderId = dto.FolderId ?? req?.FolderId ?? await FolderIdByCode("overige");
            var name = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name!.Trim() : req?.Name ?? System.IO.Path.GetFileNameWithoutExtension(dto.OriginalFilename ?? dto.StoredFilename);
            doc = new ProjectDocs
            {
                ProjectId = dto.ProjectId,
                Name = name.Length > 200 ? name[..200] : name,
                Filename = dto.StoredFilename,
                SortOrder = 0,
                Date = dto.DocDate ?? DateOnly.FromDateTime(DateTime.Today),
                FolderId = folderId,
                DocumentNumber = dto.Number,
                AuthoredBy = dto.AuthoredBy,
                Perceel = dto.Perceel ?? req?.Perceel,
                Type = req?.LegacyDocType,
                CreatedDate = now,
                CreatedByUserId = dto.UserId,
                Status = DocumentStatus.Concept
            };
            doc.ExpiresOn = dto.ExpiresOn ?? DocumentRules.ExpiryFromYears(doc.Date, req?.ExpiryYears);

            // Koppelingen: expliciete + die van het verwachte document
            foreach (var l in dto.Links) AddLinkRow(doc, l.Type, l.Id);
            if (req != null)
            {
                if (req.UnitId.HasValue) AddLinkRow(doc, "unit", req.UnitId.Value);
                if (req.ResponsibleCompanyId.HasValue) AddLinkRow(doc, "company", req.ResponsibleCompanyId.Value);
                if (req.ResponsibleClientAccountId.HasValue) AddLinkRow(doc, "client", req.ResponsibleClientAccountId.Value);
            }
            _db.ProjectDocs.Add(doc);
            await _db.SaveChangesAsync(); // eerst het document (revisie verwijst ernaar; CurrentRevisionId volgt daarna)
            await _db.Entry(doc).Reference(d => d.Folder).LoadAsync();
        }

        var viewKind = doc.Folder?.ViewKind ?? "default";
        var folderCode = doc.Folder?.Code ?? "overige";
        var nextNo = doc.Revisions.Count == 0 ? 1 : doc.Revisions.Max(r => r.RevisionNo) + 1;

        // Revisiestatus: portaalupload → ter goedkeuring (behalve offertes: die tellen meteen), intern → gekozen status
        byte revStatus;
        if (isPortal && viewKind != "offertes") revStatus = 1;
        else if (dto.Status == DocumentStatus.Concept) revStatus = 0;
        else if (dto.Status == DocumentStatus.TerGoedkeuring) revStatus = 1;
        else revStatus = 2;

        rev = new DocumentRevision
        {
            DocumentId = doc.Id,
            RevisionNo = nextNo,
            Filename = dto.StoredFilename,
            OriginalFilename = dto.OriginalFilename,
            SizeBytes = dto.SizeBytes,
            Status = revStatus,
            Note = dto.Note,
            Amount = dto.Amount,
            UploadedByKind = dto.UploaderKind,
            UploadedByUserId = dto.UserId,
            UploadedByName = dto.UserName,
            UploadedDate = now
        };
        _db.DocumentRevisions.Add(rev);
        await _db.SaveChangesAsync();

        if (!isNew)
        {
            // Metadata die bij de revisie meekwam, overschrijft enkel als ze ingevuld is
            if (dto.DocDate.HasValue) doc.Date = dto.DocDate;
            if (dto.ExpiresOn.HasValue) doc.ExpiresOn = dto.ExpiresOn;
            if (!string.IsNullOrWhiteSpace(dto.Number)) doc.DocumentNumber = dto.Number;
            if (!string.IsNullOrWhiteSpace(dto.Perceel)) doc.Perceel = dto.Perceel;
        }

        if (viewKind == "offertes")
        {
            // Een offerte is meteen "huidig" zodra ze binnen is; de status volgt Concept/Ingediend (gegund blijft gegund)
            rev.Status = 2;
            rev.ApprovedDate = now;
            Promote(doc, rev, dto.UserId, dto.UserName);
            if (doc.Status != DocumentStatus.Gegund && doc.Status != DocumentStatus.NietGegund)
                doc.Status = dto.Status == DocumentStatus.Concept ? DocumentStatus.Concept : DocumentStatus.Ingediend;
        }
        else if (revStatus == 2)
        {
            Promote(doc, rev, dto.UserId, dto.UserName);
            if (isNew && dto.Status is DocumentStatus.Getekend) doc.Status = DocumentStatus.Getekend;
        }
        else if (doc.CurrentRevisionId == null)
        {
            doc.Filename = rev.Filename;
            doc.Status = revStatus == 0 ? DocumentStatus.Concept : DocumentStatus.TerGoedkeuring;
        }

        ApplyShares(doc, dto.ShareClients || (req?.ShareWithBuyerAfterApproval == true && revStatus == 2), dto.ShareSuppliers);
        SyncLegacyColumns(doc, folderCode);
        doc.ModifiedDate = now;

        if (req != null)
        {
            req.FulfilledDocumentId = doc.Id;
            req.Status = DocumentRequestStatus.Ontvangen;
        }
        await _db.SaveChangesAsync();
        return DocResult.Success(isNew ? "Document toegevoegd." : $"Revisie {DocumentRules.RevisionLabel(nextNo)} toegevoegd.", doc.Id);
    }

    public async Task<DocResult> UpdateDocument(int projectId, int documentId, DocUpdateDto dto, string? userId)
    {
        var d = await LoadTracked(projectId, documentId);
        if (d == null) return DocResult.Fail("Document niet gevonden.");
        if (string.IsNullOrWhiteSpace(dto.Name)) return DocResult.Fail("Geef het document een naam.");
        d.Name = dto.Name.Trim().Length > 200 ? dto.Name.Trim()[..200] : dto.Name.Trim();
        if (dto.FolderId.HasValue && dto.FolderId != d.FolderId)
        {
            d.FolderId = dto.FolderId;
            await _db.Entry(d).Reference(x => x.Folder).LoadAsync();
        }
        d.DocumentNumber = string.IsNullOrWhiteSpace(dto.Number) ? null : dto.Number.Trim();
        d.AuthoredBy = string.IsNullOrWhiteSpace(dto.AuthoredBy) ? null : dto.AuthoredBy.Trim();
        d.Date = dto.DocDate;
        d.ExpiresOn = dto.ExpiresOn;
        d.Perceel = string.IsNullOrWhiteSpace(dto.Perceel) ? null : dto.Perceel.Trim();
        d.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return DocResult.Success("Document bijgewerkt.");
    }

    public async Task<DocResult> ApproveRevision(int projectId, int revisionId, string? userId, string? userName)
    {
        var rev = await _db.DocumentRevisions.Include(r => r.Document).FirstOrDefaultAsync(r => r.Id == revisionId && r.Document.ProjectId == projectId);
        if (rev == null) return DocResult.Fail("Revisie niet gevonden.");
        var d = await LoadTracked(projectId, rev.DocumentId);
        if (d == null) return DocResult.Fail("Document niet gevonden.");
        if (DocumentRules.IsFrozen(d.Status, d.Signatures.Select(s => s.Status)))
            return DocResult.Fail("Dit document is getekend en bevroren.");
        var r2 = d.Revisions.First(r => r.Id == revisionId);
        Promote(d, r2, userId, userName);

        // Verwacht document dat na goedkeuring gedeeld moet worden met de koper
        var req = await _db.DocumentRequests.FirstOrDefaultAsync(r => r.FulfilledDocumentId == d.Id && r.ShareWithBuyerAfterApproval);
        if (req != null) ApplyShares(d, true, false);
        SyncLegacyColumns(d, d.Folder?.Code ?? "overige");
        await _db.SaveChangesAsync();
        return DocResult.Success($"Revisie {DocumentRules.RevisionLabel(r2.RevisionNo)} goedgekeurd en nu de huidige versie.");
    }

    public async Task<DocResult> RejectRevision(int projectId, int revisionId, string? note, string? userId, string? userName)
    {
        var rev = await _db.DocumentRevisions.Include(r => r.Document).FirstOrDefaultAsync(r => r.Id == revisionId && r.Document.ProjectId == projectId);
        if (rev == null) return DocResult.Fail("Revisie niet gevonden.");
        if (rev.Id == rev.Document.CurrentRevisionId) return DocResult.Fail("De huidige revisie kan niet afgewezen worden.");
        rev.Status = 3;
        rev.ApprovedByUserId = userId;
        rev.ApprovedByName = userName;
        rev.ApprovedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(note)) rev.Note = string.IsNullOrWhiteSpace(rev.Note) ? "Afgewezen: " + note : rev.Note + " — afgewezen: " + note;
        await _db.SaveChangesAsync();
        return DocResult.Success("Revisie afgewezen.");
    }

    public async Task<DocResult> DeleteDocument(int projectId, int documentId)
    {
        var d = await LoadTracked(projectId, documentId);
        if (d == null) return DocResult.Fail("Document niet gevonden.");
        try
        {
            // Verwijzingen loskoppelen (circulaire FK CurrentRevision <-> Revisions eerst breken)
            d.CurrentRevisionId = null;
            await _db.SaveChangesAsync();

            foreach (var r in await _db.DocumentRequests.Where(r => r.FulfilledDocumentId == documentId).ToListAsync())
            {
                r.FulfilledDocumentId = null;
                r.Status = r.RequestedDate.HasValue ? DocumentRequestStatus.Aangevraagd : DocumentRequestStatus.Ontbreekt;
            }
            foreach (var o in await _db.ProjectDocs.Where(x => x.RelatedDocumentId == documentId).ToListAsync()) o.RelatedDocumentId = null;
            await _db.SaveChangesAsync();

            _db.DocumentSignatures.RemoveRange(d.Signatures);
            _db.DocumentLinks.RemoveRange(d.Links);
            await _db.SaveChangesAsync();
            _db.DocumentRevisions.RemoveRange(d.Revisions);
            await _db.SaveChangesAsync();
            _db.ProjectDocs.Remove(d);
            await _db.SaveChangesAsync();
            return DocResult.Success("Document verwijderd.");
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return DocResult.Fail("Het document is nog in gebruik (bv. gekoppeld aan een factuur of dossier) en kan niet verwijderd worden.");
        }
    }

    // =====================================================================
    // Koppelingen en delen
    // =====================================================================

    public async Task<DocResult> AddLink(int projectId, int documentId, string type, int targetId, bool share)
    {
        if (type is not ("unit" or "client" or "company")) return DocResult.Fail("Onbekend koppelingstype.");
        var d = await LoadTracked(projectId, documentId);
        if (d == null) return DocResult.Fail("Document niet gevonden.");
        AddLinkRow(d, type, targetId);
        if (share)
        {
            var l = d.Links.Last();
            l.SharedInPortal = true;
        }
        SyncLegacyColumns(d, d.Folder?.Code ?? "overige");
        d.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return DocResult.Success("Gekoppeld.");
    }

    public async Task<DocResult> RemoveLink(int projectId, int documentId, int linkId)
    {
        var d = await LoadTracked(projectId, documentId);
        if (d == null) return DocResult.Fail("Document niet gevonden.");
        var l = d.Links.FirstOrDefault(x => x.Id == linkId);
        if (l == null) return DocResult.Fail("Koppeling niet gevonden.");
        var hadClient = d.ClientAccountId.HasValue;
        d.Links.Remove(l);
        _db.DocumentLinks.Remove(l);
        if (!d.Links.Any(x => x.ClientAccountId.HasValue)) d.ClientAccountId = null;
        SyncLegacyColumns(d, d.Folder?.Code ?? "overige");
        // Een oud klantdocument (ClientAccountId gevuld) dat zijn laatste klantkoppeling verliest, mag niet als
        // "verkoopdocument" op de publieke website (Type 1 + geen klant) terechtkomen.
        if (hadClient && !d.ClientAccountId.HasValue && d.Type == 1) d.Type = null;
        d.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return DocResult.Success("Ontkoppeld.");
    }

    public async Task<DocResult> SetShare(int projectId, int documentId, string audience, bool share)
    {
        var d = await LoadTracked(projectId, documentId);
        if (d == null) return DocResult.Fail("Document niet gevonden.");
        if (audience == "klant")
        {
            var links = d.Links.Where(l => l.UnitId.HasValue || l.ClientAccountId.HasValue).ToList();
            if (links.Count == 0) d.ShareAllBuyers = share; else foreach (var l in links) l.SharedInPortal = share;
        }
        else if (audience == "leverancier")
        {
            var links = d.Links.Where(l => l.CompanyId.HasValue).ToList();
            if (links.Count == 0) return DocResult.Fail("Koppel eerst een leverancier.");
            foreach (var l in links) l.SharedInPortal = share;
        }
        else return DocResult.Fail("Onbekende doelgroep.");
        d.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return DocResult.Success(share ? "Gedeeld in het portaal." : "Niet meer gedeeld.");
    }

    public async Task<DocResult> SetLinkShare(int projectId, int documentId, int linkId, bool share)
    {
        var l = await _db.DocumentLinks.FirstOrDefaultAsync(x => x.Id == linkId && x.DocumentId == documentId && x.Document.ProjectId == projectId);
        if (l == null) return DocResult.Fail("Koppeling niet gevonden.");
        l.SharedInPortal = share;
        await _db.SaveChangesAsync();
        return DocResult.Success();
    }

    // =====================================================================
    // Verwachte / aangevraagde documenten
    // =====================================================================

    private static byte KindByte(string k) => k switch { "leverancier" => (byte)1, "klant" => (byte)2, "extern" => (byte)3, _ => (byte)0 };

    private static void Apply(DocumentRequest r, DocRequestDto dto)
    {
        r.Name = dto.Name.Trim();
        r.FolderId = dto.FolderId;
        r.UnitId = dto.UnitId;
        r.Perceel = string.IsNullOrWhiteSpace(dto.Perceel) ? null : dto.Perceel.Trim();
        r.ResponsibleKind = KindByte(dto.ResponsibleKind);
        r.ResponsibleCompanyId = dto.ResponsibleKind == "leverancier" ? dto.CompanyId : null;
        r.ResponsibleClientAccountId = dto.ResponsibleKind == "klant" ? dto.ClientAccountId : null;
        r.ResponsibleName = string.IsNullOrWhiteSpace(dto.ResponsibleName) ? null : dto.ResponsibleName.Trim();
        r.DueDate = dto.DueDate;
        r.DueLabel = string.IsNullOrWhiteSpace(dto.DueLabel) ? null : dto.DueLabel.Trim();
        r.ReminderDaysBefore = dto.ReminderDaysBefore;
        r.ExpiryYears = dto.ExpiryYears;
        r.ShareWithBuyerAfterApproval = dto.ShareWithBuyerAfterApproval;
        r.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
    }

    public async Task<DocResult> CreateRequest(DocRequestDto dto, string? userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return DocResult.Fail("Geef het gevraagde document een naam.");
        if (dto.FolderId <= 0) return DocResult.Fail("Kies een map.");
        var r = new DocumentRequest { ProjectId = dto.ProjectId, CreatedDate = DateTime.UtcNow, Status = DocumentRequestStatus.Ontbreekt };
        Apply(r, dto);
        if (dto.SendNow)
        {
            r.Status = DocumentRequestStatus.Aangevraagd;
            r.RequestedDate = DateTime.UtcNow;
            r.RequestedByUserId = userId;
            r.RequestedByName = userName;
        }
        _db.DocumentRequests.Add(r);
        await _db.SaveChangesAsync();
        return DocResult.Success(dto.SendNow ? "Document aangevraagd." : "Verwacht document toegevoegd.", r.Id);
    }

    public async Task<DocResult> UpdateRequest(int projectId, int requestId, DocRequestDto dto)
    {
        var r = await _db.DocumentRequests.FirstOrDefaultAsync(x => x.Id == requestId && x.ProjectId == projectId);
        if (r == null) return DocResult.Fail("Aanvraag niet gevonden.");
        if (string.IsNullOrWhiteSpace(dto.Name)) return DocResult.Fail("Geef het gevraagde document een naam.");
        Apply(r, dto);
        await _db.SaveChangesAsync();
        return DocResult.Success("Aanvraag bijgewerkt.");
    }

    public async Task<DocResult> MarkRequested(int projectId, int requestId, string? userId, string? userName)
    {
        var r = await _db.DocumentRequests.FirstOrDefaultAsync(x => x.Id == requestId && x.ProjectId == projectId);
        if (r == null) return DocResult.Fail("Aanvraag niet gevonden.");
        r.Status = DocumentRequestStatus.Aangevraagd;
        r.RequestedDate ??= DateTime.UtcNow;
        r.RequestedByUserId ??= userId;
        r.RequestedByName ??= userName;
        await _db.SaveChangesAsync();
        return DocResult.Success("Aangevraagd.");
    }

    public async Task<DocResult> MarkReminded(int projectId, int requestId)
    {
        var r = await _db.DocumentRequests.FirstOrDefaultAsync(x => x.Id == requestId && x.ProjectId == projectId);
        if (r == null) return DocResult.Fail("Aanvraag niet gevonden.");
        r.LastReminderDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return DocResult.Success("Herinnering geregistreerd.");
    }

    public async Task<DocResult> SetRequestNotRequired(int projectId, int requestId, bool notRequired)
    {
        var r = await _db.DocumentRequests.FirstOrDefaultAsync(x => x.Id == requestId && x.ProjectId == projectId);
        if (r == null) return DocResult.Fail("Aanvraag niet gevonden.");
        r.Status = notRequired ? DocumentRequestStatus.NietVereist : (r.RequestedDate.HasValue ? DocumentRequestStatus.Aangevraagd : DocumentRequestStatus.Ontbreekt);
        await _db.SaveChangesAsync();
        return DocResult.Success(notRequired ? "Als niet vereist gemarkeerd." : "Terug als verwacht gemarkeerd.");
    }

    public async Task<DocResult> DeleteRequest(int projectId, int requestId)
    {
        var r = await _db.DocumentRequests.FirstOrDefaultAsync(x => x.Id == requestId && x.ProjectId == projectId);
        if (r == null) return DocResult.Fail("Aanvraag niet gevonden.");
        _db.DocumentRequests.Remove(r);
        await _db.SaveChangesAsync();
        return DocResult.Success("Verwijderd.");
    }

    public async Task<(string? Email, string? Name)> GetRequestRecipient(int projectId, int requestId)
    {
        var r = await _db.DocumentRequests.AsNoTracking().Include(x => x.ResponsibleCompany).Include(x => x.ResponsibleClientAccount)
            .FirstOrDefaultAsync(x => x.Id == requestId && x.ProjectId == projectId);
        if (r == null) return (null, null);
        var email = r.ResponsibleCompany?.Email ?? r.ResponsibleClientAccount?.Email;
        var name = r.ResponsibleCompany?.BedrijfsNaam ?? r.ResponsibleClientAccount?.Name ?? r.ResponsibleName;
        return (email, name);
    }

    public async Task<int> GenerateExpected(int projectId, string? userId)
    {
        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId)
            .Select(p => new { p.ProjectType, p.DeliveryDate, p.DeliveryDateDef }).FirstOrDefaultAsync();
        if (project == null) return 0;

        var templates = await _db.DocumentTemplates.AsNoTracking().Where(t => t.IsActive && (t.ProjectType == null || t.ProjectType == project.ProjectType)).OrderBy(t => t.SortOrder).ToListAsync();
        // Hoofdeenheden: woningen/commerciële ruimtes (typegroep 1 en 4) die niet aan een andere eenheid hangen —
        // een losse berging of parking heeft geen EPC of elektriciteitskeuring nodig.
        var units = await _db.Units.AsNoTracking()
            .Where(u => u.ProjectId == projectId && !u.IsLink && u.AttachedUnitId == null && (u.Type == null || u.Type.GroupId == 1 || u.Type.GroupId == 4))
            .Select(u => new { u.Id, u.ClientAccountId }).ToListAsync();
        var existing = await _db.DocumentRequests.AsNoTracking().Where(r => r.ProjectId == projectId && r.TemplateId != null)
            .Select(r => new { r.TemplateId, r.UnitId }).ToListAsync();
        var existingSet = existing.Select(e => (e.TemplateId!.Value, e.UnitId)).ToHashSet();
        var docs = await _db.ProjectDocs.AsNoTracking().Include(d => d.Links).Where(d => d.ProjectId == projectId && d.Type != null).ToListAsync();

        int created = 0;
        foreach (var t in templates)
        {
            DateOnly? due = null;
            var label = t.DueLabel?.ToLowerInvariant() ?? "";
            if (label.Contains("voorlopige")) due = project.DeliveryDate;
            else if (label.Contains("oplevering")) due = project.DeliveryDateDef ?? project.DeliveryDate;

            IEnumerable<int?> targets = t.PerUnit ? units.Select(u => (int?)u.Id) : new int?[] { null };
            foreach (var uid in targets)
            {
                if (existingSet.Contains((t.Id, uid))) continue;
                // Al aanwezig via een bestaand (oud) document van hetzelfde type dat aan deze eenheid of haar koper hangt?
                if (t.LegacyDocType.HasValue)
                {
                    var buyer = uid.HasValue ? units.First(u => u.Id == uid.Value).ClientAccountId : null;
                    var covered = docs.Any(d => d.Type == t.LegacyDocType &&
                        (uid.HasValue
                            ? d.Links.Any(l => l.UnitId == uid || (buyer.HasValue && l.ClientAccountId == buyer))
                            : true));
                    if (covered) continue;
                }
                _db.DocumentRequests.Add(new DocumentRequest
                {
                    ProjectId = projectId, FolderId = t.FolderId, TemplateId = t.Id, Name = t.Name, UnitId = uid,
                    ResponsibleKind = 3, ResponsibleName = t.ResponsibleRole, ResponsibleRole = t.ResponsibleRole,
                    DueLabel = t.DueLabel, DueDate = due, ReminderDaysBefore = t.ReminderDays, ExpiryYears = t.ExpiryYears,
                    LegacyDocType = t.LegacyDocType, Status = DocumentRequestStatus.Ontbreekt, CreatedDate = DateTime.UtcNow
                });
                created++;
            }
        }
        if (created > 0) await _db.SaveChangesAsync();
        return created;
    }

    // =====================================================================
    // Ondertekening
    // =====================================================================

    public async Task<DocResult> SendForSignature(int projectId, int documentId, List<DocSignatoryDto> signatories)
    {
        var d = await LoadTracked(projectId, documentId);
        if (d == null) return DocResult.Fail("Document niet gevonden.");
        if (d.CurrentRevisionId == null) return DocResult.Fail("Keur eerst een revisie goed voor ze ter ondertekening gaat.");
        if (d.Status == DocumentStatus.Getekend) return DocResult.Fail("Dit document is al getekend.");
        var list = signatories.Where(s => !string.IsNullOrWhiteSpace(s.Name)).ToList();
        if (list.Count == 0) return DocResult.Fail("Voeg minstens één ondertekenaar toe.");
        if (d.Signatures.Any(s => s.Status == 2)) return DocResult.Fail("Er is al getekend: de ondertekenaars kunnen niet meer gewijzigd worden.");

        _db.DocumentSignatures.RemoveRange(d.Signatures);
        d.Signatures.Clear();
        var now = DateTime.UtcNow;
        var i = 0;
        foreach (var s in list)
        {
            d.Signatures.Add(new DocumentSignature
            {
                DocumentId = d.Id, RevisionId = d.CurrentRevisionId, SignOrder = i++, Name = s.Name.Trim(), Role = s.Role,
                ClientAccountId = s.ClientAccountId, UserId = s.UserId, Status = 0, SentDate = now
            });
        }
        d.Status = DocumentStatus.TerOndertekening;
        d.ModifiedDate = now;
        await _db.SaveChangesAsync();
        return DocResult.Success("Ter ondertekening verstuurd.");
    }

    public async Task<DocResult> MarkSigned(int projectId, int signatureId, string method)
    {
        var sig = await _db.DocumentSignatures.Include(s => s.Document).FirstOrDefaultAsync(s => s.Id == signatureId && s.Document.ProjectId == projectId);
        if (sig == null) return DocResult.Fail("Ondertekenaar niet gevonden.");
        if (sig.Status == 2) return DocResult.Success("Al getekend.");
        sig.Status = 2;
        sig.SignedDate = DateTime.UtcNow;
        sig.Method = method is "itsme" ? "itsme" : "manueel";
        await _db.SaveChangesAsync();

        var d = await LoadTracked(projectId, sig.DocumentId);
        if (d == null) return DocResult.Success("Getekend.");
        if (d.Signatures.All(s => s.Status == 2))
        {
            d.Status = DocumentStatus.Getekend;
            d.ModifiedDate = DateTime.UtcNow;
            var effect = await ApplySignedEffects(d);
            await _db.SaveChangesAsync();
            return DocResult.Success("Alle partijen tekenden: het document is getekend en bevroren." + (effect != null ? " " + effect : ""));
        }
        await _db.SaveChangesAsync();
        return DocResult.Success("Handtekening geregistreerd.");
    }

    /// <summary>Een volledig getekend contract (map Contracten, één eenheid + één koper) haalt de eenheid uit "optie" en zet
    /// de koper op de eenheid als die nog vrij was. Gebeurt nooit als de eenheid aan een ANDERE klant verkocht is.</summary>
    private async Task<string?> ApplySignedEffects(ProjectDocs d)
    {
        if (d.Folder?.ViewKind != "contracten") return null;
        // Een wijzigingsopdracht is een aanvulling op een bestaand contract en verandert de verkoopstatus nooit.
        if (d.ChangeOrderId != null) return null;
        var unitLinks = d.Links.Where(l => l.UnitId.HasValue).ToList();
        var clientLinks = d.Links.Where(l => l.ClientAccountId.HasValue).ToList();
        if (unitLinks.Count != 1 || clientLinks.Count != 1) return null;
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == unitLinks[0].UnitId);
        if (unit == null) return null;
        var clientId = clientLinks[0].ClientAccountId!.Value;
        if (unit.ClientAccountId.HasValue && unit.ClientAccountId != clientId)
            return $"{unit.Name} is aan een andere klant gekoppeld — de eenheid werd niet aangepast.";
        var changed = unit.IsOption || !unit.ClientAccountId.HasValue;
        unit.ClientAccountId = clientId;
        unit.IsOption = false;
        return changed ? $"{unit.Name} staat nu op Verkocht." : null;
    }

    public async Task<DocResult> MarkSignatureReminded(int projectId, int signatureId)
    {
        var sig = await _db.DocumentSignatures.FirstOrDefaultAsync(s => s.Id == signatureId && s.Document.ProjectId == projectId);
        if (sig == null) return DocResult.Fail("Ondertekenaar niet gevonden.");
        sig.ReminderDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return DocResult.Success("Herinnering geregistreerd.");
    }

    // =====================================================================
    // Offertes
    // =====================================================================

    public async Task<DocResult> Award(int projectId, int documentId, string? userId, string? userName)
    {
        var d = await LoadTracked(projectId, documentId);
        if (d == null) return DocResult.Fail("Document niet gevonden.");
        if (d.Folder?.ViewKind != "offertes") return DocResult.Fail("Enkel een offerte kan gegund worden.");
        if (d.Status != DocumentStatus.Ingediend) return DocResult.Fail("Enkel een ingediende offerte kan gegund worden.");

        d.Status = DocumentStatus.Gegund;
        d.ModifiedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(d.Perceel))
        {
            var others = await _db.ProjectDocs.Where(x => x.ProjectId == projectId && x.Id != d.Id && x.Perceel == d.Perceel && x.Folder!.ViewKind == "offertes" && x.Status == DocumentStatus.Ingediend).ToListAsync();
            foreach (var o in others) { o.Status = DocumentStatus.NietGegund; o.ModifiedDate = DateTime.UtcNow; }
        }

        var company = d.Links.FirstOrDefault(l => l.CompanyId.HasValue);
        var amount = d.Revisions.FirstOrDefault(r => r.Id == d.CurrentRevisionId)?.Amount;
        var supplier = company != null ? await _db.CompanyInfo.AsNoTracking().Where(c => c.CompanyId == company.CompanyId).Select(c => c.BedrijfsNaam).FirstOrDefaultAsync() : null;
        // De bestelbon zelf (PDF) wordt nog niet gegenereerd: hij staat klaar als verwacht document in dezelfde map.
        _db.DocumentRequests.Add(new DocumentRequest
        {
            ProjectId = projectId, FolderId = d.FolderId ?? 0, Name = $"Bestelbon {d.Perceel ?? d.Name}".Trim(),
            Perceel = d.Perceel, ResponsibleKind = 0, ResponsibleName = userName,
            Note = $"Gegund aan {supplier ?? "leverancier"}" + (amount.HasValue ? $" voor € {amount.Value:N2}" : "") + $" (offerte '{d.Name}').",
            Status = DocumentRequestStatus.Ontbreekt, RequestedByUserId = userId, RequestedByName = userName, CreatedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return DocResult.Success("Offerte gegund; de andere offertes voor dit perceel staan op 'Niet gegund'. De bestelbon staat klaar als verwacht document.");
    }
}
