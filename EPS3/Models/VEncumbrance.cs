using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class VEncumbrance
    {
        public int ContractID { get; }
        public string ContractNumber { get; }
        [Key]
        public int GroupID { get; set; }
        public string CurrentStatus { get; }
        public string ContractStatus { get; }
        public string LineItemType { get; }
        public int OriginatorUserID { get; }
        public DateTime OriginatedDate { get; }
        public DateTime LastEditedDate { get; }
        public decimal? TotalAmount { get; }
        public string FinancialProjectNumbers { get; }
    }
}
