using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class OCA
    {
        [Key]
        [Display(Name = "OCA")]
        public int OCAID { get; set; }
        public string OCACode { get; set; }
        public string OCAName { get; set; }

        public string OCASelector
        {
            get { return OCACode + " - " + OCAName; }
        }
    }
}
