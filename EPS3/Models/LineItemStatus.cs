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
    }
}
