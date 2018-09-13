using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using EPS3.DataContexts;
using EPS3.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;

namespace EPS3.Helpers
{
    public class MessageService
    {
        //private readonly ILogger<MessageService> _logger;
        private readonly EPSContext _context;
        private SmtpConfig _smtpConfig { get; }
        private string _serverpath;

        public MessageService(EPSContext context, SmtpConfig smtpConfig, string url)
        {
            _context = context;
            _smtpConfig = smtpConfig;
            _serverpath = url;
            //_logger = logger;
        }

        public int AddMessage(string updateType, LineItemGroup encumbrance, string comments)
        {
            return AddMessage(updateType, encumbrance, comments, null);
        }
        public int AddMessage(string updateType, LineItemGroup encumbrance, string comments, List<int> otherRecipients)
        {
            encumbrance = GetFullEncumbrance(encumbrance);
            int msgID = 0;
            List<int> recipientIDs = null;
            decimal encumbranceTotal = 0.0M;
            string contractViewURL = _serverpath + "/Contracts/View/" + encumbrance.ContractID + "/enc_" + encumbrance.GroupID;
            foreach (LineItem item in encumbrance.LineItems)
            {
                encumbranceTotal += item.Amount;
            }
            Contract contract = _context.Contracts.SingleOrDefault(c => c.ContractID == encumbrance.ContractID);
            User submitter = _context.Users.SingleOrDefault(u => u.UserID == encumbrance.LastEditedUserID);
            Message msg = new Message
            {
                FromUserID = encumbrance.LastEditedUserID,
                MessageDate = DateTime.Now
            };

            if (!updateType.Equals(ConstantStrings.NoChange))
            {
                
                switch (updateType)
                {
                    case ConstantStrings.DraftToFinance:
                        msg.Subject = "Encumbrance Request ID " +  encumbrance.GroupID +  " for contract " + contract.ContractNumber + " has been submitted for Finance Review";
                        msg.Body = "<p>Please process the following encumbrance request: ID " + encumbrance.GroupID + " for contract " + contract.ContractNumber + " in the amount of $" + encumbranceTotal + ".</p>\n";

                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += GetStatusComments(encumbrance);
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "View Contract Page</a>.</p>";
                        recipientIDs = (List<int>) _context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.FinanceReviewer)).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.FinanceToDraft:
                    case ConstantStrings.CFMToDraft:
                        msg.Subject = "Encumbrance ID " + encumbrance.GroupID + " for contract " + contract.ContractNumber + " has been returned.";
                        msg.Body = "<p>Encumbrance ID " + encumbrance.GroupID + " for contract " + contract.ContractNumber + " has been returned for the following reason:</p>\n";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "View Contract Page</a>.</p>";
                        recipientIDs = new List<int> { encumbrance.OriginatorUserID };
                        break;
                    case ConstantStrings.FinanceToWP:
                        msg.Subject = "An encumbrance request for contract " + contract.ContractNumber + " has been submitted for Work Program Review";
                        msg.Body = "<p>" + submitter.FullName + " has approved an encumbrance request under contract " + contract.ContractNumber + ".</p>\n";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "View Contract Page</a>.</p>";
                        recipientIDs = (List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.WPReviewer)).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.WPToCFM:
                        msg.Subject = "An encumbrance request for contract " + contract.ContractNumber + " has been approved in Work Program Review";
                        msg.Body = "<p>" + submitter.FullName + " has approved an encumbrance request in Work Program review under contract " + contract.ContractNumber + ".</p>\n";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "View Contract Page</a>.</p>";
                        recipientIDs = (List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.CFMSubmitter)).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.CFMComplete:
                        msg.Subject = "An encumbrance request for contract " + contract.ContractNumber + " has been input into CFM";
                        msg.Body = "<p>" + submitter.FullName + " has completed an encumbrance request in Work Program review under contract " + contract.ContractNumber + ".</p>\n";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>View this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "View Contract Page</a>.</p>";
                        recipientIDs = (List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.FinanceReviewer)).Select(u => u.UserID).ToList();
                        break;
                     default:
                        break;
                }
                // Save the message to the database
                try
                {
                    _context.Messages.Add(msg);
                    _context.SaveChanges();
                    msgID = msg.MessageID;
                    if (otherRecipients != null && otherRecipients.Count > 0)
                    {
                        AddRecipients(msgID, otherRecipients);
                    }
                    else
                    {
                        AddRecipients(msgID, recipientIDs);
                    }
                }catch(Exception e)
                {

                    return -1;
                }
            }
            return msgID ;
        }

        public void AddRecipients(int msgID, IEnumerable<User> recipients)
        {
            foreach(User user in recipients)
            {
                if (user.ReceiveEmails > 0)
                {
                    MessageRecipient msgRecipient = new MessageRecipient
                    {
                        MessageID = msgID,
                        UserID = user.UserID
                    };
                    _context.MessageRecipients.Add(msgRecipient);
                    _context.SaveChanges();
                }
            }
        }

        public void AddRecipients(int msgID, IEnumerable<int> recipientIDs)
        {
            List<User> users = _context.Users.Where(Utils.BuildOrExpression<User, int>(u => u.UserID, recipientIDs.ToArray<int>())).ToList();
            AddRecipients(msgID, users);
        }

        public void SendEmailMessage(int msgID)
        {
            if (msgID == 0) { return; }
            Message msg = _context.Messages.AsNoTracking()
                .Include(m => m.FromUser)
                .Include(m => m.Recipients)
                .SingleOrDefault(m => m.MessageID == msgID);
            List<User> recipients = new List<User>();
            foreach(MessageRecipient recip in msg.Recipients)
            {
                User user = _context.Users.AsNoTracking()
                    .SingleOrDefault(u => u.UserID == recip.UserID);
                if (user.Email != null && user.Email.Length > 0 && user.ReceiveEmails > 0)
                {
                    recipients.Add(user);
                }
            }
            SendMail(msg, recipients);
        }

        public void SendErrorNotification(string errorInfo)
        {
            List<MessageRecipient> adminRecipients = new List<MessageRecipient>();
            List<User> AdminUsers = _context.Users
                .Include(u => u.Roles.Where(r => r.Role == ConstantStrings.AdminRole))
                .ToList();
            //foreach (User admin in AdminUsers)
            //{
            //    MessageRecipient recip = new MessageRecipient(admin);
            //    adminRecipients.Add(recip);
            //}
            User chrissands = _context.Users.AsNoTracking().SingleOrDefault(u => u.UserLogin.Equals("KNAECCS"));
            adminRecipients.Add(new MessageRecipient(chrissands));
            Message msg = new Message()
            {
                FromUser = new User()
                {
                    FirstName = "Application",
                    LastName = "Error",
                    Email = "noreply@dot.state.fl.us",
                    Phone = "999-999-9999",
                    ReceiveEmails = 0,
                    UserLogin = "ERROR"
                },
                Subject = "[EPS2 ERROR]: An error has been reported in the EPS 2.0. application",
                Body = "An error was recorded in the application: " + errorInfo,
                MessageDate = DateTime.Now
            };
        }
        

        public void SendMail(Message msg, List<User> recipients)
        {
            try
            {
                MailAddress sender = new MailAddress(msg.FromUser.Email, msg.FromUser.FullName);
                MailMessage mail = new MailMessage(){
                    From = sender,
                    Subject = "[EPS Test]: " + msg.Subject,
                    Body = msg.Body,
                    IsBodyHtml = true
                };
                foreach (User user in recipients)
                {
                    mail.To.Add(new MailAddress(user.Email, user.FullName));
                }
                //mail.To.Add(new MailAddress("chris.sands@aecom.com", "Chris Sands"));
                SmtpClient client = new SmtpClient
                {
                    Port = _smtpConfig.Port,   // Turnpike: 25, Aecom: 23
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Host = _smtpConfig.Server// Turnpike: 156.75.163.42 Aecom: 192.29.28.71
                };

                client.Send(mail);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex.StackTrace);
                Console.Write(ex.StackTrace);
                //throw ex;
            }
        }
        public LineItemGroup GetFullEncumbrance(LineItemGroup encumbrance)
        {
            // ensure the encumbrance includes all of its LineItems and their comments.
            LineItemGroup lineItemGroup = _context.LineItemGroups.AsNoTracking()
                .Include(e => e.LineItems).ThenInclude(li => li.Statuses)
                .SingleOrDefault(e => e.GroupID == encumbrance.GroupID);
            return lineItemGroup;
        }

        public string GetStatusComments(LineItemGroup encumbrance)
        {
            string result = "";
            foreach (LineItem item in encumbrance.LineItems)
            {
                result += "<p>Line Number:" + item.LineNumber + "</p>";
                int commentCounter = 0;
                foreach (LineItemStatus status in item.Statuses)
                {
                    if (status.Comments != null && status.Comments.Length > 0)
                    {
                        commentCounter++;
                        result += "<p>Comment " + commentCounter.ToString() + ": " + status.Comments + "</p>";
                    }
                }
            }
            return result;
        }
    }
}
