using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public enum LineItemType { New, Amendment, Supplemental, LOA, Renewal, Overrun, Settlement, Correction, Deletion };
    public class LineItemStatus : IStatus
    {
        public int LineItemID { get; set; }
        public virtual LineItem LineItem { get; set; }
        [Display(Name ="Line Item Type")]
        public LineItemType LineItemType { get; set; }
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        public string LineItemNumber { get; set; } // From FACTS database. e.g. O001, S002, L003, R004, etc.
        // LineItemNumber refers to the current line item. ReferenceLineItem refers to the LineItem this one is correcting
        // Usually an Amendment or correction i.e., C014 to O003 or A009 to 0OO4
        public string ReferenceLineItemNumber { get; set; } 

    }
}
