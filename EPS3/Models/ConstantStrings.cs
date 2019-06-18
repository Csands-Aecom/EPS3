using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPS3.Models
{
    public static class ConstantStrings
    {

        public const string TPKMailbox = "TPK.EncumbranceRequest@dot.state.fl.us";
        //static class: no constructor
        //Contract Status values
        public const string ContractNew = "New Contract";
        public const string ContractDrafted = "Contract is in Draft";
        public const string ContractInFinance = "Contract Submitted to Finance";
        public const string ContractInWP = "Contract sent to Work Program";
        public const string ContractInCFM = "Contract has been input to CFM";
        public const string ContractRequest50 = "Request Close 50";
        public const string ContractRequest52 = "Request Close 52";
        public const string ContractRequest98 = "Request Close 98";
        public const string ContractComplete50 = "Closed 50";
        public const string ContractComplete52 = "Closed 52";
        public const string ContractComplete98 = "Closed 98";
        public const string ContractArchived = "Contract has been archived";
        public const string Closed = "Closed";

        // Encumbrance Status values
        public const string Draft = "Draft";
        public const string SubmittedFinance = "Finance";
        public const string SubmittedWP = "Work Program";
        public const string CFMReady = "CFM";
        public const string CFMComplete = "Complete";

        // Roles
        public const string AdminRole = "Admin";
        public const string Originator = "Originator";
        public const string FinanceReviewer = "Finance Reviewer";
        public const string WPReviewer = "WP Reviewer";
        public const string CFMSubmitter = "CFM Submitter";
        public const string Closer = "Closer";
        public const string CloserCC = "CloserCC";

        // status changes
        public const string NoChange = "NoChange";
        public const string DraftToFinance = "DraftToFinance";
        public const string DraftToCFM = "DraftToCFM";
        public const string FinanceToDraft = "FinanceToDraft";
        public const string FinanceToWP = "FinanceToWP";
        public const string FinanceToCFM = "FinanceToCFM";
        public const string FinanceToComplete = "FinanceToComplete";
        public const string WPToFinance = "WPToFinance";
        public const string WPToCFM = "WPToCFM";
        public const string CFMToDraft = "CFMToDraft";
        public const string CFMToFinance = "CFMToFinance";
        public const string CFMToWP = "CFMToWP";
        public const string CFMToComplete = "CFMToComplete";
        public const string CompleteToDraft = "CompleteToDraft";
        public const string RequestClose = "RequestCloseContract";
        public const string CloseContract = "CloseContract";


        // LineItemTypes
        public const string NewContract = "New Contract";
        public const string Advertisement = "Advertisement";
        public const string Award = "Award";
        public const string Amendment = "Amendment";
        public const string Amendment2LOA = "Amendment to LOA";
        public const string Supplemental = "Supplemental";
        public const string LOA = "LOA";
        public const string Renewal = "Renewal";
        public const string Overrun = "Overrun";
        public const string Settlement = "Settlement";
        public const string Correction = "Correction";
        public const string Emergency = "Emergency";
        public const string FastResponse = "Fast Response";
        public const string Close50 = "Close Status 50";
        public const string Close98 = "Close Status 98";
        public static string LookupConstant(string Constant)
        {
            switch (Constant)
            {
                case ("ContractNew"):
                    return ContractNew;
                case ("ContractDrafted"):
                    return ContractDrafted;
                case ("ContractInFinance"):
                    return ContractInFinance;
                case ("ContractInWP"):
                    return ContractInWP;
                case ("ContractInCFM"):
                    return ContractInCFM;
                case ("ContractRequest50"):
                    return ContractRequest50;
                case ("ContractRequest52"):
                    return ContractRequest52;
                case ("ContractRequest98"):
                    return ContractRequest98;
                case ("ContractComplete50"):
                    return ContractComplete50;
                case ("ContractComplete52"):
                    return ContractComplete52;
                case ("ContractComplete98"):
                    return ContractComplete98;
                case ("ContractArchived"):
                    return ContractArchived;
                case ("Draft"):
                    return Draft;
                case ("SubmittedFinance"):
                    return SubmittedFinance;
                case ("SubmittedWP"):
                    return SubmittedWP;
                case ("CFMReady"):
                    return CFMReady;
                case ("CFMComplete"):
                    return CFMComplete;
            }
            return Constant;
        }

        public static List<SelectListItem> GetLineItemTypeList()
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem { Text = "Select A Type", Value = "None" });
            typeList.Add(new SelectListItem { Text = "New Contract", Value = NewContract });
            typeList.Add(new SelectListItem { Text = "Advertisement", Value = Advertisement });
            typeList.Add(new SelectListItem { Text = "Award", Value = Award });
            typeList.Add(new SelectListItem { Text = "Amendment", Value = Amendment });
            typeList.Add(new SelectListItem { Text = "Amendment to LOA", Value = Amendment2LOA });
            typeList.Add(new SelectListItem { Text = "Supplemental", Value = Supplemental });
            typeList.Add(new SelectListItem { Text = "LOA", Value = LOA });
            typeList.Add(new SelectListItem { Text = "Renewal", Value = Renewal });
            typeList.Add(new SelectListItem { Text = "Overrun", Value = Overrun });
            typeList.Add(new SelectListItem { Text = "Settlement", Value = Settlement });
            typeList.Add(new SelectListItem { Text = "Correction", Value = Correction });
            typeList.Add(new SelectListItem { Text = "Fast Response", Value = FastResponse });
            typeList.Add(new SelectListItem { Text = "Close 50", Value = Close50 });
            typeList.Add(new SelectListItem { Text = "Close 98", Value = Close98 });
            // typeList.Add(new SelectListItem { Text = "Emergency", Value = Emergency });
            return typeList;
        }


        public static List<SelectListItem> GetEncumbranceStatusList()
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem { Text = "Select A Status", Value = "None" });
            typeList.Add(new SelectListItem { Text = Draft, Value = Draft });
            typeList.Add(new SelectListItem { Text = SubmittedFinance, Value = SubmittedFinance });
            typeList.Add(new SelectListItem { Text = SubmittedWP, Value = SubmittedWP });
            typeList.Add(new SelectListItem { Text = CFMReady, Value = CFMReady });
            typeList.Add(new SelectListItem { Text = CFMComplete, Value = CFMComplete });
            typeList.Add(new SelectListItem { Text = "Closed", Value = Closed });
            return typeList;
        }

        public static List<SelectListItem> GetRequestCloseList()
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem { Text = "Request Close 50", Value = ContractRequest50 });
            typeList.Add(new SelectListItem { Text = "Request Close 98", Value = ContractRequest98 });
            return typeList;
        }
        public static List<SelectListItem> GetCloseList()
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem { Text = "Closed 50", Value = ContractComplete50 });
            typeList.Add(new SelectListItem { Text = "Closed 98", Value = ContractComplete98 });
            return typeList;
        }

        public static List<SelectListItem> GetLineItemTypeListPlusRequestClose()
        {
            List<SelectListItem> typeList = GetLineItemTypeList();
            typeList.AddRange(GetRequestCloseList());
            return typeList;
        }
        public static List<SelectListItem> GetLineItemTypeListPlusClose()
        {
            List<SelectListItem> typeList = GetLineItemTypeList();
            typeList.AddRange(GetCloseList());
            return typeList;
        }
    }
}