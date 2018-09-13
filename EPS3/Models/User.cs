using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        [Display(Name = "First Name")]
        public String FirstName { get; set; }
        [Display(Name = "Last Name")]
        public String LastName { get; set; }

        public String FullName {
            get { return FirstName + " " + LastName; }
        }

        [Display(Name = "User ID")]
        public String UserLogin { get; set; }
        [Display(Name = "Email Address")]
        [EmailAddress]
        public String Email { get; set; }
        [Display(Name = "Phone Number")]
        [Phone]
        public String Phone { get; set; }
        [Display(Name = "Receive Emails?")]
        public byte ReceiveEmails { get; set; }
        [Display(Name = "Roles")]
        public virtual ICollection<UserRole> Roles { get; set; }
    }
}
