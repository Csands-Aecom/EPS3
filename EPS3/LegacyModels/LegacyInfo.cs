using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.LegacyModels
{
    public class LegacyInfo
    {
        [Key]
        public int recID { get; set; }
        public int workID { get; set; }
        public string rush { get; set; }
        public DateTime neededByDate { get; set; }
        public string reasonForRush { get; set; }
        public DateTime lettingDate { get; set; }
        public Int16 advertise { get; set; }
        public Int16 award { get; set; }
        public Int16 LOA { get; set; }
        public Int16 suplemental { get; set; }
        public Int16 renewal { get; set; }
        public Int16 correction { get; set; }
        public Int16 newForm { get; set; }
        public Int16 overrun { get; set; }
        public Int16 SettlementAgreement { get; set; }
        public Int16 fastResponse { get; set; }
        public Int16 emergency { get; set; }
        public Int16 amendment { get; set; }
        public string amendmentNumber { get; set; }
        public string LOAamendedNumber { get; set; }
        public string description { get; set; }
        public string loaID { get; set; }
        public string suplementalID { get; set; }
        public string renewalID { get; set; }
        public string originator { get; set; }
        public string dateSent { get; set; }
        public string phSC { get; set; }
        public string userEmail { get; set; }
        public string contractNumber { get; set; }
    }
}
