using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Helpers;

namespace EPS3.Models
{
    public class Message
    {
        private static byte ISCC_TRUE = 1;
        private static byte ISCC_FALSE = 0;
        [Key]
        public int MessageID { get; set; }
        [Display(Name = "Subject")]
        public string Subject { get; set; }
        [Display(Name = "Message")]
        public string Body { get; set; }
        [Display(Name = "Sent On")]
        public DateTime MessageDate { get; set; }
        public byte IsRead { get; set; }
        [ForeignKey("FromUserID")]
        [Display(Name = "Sent By")]
        public int FromUserID { get; set; }
        [Display(Name = "Sent By")]
        public virtual User FromUser { get; set; }

        [Display(Name = "Sent To")]
        public  List<MessageRecipient> Recipients { get; set; }
        //[Display(Name = "Sent To")]
        //public virtual List<MessageRecipient> Recipients { get; set; }


        //[Display(Name = "CC To")]
        //public virtual List<MessageRecipient> CCs { get; set; }

        public Message()
        {

        }
        public Message(string Subject, string Body, int FromUserID)
        {
            this.Subject = Subject;
            this.Body = Body;
            this.FromUserID = FromUserID;
            this.MessageDate = DateTime.Now;
        }
        public bool HasRecipients()
        {
            return !(Utils.IsNullOrEmpty(Recipients));
        }
        public void AddRecipient(int UserID)
        {
            MessageRecipient recipient = new MessageRecipient(this.MessageID, UserID, ISCC_FALSE);
        }

        public void AddRecipients(IEnumerable<int> recipients)
        {
            foreach (int user in recipients)
            {
                AddRecipient(user);
            }
        }
        public void AddRecipient(User User)
        {
            MessageRecipient recipient = new MessageRecipient(this.MessageID, User.UserID, ISCC_FALSE);
        }

        public void AddRecipients(IEnumerable<User> recipients)
        {
            foreach (User user in recipients)
            {
                AddRecipient(user.UserID);
            }
        }

        public void AddCCs(IEnumerable<int> ccs)
        {
            foreach (int userID in ccs)
            {
                AddCC(userID);
            }
        }
        public void AddCC(User User)
        {
            MessageRecipient cc = new MessageRecipient(this.MessageID, User.UserID, ISCC_TRUE);
        }
        public void AddCC(int userID)
        {
            MessageRecipient cc = new MessageRecipient(this.MessageID, userID, ISCC_TRUE);
        }

        public void AddCCs(IEnumerable<User> ccs)
        {
            foreach (User user in ccs)
            {
                AddCC(user);
            }
        }
        public List<User> GetToUsers()
        {
            List<User> recipients = new List<User>();
            foreach (MessageRecipient recipient in this.Recipients)
            {
                recipients.Add(recipient.User);
            }
            return recipients;
        }
        public List<int> GetToUserIDs()
        {
            List<int> recipientIDs = new List<int>();
            foreach (MessageRecipient recipient in this.Recipients)
            {
                recipientIDs.Add(recipient.UserID);
            }
            return recipientIDs;
        }
        public List<string> GetRecipientNames()
        {
            List<string> recipientNames = new List<string>();
            foreach (MessageRecipient recipient in this.Recipients)
            {
                User user = recipient.User;
                recipientNames.Add(user.FullName);
            }
            return recipientNames;
        }
        public List<string> GetRecipientLoginIDs()
        {
            List<string> recipientNames = new List<string>();
            foreach (MessageRecipient recipient in this.Recipients)
            {
                recipientNames.Add(recipient.User.UserLogin);
            }
            return recipientNames;
        }
        public List<string> GetRecipientEmailAddresses()
        {
            List<string> recipientNames = new List<string>();
            foreach (MessageRecipient recipient in this.Recipients)
            {
                recipientNames.Add(recipient.User.Email);
            }
            return recipientNames;
        }

    }
}
