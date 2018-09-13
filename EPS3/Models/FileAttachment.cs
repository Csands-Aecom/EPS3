using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class FileAttachment
    {
        [Key]
        public int AttachmentID { get; set; }
        //public FileStream Attachment { get; set; }
        public DateTime FileDate { get; set; }
        public string FileName { get; set; }
        public int LineItemID { get; set; }
        public virtual LineItem LineItem { get; set; }
    }
}
