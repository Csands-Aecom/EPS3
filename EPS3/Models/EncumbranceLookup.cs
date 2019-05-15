using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class EncumbranceLookup
    {
        public int ContractID { get; set; }
        public string ContractNumber { get; set; }
        [Key]
        public int GroupID { get; set; }
        public decimal InitialAmount { get; set; }
        public DateTime? BeginningDate { get; set; }
        public DateTime? EndingDate { get; set; }
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
        public int OriginatorUserID { get; set; }
        public DateTime OriginatedDate { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime LastEditedDate { get; set; }
        public string UserAssignedID { get; set; }
        public string LineItemType { get; set; }
        public string EncumbranceStatus { get; set; }
        public string ContractStatus { get; set; }
        public string Description { get; set; }
        public decimal EncumbranceAmount { get; set; }
        public decimal ContractAmount { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FinancialProjectNumbers { get; set; }
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
        public string EncumbranceAmountString()
        {
           return String.Format("{0:C2}", EncumbranceAmount); 
        }
        public string ContractAmountString()
        {
            return String.Format("{0:C2}", ContractAmount); 
        }
    }
}
