﻿using System;
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
        public List<MessageRecipient> Recipients { get; set; }

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
            MessageRecipient recipient = new MessageRecipient(this.MessageID, UserID);
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
            MessageRecipient recipient = new MessageRecipient(this.MessageID, User.UserID);
        }

        public void AddRecipients(IEnumerable<User> recipients)
        {
            foreach (User user in recipients)
            {
                AddRecipient(user.UserID);
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