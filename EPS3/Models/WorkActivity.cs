using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class WorkActivity
    {
        [Key]
        public int ActivityID { get; set; }
        public string ActivityCode { get; set; }
        public string ActivityName { get; set; }

    }
}
