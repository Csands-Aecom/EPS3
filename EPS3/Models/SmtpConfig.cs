using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class SmtpConfig
    {

        public string Server { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public int Port { get; set; }
    }
}
