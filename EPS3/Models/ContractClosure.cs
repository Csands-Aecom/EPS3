using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class ContractClosure
    {
        public int ContractID { get; set; }
        public string FlairID { get; set; }
        public int? LineItemGroupID { get; set; }
        public string ClosureType { get; set; }
        public string ContractOrEncumbrance { get; set; }
        public string Comments { get; set; }
    }
}
