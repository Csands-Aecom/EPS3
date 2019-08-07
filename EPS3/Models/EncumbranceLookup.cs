using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Helpers;

namespace EPS3.Models
{
    public class EncumbranceLookup : IEquatable<EncumbranceLookup>, IEqualityComparer<EncumbranceLookup>
    {
        public int ContractID { get; set; }
        public string ContractNumber { get; set; }
        [Key]
        public int GroupID { get; set; }
        [Column(TypeName = "decimal(18,2)")]
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
        [Column(TypeName = "decimal(18,2)")]
        public decimal EncumbranceAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
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
           return Utils.FormatCurrency(EncumbranceAmount); 
        }
        public string ContractAmountString()
        {
            return Utils.FormatCurrency(ContractAmount); 
        }
        
        public bool Equals(EncumbranceLookup el)
        {
            if (el == null && this == null)
               return true;
            else if (this == null || el == null)
               return false;
            else if(this.GroupID == el.GroupID)
                return true;
            else
                return false;
        }

        public bool Equals(EncumbranceLookup e1, EncumbranceLookup e2)
        {
            return e1.Equals(e2);
        }

        public override int GetHashCode()
        {
            int hCode = GroupID ^ ContractID;
            return hCode.GetHashCode();
        }

        public int GetHashCode(EncumbranceLookup el)
        {
            return el.GetHashCode();
        }
    }
}
