using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Models;

namespace EPS3.ViewModels
{
    public class LineItemsListViewModel
    {
        public Dictionary<string, List<LineItemGroup>> Encumbrances { get; set; }
    }
}
