using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class Vendor
    {
        //These are lookup values in the LookupVendor table
        //Vendors can be added, when necessary
        [Key]
        [Display(Name = "Vendor")]
        public int VendorID { get; set; }
        [Display(Name = "Vendor ID")]
        public String VendorCode { get; set; }
        [Display(Name = "Vendor Name")]
        public String VendorName { get; set; }

        public String VendorSelector {  get { return VendorCode + " - " + VendorName; } }
    }
}
