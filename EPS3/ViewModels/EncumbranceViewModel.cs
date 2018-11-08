using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Models;

namespace EPS3.ViewModels
{
    public class EncumbranceViewModel
    {   
        public Contract Contract { get; set; }
        public LineItemGroup Encumbrance { get; set; }
        public List<LineItem> LineItems { get; set; }
        public List<LineItemGroupStatus> Statuses { get; set; }
        public List<int> WpRecipients { get; set; }

    }
}
