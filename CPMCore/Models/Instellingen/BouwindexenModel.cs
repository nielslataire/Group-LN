using DALCore.Models;
using System.Collections.Generic;

namespace CPMCore.Models.Instellingen
{
    public class BouwindexenModel
    {
        public List<BouwIndex> SIndexen    { get; set; } = new();
        public List<BouwIndex> I2021Indexen { get; set; } = new();
        public List<BouwIndex> IPlusIndexen { get; set; } = new();
        public List<BouwIndex> AbexIndexen  { get; set; } = new();
    }
}
