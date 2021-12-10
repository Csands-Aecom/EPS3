using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using EPS3.DataContexts;
using EPS3.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace EPS3.Helpers
{
    public class MessageService
    {
        //private readonly ILogger<MessageService> _logger;
        private readonly EPSContext _context;
        private SmtpConfig _smtpConfig { get; }
        private string _serverpath;
        private PermissionsUtils _pu;
        private readonly ILogger _logger;

        public MessageService(EPSContext context, SmtpConfig smtpConfig, ILogger callingLogger, string url)
        {
            _context = context;
            _smtpConfig = smtpConfig;
            _serverpath = url;
            _logger = callingLogger;
            _pu = new PermissionsUtils(_context, _logger);
            //_logger = logger;
        }

        public int AddMessage(string updateType, LineItemGroup encumbrance, string comments)
        {
            return AddMessage(updateType, encumbrance, comments, null);
        }

        public int AddMessage(string updateType, LineItemGroup encumbrance, string comments, List<int> otherRecipients)
        {
           return AddMessage(updateType, encumbrance, comments, otherRecipients, null);
        }

        public int AddMessage(string updateType, LineItemGroup encumbrance, string comments, List<int> otherRecipients, List<int> ccIDs)
        {
            encumbrance = _context.GetDeepEncumbrance(encumbrance.GroupID);
            int msgID = 0;
            List<int> recipientIDs = null;  // list of IDs of email recipients
            decimal encumbranceTotal = 0.0M;
            //string contractViewURL = _serverpath + "/Contracts/View/" + encumbrance.ContractID + "/enc_" + encumbrance.GroupID;
            string contractViewURL = _serverpath + "/LineItemGroups/Manage/" + encumbrance.GroupID;
            encumbranceTotal = GetEncumbranceTotal(encumbrance);
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
                        msg.Subject = "Encumbrance Request# " + encumbrance.GroupID + " for contract " + contract.ContractNumber + " has been submitted for Finance Review";
                        msg.Body = "<p>Please process the following encumbrance request: ID " + encumbrance.GroupID + " for contract " + contract.ContractNumber + " in the amount of " + Utils.FormatCurrency(encumbranceTotal) + ".</p>\n";
                        if (comments != null && comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        if(encumbrance.FileAttachments != null && encumbrance.FileAttachments.Count > 0)
                        {
                            msg.Body += "File Attachments:<br/><ul>";
                            foreach(FileAttachment fileAtt in encumbrance.FileAttachments)
                            {
                                var fileUrl = _serverpath + "\\" + FileAttachment.UserFilesPath + "\\" + fileAtt.FileName;
                                msg.Body += "<li><a href='" + fileUrl + "'>" + fileAtt.DisplayName + "</a></li>";
                            }
                            msg.Body += "</ul>";
                        }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        // Send only to TPK Encumbrance mailbox
                        recipientIDs = getFinanceRecipients();
                        break;
                    case ConstantStrings.DraftToCFM:
                        msg.Subject = "Encumbrance Request# " + encumbrance.GroupID + " for contract " + contract.ContractNumber + " for " + encumbrance.LineItemType;
                        msg.Body = "<p>Please input the following encumbrance into CFM: request ID " + encumbrance.GroupID + " for contract " + contract.ContractNumber + ".</p>\n";
                        //msg.Body += "in the amount of " + Utils.FormatCurrency(encumbranceTotal)  + " applied to Amendment " + encumbrance.FlairAmendmentID + " Line " + encumbrance.LineID6S + ".";
                        if (encumbrance.LineItems != null && encumbrance.LineItems.Count > 0) {
                            string tblText = "<table><tr><th>Contract</th><th>Amendment</th><th>Line (6s)</th><th>Amount</th></tr>";
                            foreach (LineItem item in encumbrance.LineItems)
                            {
                                tblText += "<tr><td>" + contract.ContractNumber + "</td><td>" + item.FlairAmendmentID + "</td><td>" + item.LineID6S + "</td><td>" + Utils.FormatCurrency(item.Amount) + "</td></tr>";
                            }
                            tblText += "</table> <br/>";
                            msg.Body += tblText;
                        }
                        if (comments != null && comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>View this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        // Send only to TPK Encumbrance mailbox
                        recipientIDs = (List<int>)_context.Users.Where(u => u.Email == ConstantStrings.TPKMailbox).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.FinanceToDraft:
                    case ConstantStrings.CFMToDraft:
                    case ConstantStrings.CompleteToDraft:
                        msg.Subject = "Encumbrance Request#" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " has been returned to the Originator";
                        msg.Body = "<p>Encumbrance ID " + encumbrance.GroupID + " for contract " + contract.ContractNumber + " has been returned for the following reason:</p>\n";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        recipientIDs = new List<int> { encumbrance.OriginatorUserID };
                        break;
                    case ConstantStrings.FinanceToWP:
                        msg.Subject = "Please review encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " for Work Program Evaluation";
                        msg.Body = "<p>" + submitter.FullName + " has completed a Finance Review for encumbrance request #" + encumbrance.GroupID + " under contract " + contract.ContractNumber + ".</p>\n";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        recipientIDs = otherRecipients; //(List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.WPReviewer)).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.FinanceToCFM:
                    case ConstantStrings.FinanceToComplete:
                        // No notification required. Exit without sending message
                        return 0;
                    case ConstantStrings.WPToFinance:
                        msg.Subject = "Encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " has been returned by Work Program";
                        msg.Body = "<p>" + submitter.FullName + " has completed a Work Program review for encumbrance request #" + encumbrance.GroupID + " under contract " + contract.ContractNumber + ".</p>\n";
                        msg.Body += "<p>This encumbrance request is returned to Finance with the following comment:</p>";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        // Send only to TPK Encumbrance mailbox
                        recipientIDs = (List<int>)_context.Users.Where(u => u.Email == ConstantStrings.TPKMailbox).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.WPToCFM:
                        msg.Subject = "Encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " is ready for CFM Input";
                        msg.Body = "<p>" + submitter.FullName + " has completed a Work Program review for encumbrance request #" + encumbrance.GroupID + " in Work Program review under contract " + contract.ContractNumber + ".</p>\n";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        // Send only to TPK Encumbrance mailbox
                        recipientIDs = (List<int>)_context.Users.Where(u => u.Email == ConstantStrings.TPKMailbox).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.CFMToFinance:
                        msg.Subject = "Encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " has been returned to Finance";
                        msg.Body = "<p>" + submitter.FullName + " has returned to Encumbranc request #" + encumbrance.GroupID + " to Finance with the following comment:</p>";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        // Send only to TPK Encumbrance mailbox
                        recipientIDs = (List<int>)_context.Users.Where(u => u.Email == ConstantStrings.TPKMailbox).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.CFMToWP:
                        msg.Subject = "Please review encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " requires additional Work Program Review";
                        msg.Body = "<p>" + submitter.FullName + " has returned encumbrance request #" + encumbrance.GroupID + " from CFM for additional Work Program review for contract " + contract.ContractNumber + ".</p>\n";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        recipientIDs = otherRecipients;;
                        break;
                    case ConstantStrings.CFMToComplete:
                        //msg.Subject = "Encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " has been input into CFM";
                        //msg.Body = "<p>" + submitter.FullName + " has input encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " into CFM.</p>\n";
                        //if (comments.Length > 0)
                        //{ msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        //msg.Body += "<p>No further action is required. You may view this encumbrance request in the <a href='" + contractViewURL + "'>" +
                        //    "EPS Application</a>.</p>";
                        //// Send only to TPK Encumbrance mailbox
                        //recipientIDs = new List<int> { submitter.UserID };
                        // No notification needed per Lorna 7/9/2019
                        break;
                    case ConstantStrings.CloseContract:
                        msg.Subject = "Request to Close Contract #" + contract.ContractNumber;
                        msg.Body = "<p>" + submitter.FullName + " requests closure of the contract " + contract.ContractNumber + ", closure type " + encumbrance.LineItemType + " </p>";
                        //msg.Body += "<p>Review this closure request in the <a href='" + contractViewURL + "'>" + "EPS Application</a>.</p>";
                        recipientIDs = (List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.Closer)).Select(u => u.UserID).ToList();
                        ccIDs = (List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.CloserCC)).Select(u => u.UserID).ToList();
                        break;
                    default:
                        // if no message then exit
                        return 0;
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
                    if(ccIDs != null && ccIDs.Count > 0)
                    {
                        AddCCs(msgID, ccIDs);
                    }
                }catch(Exception e)
                {
                    Log.Error("MessageService.AddMessage Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                    return -1;
                }
            }
            return msgID ;
        }


        public bool SendReceipt(LineItemGroup encumbrance, User submitter, string comments)
        {
            int msgID = AddReceipt(encumbrance, submitter, comments);
            if (submitter.CanReceiveEmails() && submitter.IsDisabled==0)
            {
                SendEmailMessage(msgID);
                return true;
            }
            return false;
        }
        public int AddReceipt(LineItemGroup encumbrance, User submitter, string comments)
        {
            Message msg = new Message
            {
                FromUserID = encumbrance.LastEditedUserID,
                MessageDate = DateTime.Now
            };
            msg.Subject = "Encumbrance Request ID " + encumbrance.GroupID; // + " for contract " + contract.ContractNumber
            msg.Body = GetOriginatorReceipt(encumbrance);
            msg.MessageDate = DateTime.Now;
            msg.FromUserID = submitter.UserID;
            // Save the message to the database
            try
            {
                _context.Messages.Add(msg);
                _context.SaveChanges();
                AddRecipient(msg.MessageID, submitter);
                return msg.MessageID;
            }
            catch (Exception e)
            {
                Log.Error("MessageService.SendReceipt Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                return -1;
            }
        }

        public string GetOriginatorReceipt(LineItemGroup encumbrance)
        {
            Contract contract = _context.Contracts.SingleOrDefault(c => c.ContractID == encumbrance.ContractID);
            return GetOriginatorReceipt(contract, encumbrance);
        }

        public string GetOriginatorReceipt(Contract contract, LineItemGroup encumbrance)
        {
            string body = "<p>You have submitted the following encumbrance request for Finance Review in the Encumbrance Processing System application.</p><br\\>";
            string contractInfo = "";
            string encumbranceInfo = "";
            string linesInfo = "";
            string url = _serverpath + "/LineItemGroups/Manage/" + encumbrance.GroupID;
            if (this.IsShallowContract(contract))
            {
                contract = _context.GetDeepContract(contract.ContractID);
            }
            if (encumbrance == null)
            {
                return null;
            }
            else
            {
                if (this.IsShallowEncumbrance(encumbrance))
                {
                    encumbrance = _context.GetDeepEncumbrance(encumbrance.GroupID);
                }
                decimal encumbranceTotal = GetEncumbranceTotal(encumbrance);
                encumbranceInfo = GetEncumbranceInfo(encumbrance, encumbranceTotal);
                if (contract == null)
                {
                    contract = _context.GetDeepContract(encumbrance.ContractID);
                }

                // if encumbrance is populated, start with a row for contract, one row for encumbrance and include all associated Line Items
                if (encumbrance.LineItemType.Equals(ConstantStrings.NewContract) || 
                    encumbrance.LineItemType.Equals(ConstantStrings.Advertisement) || 
                    encumbrance.LineItemType.Equals(ConstantStrings.Award))
                {
                    contractInfo = GetContractInfo(contract);
                }
                else
                {
                    contractInfo += "<strong>Contract: </strong>" + contract.ContractNumber + "<br/>";
                    contractInfo += "<strong>Contract Initial Amount:</strong> " + Utils.FormatCurrency(contract.ContractTotal) + "<br/>";
                    //TODO: What other contract information should be included in the email receipt?
                }
                List<LineItem> lineItems = _context.GetDeepLineItems(encumbrance.GroupID);
                if (lineItems == null || lineItems.Count == 0)
                {
                    linesInfo += "There are no line items associated with this encumbrance.";
                }
                else
                {
                    linesInfo += GetLineItemsInfo(lineItems);
                }
            }
            if (encumbrance.FileAttachments != null && encumbrance.FileAttachments.Count > 0)
            {
                body += "File Attachments:<br/><ul>";
                foreach (FileAttachment fileAtt in encumbrance.FileAttachments)
                {
                    var fileUrl = _serverpath + "\\" + FileAttachment.UserFilesPath + "\\" + fileAtt.FileName;
                    body += "<li><a href='" + fileUrl + "'>" + fileAtt.DisplayName + "</a></li>";
                }
                body += "</ul>";
            }
            body += contractInfo + "<br/>" + encumbranceInfo + "<br/>" + linesInfo + "<br/><br/>";
            body += "You can review your encumbrance request at <a href='" + url +"'> Encumbrance " + encumbrance.GroupID + "</a>";
            return body;
        }

        private List<int> getFinanceRecipients()
        {
            return (List<int>)_context.Users.Where(u => u.Email == ConstantStrings.TPKMailbox).Select(u => u.UserID).ToList();

        }
        public void AddRecipients(int msgID, IEnumerable<User> recipients)
        {
            foreach(User user in recipients)
            {
                AddRecipient(msgID, user);
            }
        }
        public void AddRecipient(int msgID, User user)
        {
            if (user.ReceiveEmails > 0)
            {
                MessageRecipient msgRecipient = new MessageRecipient
                {
                    MessageID = msgID,
                    UserID = user.UserID,
                    IsCC = 0
                };
                _context.MessageRecipients.Add(msgRecipient);
                _context.SaveChanges();
            }
        }

        public void AddRecipients(int msgID, IEnumerable<int> recipientIDs)
        {
            List<User> users = _context.Users.Where(Utils.BuildOrExpression<User, int>(u => u.UserID, recipientIDs.ToArray<int>())).ToList();
            AddRecipients(msgID, users);
        }
        public void AddCCs(int msgID, IEnumerable<int> ccIDs)
        {
            foreach (int userID in ccIDs)
            {
                AddCC(msgID, userID);
            }
        }
        public void AddCCs(int msgID, IEnumerable<User> ccList)
        {
            foreach (User u in ccList) {
                AddCC(msgID, u.UserID);
            }
        }

        public void AddCC(int msgID, int userID)
        {
            try
            {
                MessageRecipient ccrecip = new MessageRecipient
                {
                    MessageID = msgID,
                    UserID = userID,
                    IsCC = 1
                };
                _context.MessageRecipients.Add(ccrecip);
                _context.SaveChanges();
            }catch(Exception e)
            {
                Log.Error("MessageService.AddCC Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
        }
        public void SendClosingRequest(ContractClosure closure, User submitter)
        {
            int closeStatus = closure.ClosureType.Contains("50") ? 50 : 98;
            Contract contract = (Contract)_context.Contracts.AsNoTracking().Include(c => c.Vendor)
                .SingleOrDefault(c => c.ContractID == closure.ContractID);
            Message msg = new Message
            {
                FromUserID = submitter.UserID,
                MessageDate = DateTime.Now
            };
            //ToDo add IDs
            if (closure.ContractOrEncumbrance.Equals("Contract")) {
                msg.Subject = "Please Close Contract " + contract.ContractNumber + ".";
                msg.Body = "Please place in status " + closeStatus.ToString() + ".<br/>" +
                            "Contract Number: " + contract.ContractNumber + " Vendor: " + contract.Vendor.VendorName + "<br /><br />" +
                            "<p><strong>I certify that the amounts being released are not required for current and future obligations.</strong></p>";
            } else {
                msg.Subject = "Please Close Amendment " + closure.FlairID + ".";
                msg.Body = "Please place in status " + closeStatus.ToString() + ".<br/>";
                msg.Body += "Amendment: " + closure.FlairID + "  Contract Number: " + contract.ContractNumber + "  Vendor: " + contract.Vendor.VendorName;
            }

            if (!String.IsNullOrWhiteSpace(closure.Comments))
            {
                msg.Body += "<br /><br /><strong>Comments:</strong> " + closure.Comments;
            }

            // Save the message to the database
            try
            {
                _context.Messages.Add(msg);
                _context.SaveChanges();

                //add Closer(s) as recipient(s)
                List<int> recipientIDs = (List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.Closer)).Select(u => u.UserID).ToList();
                AddRecipients(msg.MessageID, recipientIDs);
                //add CloserCC(s) as cc(s)
                List<int> ccIDs = (List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.CloserCC)).Select(u => u.UserID).ToList();
                AddCCs(msg.MessageID, ccIDs);
                SendEmailMessage(msg.MessageID);
            }

            catch (Exception e)
            {
                Log.Error("MessageService.SendReceipt Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
        }

        public void SendEmailMessage(int msgID)
        {
            try
            {
                if (msgID == 0) { return; }
                Message msg = _context.Messages.AsNoTracking()
                    .Include(m => m.FromUser)
                    .SingleOrDefault(m => m.MessageID == msgID);

                List<User> recipients = new List<User>();
                List<User> ccs = new List<User>();
                List<MessageRecipient> msgRecipients = _context.MessageRecipients
                    .AsNoTracking()
                    .Include(m => m.User)
                    .Where(m => m.MessageID == msg.MessageID && m.IsCC == 0)
                    .ToList();
                List<MessageRecipient> msgCCs = _context.MessageRecipients
                    .AsNoTracking()
                    .Include(m => m.User)
                    .Where(m => m.MessageID == msg.MessageID && m.IsCC == 1)
                    .ToList();
                foreach (MessageRecipient recip in msgRecipients)
                {
                    if (recip.User.Email != null && recip.User.Email.Length > 0 && recip.User.IsDisabled==0)
                    {
                        recipients.Add(recip.User);
                    }
                }

                foreach (MessageRecipient cc in msgCCs)
                {
                    if (cc.User.Email != null && cc.User.Email.Length > 0 && cc.User.IsDisabled == 0)
                    {
                        ccs.Add(cc.User);
                    }
                }
                SendMail(msg, recipients, ccs);
            }catch(Exception e)
            {
                Log.Error("MessageService.SendEmailMessage Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
        }

        public void SendErrorNotification(string errorInfo)
        {
            List<User> AdminUsers = _context.Users
                .Include(u => u.Roles.Where(r => r.Role == ConstantStrings.AdminRole))
                .ToList();

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
            _context.Messages.Add(msg);
            _context.SaveChanges();
            msg.AddRecipients(AdminUsers);
            SendEmailMessage(msg.MessageID);
        }
        

        public void SendMail(Message msg, List<User> recipients, List<User> ccs)
        {
            string mailPrefix = "";
            try
            {
                var appSettingsJson = AppSettingsJson.GetAppSettings();
                mailPrefix = appSettingsJson["EPSMailSubjectPrefix"];
            }
            catch (System.IO.FileNotFoundException)
            {
                mailPrefix = "EPS Test";
            }
            try {
                MailAddress sender = new MailAddress(msg.FromUser.Email, msg.FromUser.FullName);
                MailMessage mail = new MailMessage(){
                    From = sender,
                    Subject = "[" + mailPrefix + "]: " + msg.Subject,
                    Body = msg.Body,
                    IsBodyHtml = true
                };
                foreach (User user in recipients)
                {
                    mail.To.Add(new MailAddress(user.Email, user.FullName));
                }
                if (ccs != null)
                {
                    foreach (User user in ccs)
                    {
                        mail.CC.Add(new MailAddress(user.Email, user.FullName));
                    }
                }

                SmtpClient client = new SmtpClient
                {
                    Port = _smtpConfig.Port,   // Turnpike: 25, Aecom: 23
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Host = _smtpConfig.Server// Turnpike: 156.75.163.42 Aecom: 192.29.28.71
                };

                client.Send(mail);
            }
            catch (Exception e)
            {
                //_logger.LogError(ex.StackTrace);
                Log.Error("MessageService.SendEmail Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                Console.Write(e.StackTrace);
                //throw ex;
            }
        }


        public string GetContractInfo(Contract contract)
        {
            // NOTE: This must be a Deep Copy of a contract
            string contractInfo = "";
            contractInfo += "<h3>Contract Information</h3>";
            contractInfo += "<strong>Contract Number:</strong> " + contract.ContractNumber + "<br />";
            contractInfo += "<strong>Contract Type:</strong> " + contract.ContractType.ContractTypeSelector + "<br />";
            string canRenew = contract.IsRenewable > 0 ? "Yes" : "No";
            contractInfo += "<strong>Is Contract Renewable?:</strong> " + canRenew + "<br />";
            contractInfo += "<strong>Contract Initial Amount:</strong> " + Utils.FormatCurrency(contract.ContractTotal) + "<br />";
            contractInfo += "<strong>Maximum LOA Amount:</strong> " + Utils.FormatCurrency(contract.MaxLoaAmount) + "<br />";
            contractInfo += "<strong>Budget Ceiling:</strong> " + Utils.FormatCurrency(contract.BudgetCeiling)  + "<br />";
            contractInfo += "<strong>Contract Begin Date:</strong> " + contract.BeginningDate.ToString("MM/dd/yyyy") + "<br />";
            contractInfo += "<strong>Contract End Date:</strong> " + contract.EndingDate.ToString("MM/dd/yyyy") + "<br />";
            if (contract.ServiceEndingDate != null && contract.ServiceEndingDate > new DateTime(2000, 01, 01))
            {
                contractInfo += "<strong>Service End Date:</strong> " + contract.ServiceEndingDate?.ToString("MM/dd/yyyy") + "<br />";
            }
            contractInfo += "<strong>Procurement:</strong> " + contract.MethodOfProcurement.ProcurementSelector + "<br />";
            contractInfo += "<strong>Contract Funding Terms:</strong> " + contract.ContractFunding.CompensationSelector + "<br />";
            contractInfo += "<strong>Vendor:</strong> " + contract.Vendor.VendorSelector + "<br />";
            contractInfo += "<strong>Recipient:</strong> " + contract.Recipient.RecipientSelector + "<br />";
            contractInfo += "<strong>Description of Work:</strong> " + contract.DescriptionOfWork + "<br />";

            return contractInfo;
        }
        
        public string GetEncumbranceInfo(LineItemGroup encumbrance, decimal encumbranceTotal)
        {
            string encumbranceInfo = "";
            encumbranceInfo += "<strong>Encumbrance Type:</strong> " + encumbrance.LineItemType + "<br />";
            encumbranceInfo += "<strong>Status:</strong> " + encumbrance.CurrentStatus + "<br />";
            encumbranceInfo += "<strong>Encumbrance Total:</strong> " + Utils.FormatCurrency(encumbranceTotal) + "<br />";
            encumbranceInfo += "<strong>Description: </strong> " + encumbrance.Description + "<br />";
            if (encumbrance.LineID6S != null && encumbrance.LineID6S != "")
            {
                encumbranceInfo += "<strong>6s:</strong> " + encumbrance.LineID6S + "<br />";
            }
            if (encumbrance.FlairAmendmentID != null && encumbrance.FlairAmendmentID != "")
            {
                encumbranceInfo += "<strong>Original FLAIR Amendment ID:</strong> " + encumbrance.FlairAmendmentID + "<br />";
            }
            if (encumbrance.UserAssignedID != null && encumbrance.UserAssignedID != "")
            {
                encumbranceInfo += "<strong>User Assigned ID:</strong> " + encumbrance.UserAssignedID + "<br />";
            }
            if (encumbrance.AmendedLineItemID != null && encumbrance.AmendedLineItemID != "")
            {
                encumbranceInfo += "<strong>Amended LOA:</strong> " + encumbrance.AmendedLineItemID + "<br />";
            }
            if (encumbrance.AmendedFlairLOAID != null && encumbrance.AmendedFlairLOAID != "")
            {
                encumbranceInfo += "<strong>Amended/Corrected FLAIR ID:</strong> " + encumbrance.AmendedFlairLOAID + "<br />";
            }
            if (encumbrance.AdvertisedDate != null)
            {
                encumbranceInfo += "<strong>Advertised Date:</strong> " + String.Format("{0:MM/dd/yyyy}", encumbrance.AdvertisedDate) + "<br />";
            }
            if (encumbrance.LettingDate != null)
            {
                encumbranceInfo += "<strong>Letting Date:</strong> " + String.Format("{0:MM/dd/yyyy}", encumbrance.LettingDate) + "<br />";
            }
            if (encumbrance.RenewalDate != null)
            {
                encumbranceInfo += "<strong>Renewal Date:</strong> " + String.Format("{0:MM/dd/yyyy}", encumbrance.RenewalDate) + "<br />";
            }
            encumbranceInfo += "<strong>Last Updated:</strong> " + String.Format("{0:MM/dd/yyyy HH:mm}", encumbrance.LastEditedDate) + " by " + encumbrance.LastEditedUser.FirstName + " " + encumbrance.LastEditedUser.LastName + "<br />";

            return encumbranceInfo;
        }

        public string GetLineItemsInfo(List<LineItem> lineItems)
        {
            string linesInfo = "";
            linesInfo += "<table><thead><tr><th>Order</th><th>Line</th><th>Financial Project Number</th><th>Fiscal Year</th><th>Fund</th><th>Organization Code</th><th>Category</th><th>Object Code</th><th>Work Activity</th><th>OCA</th><th>State Program</th><th>EO</th><th>Amount</th></tr></thead><tbody>";
            foreach (LineItem item in lineItems)
            {
                linesInfo += "<tr>";
                linesInfo += "<td>" + item.LineNumber + "</td>";
                linesInfo += "<td>" + item.LineItemID + "</td>";
                linesInfo += "<td>" + item.FinancialProjectNumber + "</td>";
                linesInfo += "<td>" + item.FiscalYearRange + "</td>";
                linesInfo += "<td>" + item.Fund.FundCode + "</td>";
                linesInfo += "<td>55-" + item.OrgCode + "</td>";
                linesInfo += "<td>" + item.Category.CategoryCode + "</td>";
                linesInfo += "<td>" + item.FlairObject + "</td>";
                linesInfo += "<td>" + item.WorkActivity + "</td>";
                linesInfo += "<td>" + item.OCA.OCACode + "</td>";
                linesInfo += "<td>" + item.StateProgram.ProgramCode + "</td>";
                linesInfo += "<td>" + item.ExpansionObject + "</td>";
                linesInfo += "<td>" + Utils.FormatCurrency(item.Amount) + "</td>";
                linesInfo += "</tr>";
                if(item.Comments != null && item.Comments.Trim().Length > 0)
                {
                    linesInfo += "<tr><td colspan=13><strong>Comments:</strong>" + item.Comments + "</td></tr>";
                }
            }
            linesInfo += "</tbody></table>";
            return linesInfo;
        }

        public decimal GetEncumbranceTotal(LineItemGroup encumbrance)
        {
            //encumbrance is a deep copy
            decimal total = 0M;
            if (encumbrance.LineItems != null)
            {
                foreach (LineItem item in encumbrance.LineItems)
                {
                    total += item.Amount;
                }
            }
            return total;
        }

        private bool IsShallowContract(Contract contract)
        {
            // return true if contract does not include child elements
            if (contract.LineItems == null && _context.HasLineItems(contract)) { return true; }
            if (contract.ProcurementID > 0 && contract.MethodOfProcurement == null) { return true; }
            return false;
        }
        public bool IsShallowEncumbrance(LineItemGroup encumbrance)
        {
            // return true if encumbrance does not include child elements (i.e., LineItems)
            if (encumbrance.LineItems == null && _context.HasLineItems(encumbrance)) { return true; }
            if (encumbrance.OriginatorUserID > 0 && encumbrance.OriginatorUser == null) { return true; }
            return false;
        }
    }
}
