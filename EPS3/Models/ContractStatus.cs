using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class ContractStatus : IStatus
    {
        public ContractStatus() { }
        public ContractStatus(User user, Contract contract, string statusType)
        {
            this.UserID = user.UserID;
            this.User = user;
            this.ContractID = contract.ContractID;
            this.Contract = contract;
            this.CurrentStatus = statusType;
            this.SubmittalDate = DateTime.Now.Date;
        }
        public int ContractID { get; set; }
        public virtual Contract Contract { get; set; }
    }
}
