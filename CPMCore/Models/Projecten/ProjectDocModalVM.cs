using System;
using System.Collections.Generic;
using BOCore;

namespace CPMCore.Models.Projecten
{
    public class ProjectDocModalVM
    {
        public ProjectDocBO Document { get; set; } = new();
        public IReadOnlyList<IdNameBO> Clients { get; set; } = Array.Empty<IdNameBO>();
        public string Target { get; set; } = "Project";
        public int? SelectedClientAccountId { get; set; }
        public bool IsEditMode { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? DocumentUrl { get; set; }
    }
}