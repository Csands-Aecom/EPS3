using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class EncumbranceComment
    {

        // This class is just to have objects to receive requests from ajax methods
        // The data in these objects should be used to send notifications to users.
        public string status { get; set; }
        public bool receipt { get; set; }
        public bool notify { get; set; }
        public List<int> wpIDs { get; set; }
        public string comments { get; set; }
        public int userID { get; set; }

    }
}
