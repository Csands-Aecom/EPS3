﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using EPS3.DataContexts;
using EPS3.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Serilog;

namespace EPS3.Helpers
{
    public class MessageService
    {
        //private readonly ILogger<MessageService> _logger;
        private readonly EPSContext _context;
        private SmtpConfig _smtpConfig { get; }
        private string _serverpath;
        private PermissionsUtils _pu;

        public MessageService(EPSContext context, SmtpConfig smtpConfig, string url)
        {
            _context = context;
            _smtpConfig = smtpConfig;
            _serverpath = url;
            _pu = new PermissionsUtils(context);
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
                        msg.Body = "<p>Please process the following encumbrance request: ID " + encumbrance.GroupID + " for contract " + contract.ContractNumber + " in the amount of $" + encumbranceTotal.ToString("N2") + ".</p>\n";

                        if (comments != null && comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += GetStatusComments(encumbrance);
                        msg.Body += "<p>Review this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        // Send only to TPK Encumbrance mailbox
                        recipientIDs = (List<int>)_context.Users.Where(u => u.Email == ConstantStrings.TPKMailbox).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.DraftToCFM:
                        msg.Subject = "Encumbrance Request# " + encumbrance.GroupID + " for contract " + contract.ContractNumber + " for " + encumbrance.LineItemType;
                        msg.Body = "<p>Please input the following encumbrance into CFM: request ID " + encumbrance.GroupID + " for contract " + contract.ContractNumber + ".</p>\n";
                        //msg.Body += "in the amount of $" + encumbranceTotal + " applied to Amendment " + encumbrance.FlairAmendmentID + " Line " + encumbrance.LineID6S + ".";
                        if (encumbrance.LineItems != null && encumbrance.LineItems.Count > 0) {
                            string tblText = "<table><tr><th>Contract</th><th>Amendment</th><th>Line (6s)</th><th>Amount</th></tr>";
                            foreach (LineItem item in encumbrance.LineItems)
                            {
                                tblText += "<tr><td>" + contract.ContractNumber + "</td><td>" + item.FlairAmendmentID + "</td><td>" + item.LineID6S + "</td><td>$" + item.Amount.ToString("N2") + "</td></tr>";
                            }
                            tblText += "</table> <br/>";
                            msg.Body += tblText;
                        }
                        if (comments != null && comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += GetStatusComments(encumbrance);
                        msg.Body += "<p>View this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        // Send only to TPK Encumbrance mailbox
                        recipientIDs = (List<int>)_context.Users.Where(u => u.Email == ConstantStrings.TPKMailbox).Select(u => u.UserID).ToList();
                        break;
                    case ConstantStrings.FinanceToDraft:
                    case ConstantStrings.CFMToDraft:
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
                    case ConstantStrings.WPToCFM:
                        msg.Subject = "Please review encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " is ready for CFM Input";
                        msg.Body = "<p>" + submitter.FullName + " has completed a Work Program review for encumbrance request #" + encumbrance.GroupID + " in Work Program review under contract " + contract.ContractNumber + ".</p>\n";
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
                    case ConstantStrings.CFMComplete:
                        msg.Subject = "Encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " has been input into CFM";
                        msg.Body = "<p>" + submitter.FullName + " has input encumbrance request #" + encumbrance.GroupID + " for contract " + contract.ContractNumber + " into CFM.</p>\n";
                        if (comments.Length > 0)
                        { msg.Body += "<p>Comments: " + comments + "</p>\n"; }
                        msg.Body += "<p>No further action is required. You may view this encumbrance request in the <a href='" + contractViewURL + "'>" +
                            "EPS Application</a>.</p>";
                        // Send only to TPK Encumbrance mailbox
                        recipientIDs = new List<int> { encumbrance.OriginatorUserID }; ;
                        break;
                    case ConstantStrings.CloseContract:
                        msg.Subject = "Request to Close Contract #" + contract.ContractNumber;
                        msg.Body = "<p>" + submitter.FullName + " requests closure of the contract " + contract.ContractNumber + " (" + contract.ContractID + "), closure type " + encumbrance.LineItemType + " </p>";
                        msg.Body += "<p>Review this closure request in the <a href='" + contractViewURL + "'>" + "EPS Application</a>.</p>";
                        recipientIDs = (List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.Closer)).Select(u => u.UserID).ToList();
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
            if (_pu.IsShallowContract(contract))
            {
                contract = _pu.GetDeepContract(contract.ContractID);
            }
            if (encumbrance == null)
            {
                return null;
            }
            else
            {
                if (_pu.IsShallowEncumbrance(encumbrance))
                {
                    encumbrance = _pu.GetDeepEncumbrance(encumbrance.GroupID);
                }
                decimal encumbranceTotal = GetEncumbranceTotal(encumbrance);
                encumbranceInfo = GetEncumbranceInfo(encumbrance, encumbranceTotal);
                if (contract == null)
                {
                    contract = _pu.GetDeepContract(encumbrance.ContractID);
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
                    contractInfo += "<strong>Contract Initial Amount:</strong> $" + string.Format("{0:#.00}", Convert.ToDecimal(contract.ContractTotal.ToString())) + "<br/>";
                    //TODO: What other contract information should be included in the email receipt?
                }
                List<LineItem> lineItems = _pu.GetDeepLineItems(encumbrance.GroupID);
                if (lineItems == null || lineItems.Count == 0)
                {
                    linesInfo += "There are no line items associated with this encumbrance.";
                }
                else
                {
                    linesInfo += GetLineItemsInfo(lineItems);
                }
            }
            body += contractInfo + "<br/>" + encumbranceInfo + "<br/>" + linesInfo + "<br/><br/>";
            body += "You can review your encumbrance request at <a href='" + url +"'> Encumbrance " + encumbrance.GroupID + "</a>";
            return body;
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
                    UserID = user.UserID
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

        public void SendClosingRequest(ContractClosure closure, User submitter)
        {
            int groupID = closure.LineItemGroupID != null ? int.Parse(closure.LineItemGroupID) : 0;
            int contractID = closure.ContractID != null ? int.Parse(closure.ContractID) : 0;
            int closeStatus = closure.ClosureType.Contains("50") ? 50 : 98;
            if (groupID > 0) {
                LineItemGroup encumbrance = (LineItemGroup)_context.LineItemGroups.AsNoTracking().SingleOrDefault(e => e.GroupID == groupID);
            }
            Contract contract = (Contract)_context.Contracts.AsNoTracking().Include(c => c.Vendor)
                .SingleOrDefault(c => c.ContractID == contractID);
            Message msg = new Message
            {
                FromUserID = submitter.UserID,
                MessageDate = DateTime.Now
            };
            if (closure.ContractOrEncumbrance.Equals("Contract")) {
                msg.Subject = "Please Close Contract " + contract.ContractNumber + ".";
                msg.Body = "Please place in status " + closeStatus.ToString() + ".<br/>";
                msg.Body += "Contract Number: " + contract.ContractNumber + " Vendor: " + contract.Vendor.VendorName;
            } else {
                msg.Subject = "Please Close Amendment " + closure.FlairID + ".";
                msg.Body = "Please place in status " + closeStatus.ToString() + ".<br/>";
                msg.Body += "Amendment: " + closure.FlairID + "  Contract Number: " + contract.ContractNumber + "  Vendor: " + contract.Vendor.VendorName;
            }
            //add Closer(s) as recipient(s)
            List<int> recipientIDs = (List<int>)_context.UserRoles.Where(u => u.Role.Equals(ConstantStrings.Closer)).Select(u => u.UserID).ToList();
            msg.AddRecipients(recipientIDs);

            //save the message
            _context.Messages.Add(msg);
            _context.SaveChanges();
            //send the message
            SendEmailMessage(msg.MessageID);
        }

        public void SendEmailMessage(int msgID)
        {
            try
            {
                if (msgID == 0) { return; }
                Message msg = _context.Messages.AsNoTracking()
                    .Include(m => m.FromUser)
                    .Include(m => m.Recipients)
                    .SingleOrDefault(m => m.MessageID == msgID);
                List<User> recipients = new List<User>();
                foreach (MessageRecipient recip in msg.Recipients)
                {
                    User user = _context.Users.AsNoTracking()
                        .SingleOrDefault(u => u.UserID == recip.UserID);
                    if (user.Email != null && user.Email.Length > 0 && user.ReceiveEmails > 0)
                    {
                        recipients.Add(user);
                    }
                }
                SendMail(msg, recipients);
            }catch(Exception e)
            {
                Log.Error("MessageService.SendEmailMessage Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
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
            User adminUser = _context.Users.AsNoTracking().SingleOrDefault(u => u.UserLogin.Equals("KNAECCS"));
            adminRecipients.Add(new MessageRecipient(adminUser));
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
            catch (Exception e)
            {
                //_logger.LogError(ex.StackTrace);
                Log.Error("MessageService.SendEmail Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                Console.Write(e.StackTrace);
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

        public string GetContractInfo(Contract contract)
        {
            // NOTE: This must be a Deep Copy of a contract
            string contractInfo = "";
            contractInfo += "<h3>Contract Information</h3>";
            contractInfo += "<strong>Contract Number:</strong> " + contract.ContractNumber + "<br />";
            contractInfo += "<strong>Contract Type:</strong> " + contract.ContractType.ContractTypeSelector + "<br />";
            string canRenew = contract.IsRenewable > 0 ? "Yes" : "No";
            contractInfo += "<strong>Is Contract Renewable?:</strong> " + canRenew + "<br />";
            contractInfo += "<strong>Contract Initial Amount:</strong> $" + string.Format("{0:#.00}", Convert.ToDecimal(contract.ContractTotal.ToString())) + "<br />";
            contractInfo += "<strong>Maximum LOA Amount:</strong> $" + string.Format("{0:#.00}", Convert.ToDecimal(contract.MaxLoaAmount.ToString())) + "<br />";
            contractInfo += "<strong>Budget Ceiling:</strong> $" + string.Format("{0:#.00}", Convert.ToDecimal(contract.BudgetCeiling.ToString()))  + "<br />";
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
            encumbranceInfo += "<strong>Encumbrance Total:</strong> $" + string.Format("{0:#.00}", Convert.ToDecimal(encumbranceTotal.ToString())) + "<br />";
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
            linesInfo += "<table><thead><tr><th>Order</th><th>Line</th><th>Organization Code</th><th>Financial Project Number</th><th>State Program</th><th>Category</th><th>Work Activity</th><th>OCA</th><th>EO</th><th>Object Code</th><th>Fund</th><th>Fiscal Year</th><th>Amount</th></tr></thead><tbody>";
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
                linesInfo += "<td>$" + string.Format("{0:#.00}", Convert.ToDecimal(item.Amount.ToString())) + "</td>";
                linesInfo += "</tr>";
                if(item.Comments != null)
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
            decimal total = 0;
            foreach (LineItem item in encumbrance.LineItems)
            {
                total += item.Amount;
            }
            return total;
        }
    }
}
