using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class LineItemGroup
    {
        public LineItemGroup() {

        }
        public LineItemGroup(Contract contract, User user)
        {
            this.ContractID = contract.ContractID;
            this.LastEditedUserID = user.UserID;
            this.LastEditedDate = DateTime.Now;
            this.IsEditable = 1;
        }
        public LineItemGroup(int contractID, int userID)
        {
            this.ContractID = contractID;
            this.LastEditedUserID = userID;
            this.LastEditedDate = DateTime.Now;
            this.IsEditable = 1;
        }


        [Key]
        [Display(Name = "Encumbrance ID")]
        public int GroupID { get; set; }
        public int ContractID { get; set; }
        public virtual Contract Contract { get; set; }

        [Display(Name = "Encumbrance Type")]
        public string LineItemType { get; set; }
        [Display(Name = "User Assigned Amendment ID")]
        [StringLength(10)]
        public string AmendedLineItemID { get; set; }

        [Display(Name = "FLAIR Amendment ID")]
        [StringLength(10)]
        public string FlairAmendmentID { get; set; }

        [Display(Name = "User Assigned Amendment ID")]
        [StringLength(10)]
        public string UserAssignedID { get; set; }

        public DateTime LastEditedDate { get; set; }
        public int LastEditedUserID { get; set; }
        public virtual User LastEditedUser { get; set; }

        public int OriginatorUserID { get; set; }
        public virtual User OriginatorUser { get; set; }

        public byte IsEditable { get; set; }

        [Display(Name = "Status")]
        public string CurrentStatus { get; set; }
        public virtual ICollection<LineItem> LineItems { get; set; }
        public virtual ICollection<LineItemGroupStatus> Statuses { get; set; }

    }
}
