using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public enum LineItemType { New, Amendment, Supplemental, LOA, Renewal, Overrun, Settlement, Correction, Deletion };
    public class LineItemStatus : IStatus
    {
        public LineItemStatus() { }
        public LineItemStatus(int userID, int lineItemID, string lineItemType)
        {
            UserID = userID;
            LineItemID = lineItemID;
            SubmittalDate = DateTime.Now;
        }

        [Display(Name = "Amount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public int ContractID { get; set; }
        public virtual Contract Contract { get; set; }
        public int LineItemID { get; set; }
        public virtual LineItem LineItem { get; set; }

        public string LineItemNumber { get; set; } // From FACTS database. e.g. O001, S002, L003, R004, etc.
        // LineItemNumber refers to the current line item. ReferenceLineItem refers to the LineItem this one is correcting
        // Usually an Amendment or correction i.e., C014 to O003 or A009 to 0OO4
        public string ReferenceLineItemNumber { get; set; } 

    }
}
