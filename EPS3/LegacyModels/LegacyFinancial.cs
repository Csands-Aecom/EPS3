using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.LegacyModels
{
    public class LegacyFinancial
    {
        [Key]
        public int RecID { get; set; }
        public string Org { get; set; }
        public string Code { get; set; }
        public string EO { get; set; }
        public string ObjectFIN { get; set; }
        public decimal Ammount { get; set; }
        public string Finproject { get; set; }
        public string Fcy { get; set; }
        public string FiscalYear { get; set; }
        public string Found { get; set; }
        public string FlairAccountCode { get; set; }
    }
}
