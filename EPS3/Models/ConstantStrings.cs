using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EPS3.Models
{
    public static class ConstantStrings
    {
        //static class: no constructor
        //Contract Status values
        public const string ContractNew = "New Contract";
        public const string ContractDrafted = "Contract is in Draft";
        public const string ContractInCFM = "Contract has been input to CFM";
        public const string ContractComplete50 = "Contract is complete with status 50";
        public const string ContractComplete52 = "Contract is complete with status 52";
        public const string ContractComplete98 = "Contract is complete with status 98";
        public const string ContractArchived = "Contract has been archived";

        // Encumbrance Status values
        public const string Draft = "Draft Encumbrance";
        public const string SubmittedFinance = "Encumbrance Submitted to Finance";
        public const string SubmittedWP = "Encumbrance Submitted to Work Program";
        public const string CFMReady = "Encumbrance Ready for CFM Input";
        public const string CFMComplete = "Encumbrance has been Input into CFM";

        // Roles
        public const string AdminRole = "Admin";
        public const string Originator = "Originator";
        public const string FinanceReviewer = "FinanceReviewer";
        public const string WPReviewer = "WPReviewer";
        public const string CFMSubmitter = "CFMSubmitter";

        // status changes
        public const string NoChange = "NoChange";
        public const string DraftToFinance = "DraftToFinance";
        public const string FinanceToDraft = "FinanceToDraft";
        public const string CFMToDraft = "CFMToDraft";
        public const string FinanceToWP = "FinanceToWP";
        public const string WPToCFM = "WPToCFM";


        public static string LookupConstant(string Constant)
        {
            switch (Constant)
            {
                case ("ContractNew"):
                    return ContractNew;
                case ("ContractDrafted"):
                    return ContractDrafted;
                case ("ContractInCFM"):
                    return ContractInCFM;
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
            return null;
        }

        public static List<SelectListItem> GetLineItemTypeList()
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem { Text = "None", Value = "Select A Type" });
            typeList.Add(new SelectListItem { Text = "Amendment", Value = "Amendment" });
            typeList.Add(new SelectListItem { Text = "Supplemental", Value = "Supplemental" });
            typeList.Add(new SelectListItem { Text = "LOA", Value = "LOA" });
            typeList.Add(new SelectListItem { Text = "Renewal", Value = "Renewal" });
            typeList.Add(new SelectListItem { Text = "Overrun", Value = "Overrun" });
            typeList.Add(new SelectListItem { Text = "Settlement", Value = "Settlement" });
            typeList.Add(new SelectListItem { Text = "Correction", Value = "Correction" });
            return typeList;
        }

        public static List<SelectListItem> GetContractStatusList()
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem { Value = "ContractNew", Text = "New Contract" });
            typeList.Add(new SelectListItem { Value = "ContractDrafted", Text = "Contract is in Draft" });
            typeList.Add(new SelectListItem { Value = "ContractInCFM", Text = "Contract has been input to CFM" });
            typeList.Add(new SelectListItem { Value = "ContractComplete50", Text = "Contract is complete with status 50" });
            typeList.Add(new SelectListItem { Value = "ContractComplete52", Text = "Contract is complete with status 52" });
            typeList.Add(new SelectListItem { Value = "ContractComplete98", Text = "Contract is complete with status 98" });
            typeList.Add(new SelectListItem { Value = "ContractArchived", Text = "Contract has been archived" });
            return typeList;
        }
    }
}
