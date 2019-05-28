using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class SearchForm
    {
        public string SearchContractNumber { get; set; }
        public string SearchCurrentStatus { get; set; }
        public DateTime? SearchStartDate { get; set; }
        public DateTime? SearchEndDate { get; set; }
        public decimal? SearchMinAmount { get; set; }
        public decimal? SearchMaxAmount { get; set; }
        public bool IsLineItemAmount { get; set; }
        public bool IsEncumbranceAmount { get; set; }
        public bool IsContractAmount { get; set; }
    }
}
