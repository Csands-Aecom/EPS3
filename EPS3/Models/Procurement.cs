using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class Procurement
    {
        //These are lookup values in LookupProcurement table
        [Key]
        public int ProcurementID { get; set; }
        [Display(Name = "Procurement Code")]
        public String ProcurementCode { get; set; }
        [Display(Name = "Procurement")]
        public String ProcurementDescription { get; set; }

        public String ProcurementSelector { get { return ProcurementCode + " - " + ProcurementDescription; } }
    }
}
