using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class LineItem
    {
        private string _EO;
        public LineItem() { }
        public LineItem(LineItem lineItem)
        {
            this.AmendedLineItemID = lineItem.AmendedLineItemID;
            this.Amount = lineItem.Amount;
            this.CategoryID = lineItem.CategoryID;
            this.Comments = lineItem.Comments;
            this.ContractID = lineItem.ContractID;
            this.ExpansionObject = lineItem.ExpansionObject;
            this.FinancialProjectNumber = lineItem.FinancialProjectNumber;
            this.FiscalYear = lineItem.FiscalYear;
            this.FlairAmendmentID = lineItem.FlairAmendmentID;
            this.FlairObject = lineItem.FlairObject;
            this.FundID = lineItem.FundID;
            this.LineItemGroupID = lineItem.LineItemGroupID;
            this.LineItemType = lineItem.LineItemType;
            this.LineNumber = lineItem.LineNumber;
            this.OCAID = lineItem.OCAID;
            this.OrgCode = lineItem.OrgCode;
            this.StateProgramID = lineItem.StateProgramID;
            this.UserAssignedID = lineItem.UserAssignedID;
            this.WorkActivity = lineItem.WorkActivity;
        }

        [Display(Name = "Corrects FLAIR Amendment ID")]
        [StringLength(10)]
        public string AmendedLineItemID { get; set; }
        [Display(Name = "Amount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Amount { get; set; }
        public String AmountString
        {
            get { return Amount.ToString("#,##0.00"); }
        }
        [ForeignKey("CategoryID")]
        [Display(Name = "Category")]
        [Required]
        public int CategoryID { get; set; }
        [Display(Name = "Category")]
        public virtual Category Category { get; set; }
        [Key]
        public int LineItemID { get; set; }
        public int LineNumber { get; set; }
        [Display(Name = "Line Comments")]
        public string Comments { get; set; }
        [ForeignKey("ContractID")]
        public int ContractID { get; set; }
        public virtual Contract Contract { get; set; }
        [Display(Name = "Expansion Option")]
        [StringLength(2)]
        public string ExpansionObject
        {
            get { return _EO; }
            set { _EO = (value == null) ? null : value.ToUpper(); }
        }
        [Display(Name = "Work Activity")]
        [StringLength(3)]
        public string WorkActivity { get; set; }
        [Display(Name = "Financial Project Number")]
        [StringLength(11)]
        public string FinancialProjectNumber { get; set; }
        [Display(Name = "Fiscal Year")]
        public int FiscalYear { get; set; }
        [Display(Name = "FLAIR Amendment ID")]
        [StringLength(10)]
        public string FlairAmendmentID { get; set; }

        [Display(Name = "6s Line ID")]
        [StringLength(10)]
        public string LineID6S { get; set; }

        [Display(Name = "Object Code")]
        [StringLength(6)]
        public string FlairObject { get; set; }
        [Required]
        [Display(Name ="Fund")]
        public int FundID { get; set; }
        [Display(Name = "Fund")]
        public virtual Fund Fund { get; set; }
        [Display(Name = "Encumbrance Type")]
        public string LineItemType { get; set; }
        [ForeignKey("OCAID")]
        [Display(Name = "OCA")]
        public int OCAID { get; set; }
        [Display(Name ="OCA")]
        public virtual OCA OCA { get; set; }
        [Display(Name = "Organization Code")]
        [StringLength(12)] // 55-plus 9 digit string. Only 9 digits are saved
        // Display with "55" prefix
        public string OrgCode { get; set; }
        [ForeignKey("StateProgramID")]
        [Display(Name = "State Program")]
        public int StateProgramID { get; set; }
        [Display(Name ="State Program")]
        public virtual StateProgram StateProgram { get; set; }

        [Display(Name = "User Assigned Amendment ID")]
        [StringLength(10)]
        public string UserAssignedID { get; set; }
        [Display(Name = "History")]
        public virtual ICollection<LineItemStatus> Statuses { get; set; }
        [Display(Name = "Encumbrance ID")]
        public int LineItemGroupID { get; set; }
        public virtual LineItemGroup LineItemGroup { get; set; }
        public string FiscalYearRange {
            get { return FiscalYear.ToString() + " - " + (FiscalYear + 1).ToString(); }
        }
        public string FormattedFiscalYear()
        {
            // fiscal year is numeric, 4-digit, ending year of a two year range
            int priorYear = FiscalYear - 1;
            int millenium = 2000;
            string formattedFY = priorYear.ToString() + " - " + (FiscalYear - millenium).ToString();
            return formattedFY;
        }

        public LineItem ShallowCopy()
        {
            return (LineItem)this.MemberwiseClone();
        }

    }
}
