using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class LineItemComment
    {
        public LineItemComment() { }
        public LineItemComment(int userID, int lineItemID)
        {
            UserID = userID;
            LineItemID = lineItemID;
            SubmittalDate = DateTime.Now;
        }
        public LineItemComment(int userID, int lineItemID, string comment)
        {
            UserID = userID;
            LineItemID = lineItemID;
            SubmittalDate = DateTime.Now;
            Comments = comment;
        }
        [Key]
        public int CommentID { get; set; }
        [ForeignKey("UserID")]
        public int UserID { get; set; }
        public virtual User User { get; set; }
        [Display(Name = "Date")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy hh:mm }", ApplyFormatInEditMode = true)]
        public DateTime SubmittalDate { get; set; }
        [Display(Name = "Comment")]
        public String Comments { get; set; }
        public int LineItemID { get; set; }
        public virtual LineItem LineItem { get; set; }
    }
}
