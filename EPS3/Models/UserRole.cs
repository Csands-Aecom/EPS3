using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EPS3.Models
{
    public class UserRole
    {
        [Key]
        public int UserRoleID { get; set; }

        [ForeignKey("UserID")]
        [Display(Name = "User")]
        public int UserID { get; set; }
        [Display(Name = "Role")]
        [StringLength(25)]
        //Note this conceptually should relate to Roles table, but that table is empty and not modeled as an entity class.
        // Accepted Values= {Admin, Originator, Finance Reviewer, WP Reviewer, CFM Submitter, Closer, and CloserCC}

        public string Role { get; set; }
        [Display(Name = "Begin Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime BeginDate { get; set; }
        [Display(Name = "End Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? EndDate { get; set; }

        public override string ToString()
        {
            return Role;
        }
    }
}
