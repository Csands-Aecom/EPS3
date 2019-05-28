using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Models;

namespace EPS3.ViewModels
{
    public class ContractViewModel
    {
        public Contract Contract{ get; set; }
        public List<LineItem> LineItems { get; set; }

    }
}
