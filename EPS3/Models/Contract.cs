using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Helpers;

namespace EPS3.Models
{
    public class Contract
    {
        [Display(Name = "Contract Begin Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Required(ErrorMessage = "Please select a Contract Begin Date")]
        public DateTime BeginningDate { get; set; }
        [Display(Name = "Budget Ceiling")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BudgetCeiling { get; set; }
        [ForeignKey("CompensationID")]
        [Display(Name = "Contract Funding Terms")]
        public int CompensationID { get; set; }
        [Display(Name = "Contract Funding Terms")]
        public virtual Compensation ContractFunding { get; set; }
        [Key]
        [Display(Name = "Contract ID")]
        public int ContractID { get; set; }
        [Display(Name = "Contract Number")]
        [StringLength(5)]
        public string ContractNumber { get; set; }
        
        [Display(Name = "Contract Initial Amount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ContractTotal { get; set; } //Not really the contract total, apparently, based on the display name

        [ForeignKey("ContractTypeID")]
        [Display(Name = "Contract Type")]
        public int ContractTypeID { get; set; }
        [Display(Name = "Contract Type")]
        public virtual ContractType ContractType { get; set; }
        [Display(Name = "Created Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; }
        [Display(Name = "Current Status")]
        public string CurrentStatus { get; set; }
        [Display(Name = "Description of Work")]
        public string DescriptionOfWork { get; set; }
        [Display(Name = "Contract End Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        [Required(ErrorMessage = "Please select a Contract End Date")]
        public DateTime EndingDate { get; set; }
        [Display(Name = "Is Contract Renewable?")]
        public byte IsRenewable { get; set; }
        [Display(Name = "Maximum LOA Amount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxLoaAmount { get; set; }
        [Display(Name = "Modified Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ModifiedDate { get; set; }
        [ForeignKey("ProcurementID")]
        [Display(Name = "Procurement")]
        public int ProcurementID { get; set; }
        [Display(Name = "Procurement")]
        public virtual Procurement MethodOfProcurement { get; set; }
        [ForeignKey("RecipientID")]
        [Display(Name = "Recipient")]
        public int RecipientID { get; set; }
        [Display(Name = "Recipient")]
        public virtual Recipient Recipient { get; set; }
        [ForeignKey("UserID")]
        [Display(Name = "User")]
        public int UserID { get; set; }
        public virtual User User { get; set; }
        [Display(Name = "Service End Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        //[Required(ErrorMessage = "Please select a Service End Date")]
        public DateTime? ServiceEndingDate { get; set; }
        [Required]
        [ForeignKey("VendorID")]
        [Display(Name = "Vendor")]
        public int VendorID { get; set; }
        [Display(Name = "Vendor")]
        public virtual Vendor Vendor { get; set; }
        [Display(Name = "Line Items")]
        public virtual ICollection<LineItem> LineItems { get; set; }

        // These are properties, not methods, so they can be accessed in the Views 
        public string BudgetCeilingString { get { return Utils.FormatCurrency(BudgetCeiling); } }
        public string MaxLoaAmountString { get { return Utils.FormatCurrency(MaxLoaAmount); } }
        public string ContractTotalString{ get { return Utils.FormatCurrency(ContractTotal); } }
    }
}
