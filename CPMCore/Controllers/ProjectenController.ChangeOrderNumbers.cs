using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace CPMCore.Controllers
{
    /// <summary>Publiek nummer van een offerte/wijzigingsopdracht (migratie 070): "OF-2026-014" / "WO-2026-006-v2".
    /// Voor plekken waar enkel een Id bekend is (bron, omgezet naar, vervangen door) wordt het nummer opgezocht
    /// en per request onthouden.</summary>
    public partial class ProjectenController
    {
        private readonly Dictionary<int, string> _coNumberCache = new();

        private string CoNo(ChangeOrder co) => co.PublicNumber;

        private string CoNo(int id)
        {
            if (_coNumberCache.TryGetValue(id, out var cached)) return cached;
            var co = _db.ChangeOrder.AsNoTracking().Where(c => c.Id == id)
                .Select(c => new { c.IsQuote, c.NumberYear, c.NumberSeq, c.VersionNo })
                .FirstOrDefault();
            var text = co is null ? $"#{id}" : DALCore.ChangeOrderNumbering.Format(co.IsQuote, co.NumberYear, co.NumberSeq, co.VersionNo, id);
            _coNumberCache[id] = text;
            return text;
        }

        private string CoNo(int? id) => id.HasValue ? CoNo(id.Value) : "";
    }
}
