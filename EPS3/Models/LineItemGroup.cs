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
            this.OriginatorUserID = user.UserID;
            this.LastEditedDate = DateTime.Now;
            this.OriginatedDate = DateTime.Now;
            this.CurrentStatus = ConstantStrings.Draft;
            this.IsEditable = 1;
        }
        public LineItemGroup(int contractID, int userID)
        {
            this.ContractID = contractID;
            this.LastEditedUserID = userID;
            this.OriginatorUserID = userID;
            this.LastEditedDate = DateTime.Now;
            this.OriginatedDate = DateTime.Now;
            this.CurrentStatus = ConstantStrings.Draft;
            this.IsEditable = 1;
        }


        [Key]
        [Display(Name = "Encumbrance ID")]
        public int GroupID { get; set;
        }
        [Display(Name = "Contract ID")]
        public int ContractID { get; set; }

        public virtual Contract Contract { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Encumbrance Type")]
        public string LineItemType { get; set; }

        [Display(Name = "Amended LOA")]
        [StringLength(10)]
        public string AmendedLineItemID { get; set; }

        [Display(Name = "6s Line ID")]
        [StringLength(10)]
        public string LineID6S { get; set; }

        [Display(Name = "FLAIR Amendment ID")]
        [StringLength(10)]
        public string FlairAmendmentID { get; set; }

        [Display(Name = "Amended FLAIR LOA ID")]
        [StringLength(10)]
        public string AmendedFlairLOAID { get; set; }

        [Display(Name = "User Assigned Amendment ID")]
        [StringLength(10)]
        public string UserAssignedID { get; set; }
        [Display(Name = "Last Edited Date")]

        public DateTime LastEditedDate { get; set; }
        [Display(Name = "Originated Date")]

        public DateTime OriginatedDate { get; set; }

        public int LastEditedUserID { get; set; }

        public virtual User LastEditedUser { get; set; }

        [Display(Name ="Originator")]
        public int OriginatorUserID { get; set; }

        [Display(Name = "Originator")]
        public virtual User OriginatorUser { get; set; }

        public byte IsEditable { get; set; }

        public byte IncludesContract { get; set; }

        [Display(Name = "Status")]
        public string CurrentStatus { get; set; }
        public virtual ICollection<LineItem> LineItems { get; set; }
        public virtual ICollection<LineItemGroupStatus> Statuses { get; set; }

        [Display(Name = "Advertised Date")]
        public DateTime? AdvertisedDate { get; set; }

        [Display(Name = "Letting Date")]
        public DateTime? LettingDate { get; set; }
            
    }
}
