using EPS3.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class ExtendedContract 
    {
        public ExtendedContract(Contract contract)
        {
            this.ContractID = contract.ContractID;
            this.ContractNumber = contract.ContractNumber;
            if (contract.ContractFunding != null)
            {
                this.CompensationName = contract.ContractFunding.CompensationSelector;
            }
            if(contract.ContractType != null)
            {
                this.ContractTypeName = contract.ContractType.ContractTypeSelector;
            }
            if(contract.CreatedDate != null)
            {
                this.CreatedDate = contract.CreatedDate.ToString("MM/dd/yyyy");
            }
            if(contract.ModifiedDate != null)
            {
                this.ModifiedDate = ((DateTime) contract.ModifiedDate).ToString("MM/dd/yyyy");
            }
            if(contract.MethodOfProcurement != null)
            {
                this.ProcurementName = contract.MethodOfProcurement.ProcurementSelector;
            }
            if(contract.Recipient != null)
            {
                this.RecipientName = contract.Recipient.RecipientSelector;
            }
            if(contract.User != null)
            {
                this.OriginatorName = contract.User.FullName;
                this.OriginatorEmail = contract.User.Email;
                this.OriginatorLogin = contract.User.UserLogin;
                this.OriginatorPhone = contract.User.Phone;
            }
            if (contract.Vendor != null)
            {
                this.VendorName = contract.Vendor.VendorSelector;
            }
            if (contract.BeginningDate != null)
            {
                this.FormattedBeginningDate = contract.BeginningDate.ToString("MM/dd/yyyy");
            }
            if(contract.DescriptionOfWork != null)
            {
                this.DescriptionOfWork = contract.DescriptionOfWork;
            }
            if (contract.EndingDate != null)
            {
                this.FormattedEndingDate = contract.EndingDate.ToString("MM/dd/yyyy");
            }
            if (contract.ServiceEndingDate != null && contract.ServiceEndingDate > new DateTime(2000, 01, 01))
            {
                this.FormattedServiceEndingDate = contract.ServiceEndingDate?.ToString("MM/dd/yyyy");
            }
            else
            {
                this.FormattedServiceEndingDate = "none";
            }

            this.FormattedContractInitialAmount = Utils.FormatCurrency(contract.ContractTotal);

            this.FormattedMaxLoaAmount = Utils.FormatCurrency(contract.MaxLoaAmount);
            this.BudgetCeiling = contract.BudgetCeiling;

            this.FormattedBudgetCeiling = Utils.FormatCurrency(contract.BudgetCeiling);

            if (contract.IsRenewable >0)
            {
                this.ContractRenewable = "Yes";
            }
            else
            {
                this.ContractRenewable = "No";
            }

            this.CurrentStatus = contract.CurrentStatus;
        }

        //This is a class for Contract used to pass child properties in a JSON string
        public int ContractID { get; set; }
        public string ContractNumber { get; set; }
        public decimal BudgetCeiling { get; set; }
        public string CompensationName { get; set; }
        public string ContractTypeName { get; set; }
        public string CreatedDate { get; set; }
        public string ModifiedDate { get; set; }
        public string DescriptionOfWork { get; set; }
        public string OriginatorName { get; set; }
        public string OriginatorEmail { get; set; }
        public string OriginatorPhone { get; set; }
        public string OriginatorLogin { get; set; }

        public string ProcurementName { get; set; }
        public string RecipientName { get; set; }
        public string VendorName { get; set; }

        public string FormattedBeginningDate { get; set; }
        public string FormattedEndingDate { get; set; }
        public string FormattedServiceEndingDate { get; set; }
        public string FormattedContractInitialAmount { get; set; }
        public string FormattedMaxLoaAmount { get; set; }
        public string FormattedBudgetCeiling { get; set; }
        public string ContractRenewable { get; set; }

        public string CurrentStatus { get; set; }
    }
}
