using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    class ToDoRequest
    {
        public string status { get; set; }
        public string userID { get; set; }
        public ToDoRequest(string status, string userID)
        {
            this.status = status;
            this.userID = userID;
        }
    }
}
