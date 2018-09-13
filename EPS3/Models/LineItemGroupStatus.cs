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

        public int LineItemGroupID { get; set; }
        public virtual LineItemGroup LineItemGroup { get; set; }
    }
}
