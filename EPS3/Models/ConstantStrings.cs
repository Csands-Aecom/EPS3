using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class ConstantStrings
    {
        //static class: no constructor

        //public enum StatusType { New, Draft, Submitted, FinanceApproved, BypassWP, WPApproved, CFMReady, CompleteWorkDone, CompleteInvalid, Closed, Deleted }
        public const string NewContract = "New Contract";
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string FinanceApproved = "Finance Approved";
        public const string ByPassWP = "Bypass Work Program";
        public const string WPApproved = "Work Program Approved";
        public const string CFMReady = "Ready for CFM";
        public const string CompleteWorkDone = "Completed: Work Done";
        public const string CompleteInvalid = "Completed: Invalid";
        public const string ClosedContract = "Closed";
        public const string DeletedContract = "Deleted Contract";

        //public enum Roles { Admin, Originator, FinanceReviewer, WPReviewer, CFMSubmitter}
        public const string AdminRole = "Admin";
        public const string Originator = "Originator";
        public const string FinanceReviewer = "FinanceReviewer";
        public const string WPReviewer = "WPReviewer";
        public const string CFMSubmitter = "CFMSubmitter";
    }
}
