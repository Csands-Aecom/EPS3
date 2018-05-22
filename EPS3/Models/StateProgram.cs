using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class StateProgram
    {
        [Key]
        [Display(Name ="State Program")]
        public int ProgramID { get; set; }
        public string ProgramCode { get; set; }
        public string ProgramName { get; set; }

        public string ProgramSelector
        {
            get { return ProgramCode + " - " + ProgramName; }
        }
    }
}
