using EPS3.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        public string AmountReduced { get; set; }
        public string ItemReduced { get; set; }

        public int LineItemGroupID { get; set; }
        public virtual LineItemGroup LineItemGroup { get; set; }
    }
}
