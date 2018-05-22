using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class ContractType
    {
        [Key]
        [Display(Name = "Contract Type")]
        public int ContractTypeID { get; set; }
        [Display(Name = "Contract Type Code")]
        public string ContractTypeCode { get; set; }
        [Display(Name = "Contract Type")]
        public string ContractTypeName { get; set; }

        public string ContractTypeSelector
        {
            get { return ContractTypeCode + " - " + ContractTypeName; }
        }
    }
}
