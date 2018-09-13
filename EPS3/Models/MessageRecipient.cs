using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Models
{
    public class MessageRecipient
    {
        [Key]
        public int RecipientID { get; set; }
        [ForeignKey("MessageID")]
        public int MessageID { get; set; }
        public virtual Message Message { get; set; }
        [ForeignKey("UserID")]
        public int UserID { get; set; }
        public virtual User User { get; set; }

        public MessageRecipient() { }
        public MessageRecipient(User user)
        {
            this.User = user;
            this.UserID = user.UserID;
        }
        public MessageRecipient(int UserID)
        {
            this.UserID = UserID;
        }
        public MessageRecipient(int MessageID, int UserID)
        {
            this.MessageID = MessageID;
            this.UserID = UserID;
        }
    }
}
