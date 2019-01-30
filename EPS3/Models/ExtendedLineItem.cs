using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class ExtendedLineItem
    {
        public ExtendedLineItem(LineItem lineItem)
        {
            this.LineItemID = lineItem.LineItemID;

            this.LineItemNumber = lineItem.LineNumber.ToString();
            this.Amount = lineItem.Amount.ToString("C", System.Globalization.CultureInfo.CurrentCulture);
            this.FiscalYear = lineItem.FormattedFiscalYear();
            this.OrgCode = "55-" + lineItem.OrgCode;
            this.CategoryName = lineItem.Category.CategorySelector;
            this.CategoryID = lineItem.CategoryID;
            this.FundName = lineItem.Fund.FundSelector;
            this.FundID = lineItem.FundID;
            this.OcaName = lineItem.OCA.OCASelector;
            this.OcaID = lineItem.OCAID;
            this.StateProgramName = lineItem.StateProgram.ProgramSelector;
            this.StateProgramID = lineItem.StateProgramID;

            this.EO = lineItem.ExpansionObject.ToUpper();
            this.FinancialProjectNumber = lineItem.FinancialProjectNumber;
            this.FlairObject = lineItem.FlairObject;
            this.WorkActivity= lineItem.WorkActivity;

            this.Comments = lineItem.Comments == null ? "" : lineItem.Comments;
            this.LineID6S = lineItem.LineID6S == null ? "" : lineItem.LineID6S;
            this.FlairAmendmentID = lineItem.FlairAmendmentID == null ? "" : lineItem.FlairAmendmentID;
            this.UserAssignedID = lineItem.UserAssignedID == null ? "" : lineItem.UserAssignedID;
            this.AmendedLineItemID = lineItem.AmendedLineItemID == null ? "" : lineItem.AmendedLineItemID;
        }
        public int LineItemID { get; set; }
        public string LineItemNumber { get; set; }
        public string Amount { get; set; }
        public string FiscalYear { get; set; }
        public string OrgCode { get; set; }
        public string CategoryName { get; set; }
        public int CategoryID { get; set; }
        public string FundName { get; set; }
        public int FundID { get; set; }
        public string OcaName { get; set; }
        public int OcaID { get; set; }
        public string StateProgramName { get; set; }
        public int StateProgramID { get; set; }
        public string LineID6S { get; set; }
        public string FlairAmendmentID { get; set; }
        public string UserAssignedID { get; set; }
        public string AmendedLineItemID { get; set; }

        public string EO { get; set; }
        public string FinancialProjectNumber { get; set; }
        public string FlairObject { get; set; }
        public string WorkActivity { get; set; }


        public string Comments { get; set; }
    }
}
