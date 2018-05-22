using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class Compensation
    {
        //These are lookup values in LookupCompensation table
        [Key]
        public int CompensationID { get; set; }
        [Display(Name = "Compensation")]
        public String CompensationType { get; set; }
    }
}
