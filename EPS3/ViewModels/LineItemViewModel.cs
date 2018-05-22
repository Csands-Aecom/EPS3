using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Models;

namespace EPS3.ViewModels
{
    public class LineItemViewModel
    {
        public LineItem LineItem { get; set; }
        public List<LineItemStatus> Statuses  { get; set; }
    }
}
