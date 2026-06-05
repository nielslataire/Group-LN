using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DALCore.Models
{
    [Table("BouwIndex")]
    public class BouwIndex
    {
        [Key] public int Id { get; set; }
        [MaxLength(10)] public string IndexType { get; set; } = "";
        public int? Jaar { get; set; }
        public int? Maand { get; set; }
        public decimal IndexWaarde { get; set; }
        public bool IsActief { get; set; }
        public DateTime? GeldigVanaf { get; set; }
        [MaxLength(100)] public string? Categorie { get; set; }
        [MaxLength(100)] public string? SubCategorie { get; set; }
        [MaxLength(200)] public string? Bron { get; set; }
        [MaxLength(500)] public string? Opmerking { get; set; }
    }
}
