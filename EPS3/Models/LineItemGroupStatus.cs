using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class LineItemGroupStatus : IStatus
    {
        public LineItemGroupStatus() { }
        public LineItemGroupStatus(LineItemGroup lineItemGroup)
        {
            LineItemGroupID = lineItemGroup.GroupID;
            LineItemGroup = lineItemGroup;
        }

        // These fields are used in Work Program review
        public decimal? AmountReduced { get; set; }
        public String AmountString
        {
            get {
                if (AmountReduced == null) { return ""; }
                else {
                    decimal Amount = (decimal)AmountReduced;
                    return "$"+Amount.ToString("#,##0.00");
                }
            }
        }
        public string ItemReduced { get; set; }

        public int LineItemGroupID { get; set; }
        public virtual LineItemGroup LineItemGroup { get; set; }
    }
}
