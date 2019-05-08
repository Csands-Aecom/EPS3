using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class VEncumbrance
    {
        public int ContractID { get; set; }
        public string ContractNumber { get; set; }
        [Key]
        public int GroupID { get; set; }
        public string EncumbranceStatus { get; set; }
        public string ContractStatus { get; set; }
        public string LineItemType { get; set; }
        public int OriginatorUserID { get; set; }
        public DateTime OriginatedDate { get; set; }
        public DateTime LastEditedDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }
        public string FinancialProjectNumbers { get; set; }
        
        public int OriginatorID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public List<string> FinProjList() {
            if (FinancialProjectNumbers != null)
            {
                return FinancialProjectNumbers.Split(',').ToList();
            }
            else
            {
                return null;
            }
        }
        public string TotalAmountString()
        {
            if (TotalAmount != null)
            {
                return String.Format("{0:C2}", TotalAmount);
            }
            else
            {
                return "$0.00";
            }
        }
    }
}
