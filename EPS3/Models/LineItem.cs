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
        [Display(Name = "Amount")]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Amount { get; set; }
        [ForeignKey("CategoryID")]
        [Display(Name = "Category")]
        public int CategoryID { get; set; }
        [Display(Name = "Category")]
        public virtual Category Category { get; set; }
        [Key]
        public int LineItemID { get; set; }
        [ForeignKey("ContractID")]
        public int ContractID { get; set; }
        public virtual Contract Contract { get; set; }
        [Display(Name = "EO")]
        [StringLength(2)]
        public string ExpansionObject { get; set; }
        [Display(Name = "Work Activity")]
        [StringLength(3)]
        public string WorkActivity { get; set; }
        [Display(Name = "Financial Project Number")]
        [StringLength(11)]
        public string FinancialProjectNumber { get; set; }
        [Display(Name = "Fiscal Year")]
        public int FiscalYear { get; set; }
        [Display(Name = "Object")]
        [StringLength(6)]
        public string FlairObject { get; set; }
        [Display(Name ="Fund")]
        public int FundID { get; set; }
        [Display(Name = "Fund")]
        public virtual Fund Fund { get; set; }
        [ForeignKey("OCAID")]
        [Display(Name = "OCA")]
        public int OCAID { get; set; }
        [Display(Name ="OCA")]
        public OCA OCA { get; set; }
        [Display(Name = "Organization Code")]
        [StringLength(12)] // 55-plus 9 digit string. Only 9 digits are saved
        // Display with "55" prefix
        public string OrgCode { get; set; }
        [ForeignKey("StateProgramID")]
        [Display(Name = "State Program")]
        public int StateProgramID { get; set; }
        [Display(Name ="State Program")]
        public StateProgram StateProgram { get; set; }
        [Display(Name = "History")]
        public virtual ICollection<LineItemStatus> Statuses { get; set; }

        public string FiscalYearRange {
            get { return FiscalYear.ToString() + " - " + (FiscalYear + 1).ToString(); }
        }

    }
}
