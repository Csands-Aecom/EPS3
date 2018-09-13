using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Models;
using EPS3.Helpers;
using EPS3.ViewModels;
using EPS3.DataContexts;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Extensions.Options;

namespace EPS3.Controllers
{
    public class LineItemGroupsController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<LineItemGroupsController> _logger;
        private PermissionsUtils _pu;
        public SmtpConfig SmtpConfig { get; }

        public LineItemGroupsController(EPSContext context, ILoggerFactory loggerFactory, IOptions<SmtpConfig> smtpConfig)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<LineItemGroupsController>();
            SmtpConfig = smtpConfig.Value;
            _pu = new PermissionsUtils(_context);
        }

        public IActionResult Index()
        {
            // Get Current User
            // Get Roles
            // Get Line Items needing attention for the roles
            // i.e., line items in encumbrance requests that are in status
            //   Submitted to Finance for FinanceReviewer role
            //   Submiitted to Work Program for WP Reviewer role
            //   Ready for CFM for CFMSubmitter Role
            // Group by role
            // Order by last updated (ascending)

            PopulateViewBag(0);
            User currentUser = ViewBag.CurrentUser;
            string roles = ViewBag.Roles;
            Dictionary<string, List<LineItemGroup>> encumbrances = new Dictionary<string, List<LineItemGroup>>();
            List<LineItemGroup> FinanceEncumbrances = null;
            List<LineItemGroup> WPEncumbrances = null;
            List<LineItemGroup> CFMEncumbrances = null;
            if (roles.Contains(ConstantStrings.FinanceReviewer))
            {
                FinanceEncumbrances = _context.LineItemGroups
                    .AsNoTracking()
                    .Where(g => g.CurrentStatus.Equals(ConstantStrings.SubmittedFinance))
                    .Include(g => g.LineItems).OrderBy(li => li.LastEditedDate)
                    .Include(g => g.Contract).ThenInclude(c => c.Vendor)
                    .ToList();
            }
            if (roles.Contains(ConstantStrings.WPReviewer))
            {
                WPEncumbrances = _context.LineItemGroups
                    .AsNoTracking()
                    .Where(g => g.CurrentStatus.Equals(ConstantStrings.SubmittedWP))
                    .Include(g => g.LineItems).OrderBy(li => li.LastEditedDate)
                    .Include(g => g.Contract).ThenInclude(c => c.Vendor)
                    .ToList();
            }
            if (roles.Contains(ConstantStrings.CFMSubmitter))
            {
                CFMEncumbrances = _context.LineItemGroups
                    .AsNoTracking()
                    .Where(g => g.CurrentStatus.Equals(ConstantStrings.CFMReady))
                    .Include(g => g.LineItems).OrderBy(li => li.LastEditedDate)
                    .Include(g => g.Contract).ThenInclude(c => c.Vendor)
                    .ToList();
            }
            encumbrances.Add(ConstantStrings.SubmittedFinance, FinanceEncumbrances);
            encumbrances.Add(ConstantStrings.SubmittedWP, WPEncumbrances);
            encumbrances.Add(ConstantStrings.CFMReady, CFMEncumbrances);
            LineItemsListViewModel vmEncumbrances = new LineItemsListViewModel();
            vmEncumbrances.Encumbrances = encumbrances;
            return View(vmEncumbrances);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            LineItemGroup group = await _context.LineItemGroups
                .Include(l => l.OriginatorUser)
                .Include(l => l.LastEditedUser)
                .SingleOrDefaultAsync(l => l.GroupID == id);
                
            return  View(group);
        }
        [HttpPost]
        public async  Task<IActionResult> Edit([Bind("GroupID,ContractID,LineItemType,AmendedLineItemID,FlairAmendmentID,UserAssignedID,LastEditedUserID,OriginatorUserID,CurrentStatus")] LineItemGroup group, string Comments, int CurrentUserID)
        {
             LineItemGroup existingGroup = await _context.LineItemGroups
                   .Include(l => l.OriginatorUser)
                   .Include(l => l.LastEditedUser)
                   .SingleOrDefaultAsync(l => l.GroupID == group.GroupID);

            return  View(group);
        }
        [HttpPost]
        //This is the AJAX method that returns a JSON object

        public JsonResult Update(string encumbrance)
        {
            EncumbranceRequestViewModel encumbranceVM = JsonConvert.DeserializeObject<EncumbranceRequestViewModel>(encumbrance);
            //encumbrance is a JSON object. Unpack it here
            LineItemGroup newGroup = encumbranceVM.LineItemGroups[0];
            LineItemGroup existingGroup = _context.LineItemGroups.AsNoTracking().SingleOrDefault(l => l.GroupID == encumbranceVM.LineItemGroups[0].GroupID);
            List<int> workProgramUserIds = encumbranceVM.WpRecipients.ToList();
            LineItemGroupStatus newStatus = encumbranceVM.Statuses[0];
            newStatus.CurrentStatus = ConstantStrings.LookupConstant(newStatus.CurrentStatus);
            string oldStatus = existingGroup.CurrentStatus;
            // 1. get the LineItemGroup from the DB and see if status has changed
            bool statusChange = (newStatus.CurrentStatus != existingGroup.CurrentStatus) || (newStatus.Comments != null && newStatus.Comments.Length > 0);
            newGroup.CurrentStatus = newStatus.CurrentStatus;
            bool isDirty = false;
            if (!existingGroup.Equals(newGroup))
            {
                if(existingGroup.CurrentStatus != newStatus.CurrentStatus && newGroup.CurrentStatus != null)
                {
                    existingGroup.CurrentStatus = newStatus.CurrentStatus;
                    isDirty = true;
                }
                if (existingGroup.LineItemType != newGroup.LineItemType)
                {
                    existingGroup.LineItemType = newGroup.LineItemType;
                    isDirty = true;
                }
                if (existingGroup.FlairAmendmentID != newGroup.FlairAmendmentID && newGroup.FlairAmendmentID != null)
                {
                    existingGroup.FlairAmendmentID = newGroup.FlairAmendmentID;
                    isDirty = true;
                }
                if (existingGroup.UserAssignedID != newGroup.UserAssignedID && newGroup.UserAssignedID != null)
                {
                    existingGroup.UserAssignedID = newGroup.UserAssignedID;
                    isDirty = true;
                }
                if (existingGroup.AmendedLineItemID != newGroup.AmendedLineItemID && newGroup.AmendedLineItemID != null)
                {
                    existingGroup.AmendedLineItemID = newGroup.AmendedLineItemID;
                    isDirty = true;
                }
                existingGroup.LastEditedDate = DateTime.Now;
                existingGroup.LastEditedUserID = newGroup.LastEditedUserID;
                if(existingGroup.OriginatorUserID <= 0)
                {
                    existingGroup.OriginatorUserID = newGroup.OriginatorUserID;
                }
            }
            // 2. add status record
            if (statusChange)
            {
                LineItemGroupStatus status = new LineItemGroupStatus()
                {
                    CurrentStatus = newStatus.CurrentStatus,
                    LineItemGroupID = existingGroup.GroupID,
                    SubmittalDate = DateTime.Now,
                    Comments = newStatus.Comments
                };
                if (newGroup.LastEditedUserID > 0) { status.UserID = newGroup.LastEditedUserID;}
                _context.LineItemGroupStatuses.Add(status);
                _context.SaveChanges();

                // 3. make updates and save
                if (isDirty)
                {
                    _context.LineItemGroups.Update(existingGroup);
                    _context.SaveChanges();
                }
            }
            // 4. send appropriate notifications
            int msgID = 0;
            string url = this.Request.Scheme + "://" + this.Request.Host;
            MessageService messageService = new MessageService(_context, SmtpConfig, url);
            if (workProgramUserIds != null && workProgramUserIds.Count > 0)
            {
                msgID = messageService.AddMessage(AssessStatusChange(newGroup.CurrentStatus, oldStatus), existingGroup, newStatus.Comments, workProgramUserIds);
            }
            else
            {
                msgID = messageService.AddMessage(AssessStatusChange(newGroup.CurrentStatus, oldStatus), existingGroup, newStatus.Comments);
            }
            messageService.SendEmailMessage(msgID);
            User user = existingGroup.LastEditedUser;
            // 5. return success message with updated LineItemGroup in JSON object
            // roll my own json response
            string response = "{'success': 'true', 'FlairAmendmentID' : '" + (existingGroup.FlairAmendmentID.IsNullOrEmpty() ? "None." : existingGroup.FlairAmendmentID) + "', " +
                "'UserAssignedID' : '" + (existingGroup.UserAssignedID.IsNullOrEmpty() ? "None." : existingGroup.UserAssignedID) + "', " +
                "'AmendedLineItemID' : '" + (existingGroup.AmendedLineItemID.IsNullOrEmpty() ? "None." : existingGroup.UserAssignedID) + "', " +
                "'LineItemType' : '" + existingGroup.LineItemType + "', " +
                "'GroupID' : '" + existingGroup.GroupID + "', 'StatusID' : '" + newStatus.StatusID + "', 'UserName' : '" + user.FullName + "'}";
            return Json(response);
            // This gets stuck in a self-referential loop
            //return new JsonResult(existingGroup);
        }
        private string AssessStatusChange(string newStatus, string oldStatus)
        {
            string changeType = ConstantStrings.NoChange;
            if (oldStatus == null) { oldStatus = "null";  }
            if (!oldStatus.Equals(newStatus))
            {
                //Status has changed
                //1. Draft to Submitted
                if (oldStatus.Equals(ConstantStrings.Draft) && newStatus.Equals(ConstantStrings.SubmittedFinance))
                {
                    changeType = ConstantStrings.DraftToFinance;
                }
                //2. Submitted to Draft
                if (oldStatus.Equals(ConstantStrings.SubmittedFinance) && newStatus.Equals(ConstantStrings.Draft))
                {
                    changeType = ConstantStrings.FinanceToDraft;
                }
                //3. Submitted to WP Review
                if (oldStatus.Equals(ConstantStrings.SubmittedFinance) && newStatus.Equals(ConstantStrings.SubmittedWP))
                {
                    changeType = ConstantStrings.FinanceToWP;
                }
                //4. WP Review to CFM Ready
                if (oldStatus.Equals(ConstantStrings.SubmittedWP) && newStatus.Equals(ConstantStrings.CFMReady))
                {
                    changeType = ConstantStrings.WPToCFM;
                }
                //5. CFM Complete
                if (( oldStatus.Equals(ConstantStrings.CFMReady)) && newStatus.Equals(ConstantStrings.CFMComplete))
                {
                    changeType = ConstantStrings.CFMComplete;
                }
            }
            return changeType;
        }
        private string GetLogin()
        {
            string userLogin = "";
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                userLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            }
            else
            {
                userLogin = HttpContext.User.Identity.Name;
            }
            return _pu.GetLogin(userLogin);
        }

        public void PopulateViewBag(int contractID)
        {
            //const string sessionKey = "CurrentUser";
            string userLogin = GetLogin();

            if (userLogin != null)
            {
                try
                {
                    User currentUser = _pu.GetUser(userLogin);
                    string roles = _pu.GetUserRoles(userLogin);
                    Contract contract = _pu.GetContractByID(contractID);
                    ViewBag.Contract = contract;
                    ViewBag.CurrentUser = currentUser;
                    ViewBag.Roles = roles;
                }
                catch (Exception e)
                {
                    _logger.LogError("ContractsController.PopulateViewBag Error:" + e.GetBaseException());
                }
            }
            else
            {
                RedirectToAction("Home");
            }
        }
    }
}