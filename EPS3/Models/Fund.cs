using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class Fund
    {
        [Key]
        [Display(Name = "Fund")]
        public int FundID { get; set; }
        [Display(Name = "Fund Code")]
        public String FundCode { get; set; }
        [Display(Name = "Fund")]
        public String FundDescription { get; set; }
        
        public String FundSelector
        {
            get { return FundCode + " - " + FundDescription; }
        }
    }
}
