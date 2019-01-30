using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.LegacyModels
{
    public class LegacyContract
    {
        [Key]
        public int RecID { get; set; }
        public int WorkID { get; set; }
        public string ContractNumber { get; set; }
        public string ContractType { get; set; }
        public string MethodOfProcurement { get; set; }
        public string MethodOfProcurementIndex { get; set; }
        public string MethodOfComp { get; set; }
        public string MethodOfCompIndex { get; set; }
        public decimal ContractTotal { get; set; }
        public decimal MaxAmtPerLOA { get; set; }
        public Int16 CanContractBeRenewed { get; set; }
        public string VendorName { get; set; }
        public string VendorID { get; set; }
        public Int16 ReceivedYes { get; set; }
        public Int16 ReceivedNo { get; set; }
        public decimal FedAgmAmt { get; set; }
        public decimal StateFunds { get; set; }
        public decimal LocalFunds { get; set; }
        public DateTime? AgmtBegDate { get; set; }
        public DateTime? AgmtEndDate { get; set; }
        public DateTime? SvcEndDate { get; set; }
        public decimal BudgetCeiling { get; set; }
        public Int16 CanBeRenewedYES { get; set; }
        public Int16 CanBeRenewedNO { get; set; }
        public Int16 AuthorizedToBeginYes { get; set; }
        public Int16 AuthorizedToBeginNo { get; set; }
        public Int16 AlterationsByContractTermsYes { get; set; }
        public Int16 AlterationsByContractTermsNo { get; set; }
        public DateTime? RevisionsDate { get; set; }
        public Int16 ReimbursementByOthersYes { get; set; }
        public Int16 ReimbursementByOthersNo { get; set; }
        public Int16 Closed { get; set; }

        public virtual ICollection<LegacyFinancial> Financials { get; set; }
        public virtual ICollection<LegacyInfo> Info { get; set; }
    }
}
