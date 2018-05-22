using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class Recipient
    {
        [Key]
        [Display(Name = "Recipient")]
        public int RecipientID { get; set; }
        public string RecipientCode { get; set; }
        public string RecipientName { get; set; }

        public string RecipientSelector {
            get { return RecipientCode + " - " + RecipientName; }
        }
    }
}
