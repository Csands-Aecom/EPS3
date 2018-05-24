using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public enum StatusType { New, Draft, Submitted, FinanceApproved, BypassWP, WPApproved, CFMReady, CompleteWorkDone, CompleteInvalid, Closed, Deleted }
    public class IStatus
    {
        [Key]
        public int StatusID { get; set; }
        [ForeignKey("UserID")]
        public int UserID { get; set; }
        public virtual User User { get; set; }
        public string CurrentStatus { get; set; }
        [Display(Name = "Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy hh:mm }", ApplyFormatInEditMode = true)]
        public DateTime SubmittalDate { get; set; }
        [Display(Name = "Comment")]
        public String Comments { get; set; }
    }
}
