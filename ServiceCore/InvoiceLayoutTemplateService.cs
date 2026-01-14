using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Invoicing.Pdf.Templates;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore;

public class InvoiceLayoutTemplateService : IInvoiceLayoutTemplateService
{
    private readonly UnitOfWorkCore _uow;
    private readonly cpmRunningContext _db;

    public InvoiceLayoutTemplateService(UnitOfWorkCore uow)
    {
        _uow = uow;
        _db = (cpmRunningContext)uow.Context;
    }

    public async Task EnsureDefaultsAsync(CancellationToken ct = default)
    {
        var existingKeys = await _db.InvoiceLayoutTemplate
            .AsNoTracking()
            .Select(t => t.Key)
            .ToListAsync(ct);

        var defaults = new[]
        {
            new InvoiceLayoutTemplate
            {
                Key = "layoutA",
                Name = "Layout A (Group LN / BCO)",
                Description = "Standaard lay-out voor Group LN / BCO.",
                TemplateJson = DefaultLayouts.LayoutA,
                IsSystem = true
            },
            new InvoiceLayoutTemplate
            {
                Key = "layoutHE",
                Name = "Layout B (Home-Estate)",
                Description = "Standaard lay-out voor Home-Estate.",
                TemplateJson = DefaultLayouts.LayoutHE,
                IsSystem = true
            }
        };

        var toAdd = defaults
            .Where(d => !existingKeys.Contains(d.Key, System.StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (toAdd.Count == 0)
            return;

        _db.InvoiceLayoutTemplate.AddRange(toAdd);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<InvoiceLayoutTemplateBO>> ListAsync(CancellationToken ct = default)
        => await _db.InvoiceLayoutTemplate.AsNoTracking()
            .OrderByDescending(t => t.IsSystem)
            .ThenBy(t => t.Name)
            .Select(t => new InvoiceLayoutTemplateBO
            {
                Id = t.Id,
                Key = t.Key,
                Name = t.Name,
                Description = t.Description,
                TemplateJson = t.TemplateJson,
                IsSystem = t.IsSystem
            })
            .ToListAsync(ct);

    public async Task<InvoiceLayoutTemplateBO?> GetAsync(int id, CancellationToken ct = default)
        => await _db.InvoiceLayoutTemplate.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new InvoiceLayoutTemplateBO
            {
                Id = t.Id,
                Key = t.Key,
                Name = t.Name,
                Description = t.Description,
                TemplateJson = t.TemplateJson,
                IsSystem = t.IsSystem
            })
            .FirstOrDefaultAsync(ct);

    public async Task<InvoiceLayoutTemplateBO?> GetByKeyAsync(string key, CancellationToken ct = default)
        => await _db.InvoiceLayoutTemplate.AsNoTracking()
            .Where(t => t.Key == key)
            .Select(t => new InvoiceLayoutTemplateBO
            {
                Id = t.Id,
                Key = t.Key,
                Name = t.Name,
                Description = t.Description,
                TemplateJson = t.TemplateJson,
                IsSystem = t.IsSystem
            })
            .FirstOrDefaultAsync(ct);

    public async Task<int> CreateAsync(InvoiceLayoutTemplateBO bo, CancellationToken ct = default)
    {
        var entity = new InvoiceLayoutTemplate
        {
            Key = bo.Key.Trim(),
            Name = bo.Name.Trim(),
            Description = bo.Description,
            TemplateJson = bo.TemplateJson,
            IsSystem = bo.IsSystem
        };
        _uow.InvoiceLayoutTemplates.Add(entity);
        await _uow.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(InvoiceLayoutTemplateBO bo, CancellationToken ct = default)
    {
        var entity = await _db.InvoiceLayoutTemplate.FirstAsync(t => t.Id == bo.Id, ct);
        entity.Key = bo.Key.Trim();
        entity.Name = bo.Name.Trim();
        entity.Description = bo.Description;
        entity.TemplateJson = bo.TemplateJson;
        entity.IsSystem = bo.IsSystem;
        entity.UpdatedAt = System.DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.InvoiceLayoutTemplate.FirstAsync(t => t.Id == id, ct);
        _uow.InvoiceLayoutTemplates.Remove(entity);
        await _uow.SaveChangesAsync(ct);
    }
}
