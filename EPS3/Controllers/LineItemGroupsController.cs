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
using Serilog;

namespace EPS3.Controllers
{
    public class LineItemGroupsController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<LineItemGroupsController> _logger;
        private PermissionsUtils _pu;
        private MessageService _messageService;
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
            //   Finance for FinanceReviewer role
            //   Work Program for WP Reviewer role
            //   CFM for CFMSubmitter Role
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

        [HttpGet]
        public  IActionResult Manage(int? id)
        {
            PopulateViewBag(0);

            int groupID = (id == null) ? 0 : (int)id;

            List<LineItem> LineList = _pu.GetDeepLineItems(groupID);
            ViewBag.LineItems = LineList;
            ViewBag.LineItemCount = LineList.Count();
            ViewBag.LineItemTypes = ConstantStrings.GetLineItemTypeList();
            // ViewBag.LineItemsMap = Map of LineItemID, JSONString of serialized object
            Dictionary<int, string> lineItemsMap = new Dictionary<int, string>();
            foreach (LineItem lineItem in LineList) {
                lineItemsMap.Add(lineItem.LineItemID, JsonConvert.SerializeObject(lineItem));
            }
            ViewBag.LineItemsMap = lineItemsMap;
            if (groupID > 0)
            {
                LineItemGroup Encumbrance = _pu.GetDeepEncumbrance(groupID);
                if (Encumbrance != null)
                {
                    Contract Contract = _pu.GetDeepContract(Encumbrance.ContractID);
                    ViewBag.Contract = Contract;
                    return View(Encumbrance);
                }
            }
                return View();
        }

        [HttpPost]
        public  IActionResult Manage([Bind("GroupID,ContractID,LineItemType,AmendedLineItemID,FlairAmendmentID,UserAssignedID,LastEditedUserID,OriginatorUserID,CurrentStatus,Description")] LineItemGroup group, string Comments, int CurrentUserID)
        {
            if (group.ContractID >0)
            {
                return RedirectToAction("View", "Contracts", new { id = group.ContractID });
            }
            else
            {
                return View();
            }
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            // Delete an encumbrance and all of its statuses, its line items and their statuses
            try
            {
                LineItemGroup encumbrance = await _context.LineItemGroups
                     .Include(g => g.Statuses)
                     .Include(g => g.LineItems).ThenInclude(li => li.Statuses)
                     .SingleOrDefaultAsync(g => g.GroupID == id);
                if (encumbrance == null)
                {
                    return NotFound();
                }
                return View(encumbrance);
            }catch(Exception e)
            {
                _logger.LogError("LineItemGroupsController.Delete Error:" + e.GetBaseException());
                Log.Error("LineItemGroupsController.Delete  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                return NotFound();
            }
        }


        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            return View();
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
            if (_messageService == null)
            {
                string url = this.Request.Scheme + "://" + this.Request.Host;
                _messageService = new MessageService(_context, SmtpConfig, url);
            }
            if (workProgramUserIds != null && workProgramUserIds.Count > 0)
            {
                msgID = _messageService.AddMessage(AssessStatusChange(newGroup.CurrentStatus, oldStatus), existingGroup, newStatus.Comments, workProgramUserIds);
            }
            else
            {
                msgID = _messageService.AddMessage(AssessStatusChange(newGroup.CurrentStatus, oldStatus), existingGroup, newStatus.Comments);
            }
            _messageService.SendEmailMessage(msgID);
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
                    _logger.LogError("LineItemGroupsController.PopulateViewBag Error:" + e.GetBaseException());
                    Log.Error("LineItemGroupsController.PopulateViewBag  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
            }
            else
            {
                RedirectToAction("Home");
            }
        }

        [HttpGet]
        public IActionResult NewContractPartial(int? id)
        {

            Contract contract = null;
            if (id != null && id > 0) { 
                contract = _context.Contracts
                    .Include(c => c.ContractType)
                    .Include(c => c.Vendor)
                    .SingleOrDefault(c => c.ContractID == id);
            }
            string userLogin = GetLogin();
            PopulateViewBag(0);
            ViewData["Procurements"] = _context.Procurements.OrderBy(p => p.ProcurementCode);
            ViewData["Compensations"] = _context.Compensations.OrderBy(c => c.CompensationID);
            ViewData["Vendors"] = _context.Vendors.OrderBy(v => v.VendorName);
            ViewData["Recipients"] = _context.Recipients.OrderBy(v => v.RecipientCode);
            if (contract != null)
            {
                return PartialView("NewContractPartial", contract);
            }
            return PartialView("NewContractPartial");
        }

        [HttpGet]
        public IActionResult NewLineItemPartial(int? id)
        {
            Contract contract = null;
            if (id != null && id > 0)
            {
                contract = _context.Contracts.SingleOrDefault(c => c.ContractID == id);
            }
            string userLogin = GetLogin();
            PopulateViewBag(0);
            ViewBag.currentFiscalYear = _pu.GetCurrentFiscalYear();
            ViewData["Categories"] = _context.Categories.OrderBy(v => v.CategoryCode);
            ViewData["StatePrograms"] = _context.StatePrograms.OrderBy(v => v.ProgramCode);
            if(contract != null)
            {
                return PartialView("NewLineItemPartial", contract);
            }
            return PartialView("NewLineItemPartial");
        }

        [HttpPost]
        public JsonResult AddNewEncumbrance(string lineItemGroup, string comments)
        {
            LineItemGroup newLineItemGroup = JsonConvert.DeserializeObject<LineItemGroup>(lineItemGroup);
            EncumbranceComment newComment = JsonConvert.DeserializeObject<EncumbranceComment>(comments);
            try
            {
                int groupID = newLineItemGroup.GroupID;
                string oldStatus = "";
                bool isNew = groupID <= 0;
                Contract contract = _context.Contracts.SingleOrDefault(c => c.ContractID == newLineItemGroup.ContractID);
                if (isNew)
                {
                    // add new
                    newLineItemGroup.OriginatorUserID = newLineItemGroup.LastEditedUserID;
                    newLineItemGroup.LastEditedDate = DateTime.Now;
                    newLineItemGroup.OriginatedDate = DateTime.Now;
                    newLineItemGroup.Contract = contract;
                    newLineItemGroup.CurrentStatus = newComment.status;
                    _context.LineItemGroups.Add(newLineItemGroup);
                }
                else
                {
                    // update existing
                    LineItemGroup existingGroup = _context.LineItemGroups
                        .SingleOrDefault(g => g.GroupID == groupID);
                    newLineItemGroup.LastEditedDate = DateTime.Now;
                    existingGroup.AmendedLineItemID = newLineItemGroup.AmendedLineItemID;
                    existingGroup.ContractID = newLineItemGroup.ContractID;
                    oldStatus = existingGroup.CurrentStatus;
                    existingGroup.CurrentStatus = newComment.status;
                    existingGroup.Description = newLineItemGroup.Description;
                    existingGroup.FlairAmendmentID = newLineItemGroup.FlairAmendmentID;
                    existingGroup.IncludesContract = newLineItemGroup.IncludesContract;
                    existingGroup.IsEditable = newLineItemGroup.IsEditable;
                    existingGroup.LastEditedUserID = newLineItemGroup.LastEditedUserID;
                    existingGroup.UserAssignedID = newLineItemGroup.UserAssignedID;
                    existingGroup.Contract = contract;
                    _context.Update(existingGroup);
                    newLineItemGroup = existingGroup;
                }
                _context.SaveChanges();
                // add LineItemGroupStatus
                LineItemGroupStatus newStatus = new LineItemGroupStatus()
                {
                    LineItemGroupID = newLineItemGroup.GroupID,
                    CurrentStatus = newComment.status,
                    UserID = newComment.userID,
                    Comments = newComment.comments,
                    SubmittalDate = DateTime.Now
                };
                _context.LineItemGroupStatuses.Add(newStatus);
                _context.SaveChanges();

                // send receipt and notification if Encumbrance is submitted for review
                string statusChange = AssessStatusChange(newLineItemGroup.CurrentStatus, oldStatus);
                int msgID = 0;

                // initialize the message service if necessary
                if (_messageService == null)
                {
                    string url = this.Request.Scheme + "://" + this.Request.Host;
                    _messageService = new MessageService(_context, SmtpConfig, url);
                }

                if (newComment.notify)
                {
                    // TODO: Add ability to cc: the sender.
                    //User sender = _pu.GetUserByID(newComment.userID);
                    //msgID = _messageService.AddMessage(statusChange, newLineItemGroup, newComment.comments, newComment.wpIDs, sender);


                    // For now, just add the sender to the recipients list
                    newComment.wpIDs.Add(newComment.userID);
                    msgID = _messageService.AddMessage(statusChange, newLineItemGroup, newComment.comments, newComment.wpIDs);
                }
                else
                {
                    msgID = _messageService.AddMessage(statusChange, newLineItemGroup, newComment.comments, newComment.wpIDs);
                }
                if (newComment.receipt)
                {
                    User sender = _pu.GetUserByID(newComment.userID);
                    _messageService.SendReceipt(newLineItemGroup, sender, newComment.comments);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("LineItemGroupsController.AddNewEncumbrance Error:" + e.GetBaseException());
                Log.Error("LineItemGroupsController.AddNewEncumbrance  Error:" + e.GetBaseException() + "\n" + e.StackTrace);

            }
            // return complete, updated object (with GroupID, if new)
            // avoid self-referential loop with unnecessary information in Statuses
            newLineItemGroup.Statuses = null;
            string result = JsonConvert.SerializeObject(newLineItemGroup);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ListContracts(string searchString)
        {
            var searchSTRING = searchString.ToUpper();
            if (!string.IsNullOrEmpty(searchString))
            {
                List<Contract> ContractList = _context.Contracts
                    .Where(c => (c.ContractNumber.ToUpper().Contains(searchSTRING)))
                    .OrderBy(c => c.ContractNumber)
                    .ToList();
                return Json(ContractList);
            }
            return new JsonResult(searchString);
        }
        // GET: LineItems
        [HttpGet]
        public IActionResult List()
        {
            Dictionary<string, List<LineItemGroup>> categorizedLineItemGroups = getCategorizedLineItemGroups();
            categorizedLineItemGroups.Add(ConstantStrings.Advertisement, getAdvertisedLineItemGroups());
            Dictionary<int, string> lineItemGroupAmounts = getLineItemGroupAmounts(categorizedLineItemGroups);
            ViewBag.EncumbranceAmounts = lineItemGroupAmounts;
            return View(categorizedLineItemGroups);
        }

        private Dictionary<string, List<LineItemGroup>> getCategorizedLineItemGroups()
        {
            Dictionary<string, List<LineItemGroup>> results = new Dictionary<string, List<LineItemGroup>>();
            PopulateViewBag(0);
            User user = ViewBag.CurrentUser;

            if (ViewBag.Roles.Contains(ConstantStrings.Originator))
            {
                // add Group IDs for Groups in Draft where user is the originator
                List<LineItemGroup> origLineIDs = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.CurrentStatus.Equals(ConstantStrings.Draft))
                    .Where(l => l.Contract.User.UserLogin.Equals(user.UserLogin))
                    .Include(l => l.Contract)
                    .ToList();
                results.Add(ConstantStrings.Originator, origLineIDs);
            }
            if (ViewBag.Roles.Contains(ConstantStrings.FinanceReviewer))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<LineItemGroup> finLineIDs = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.CurrentStatus.Equals(ConstantStrings.SubmittedFinance))
                    .Include(l => l.Contract)
                    .ToList();
                results.Add(ConstantStrings.FinanceReviewer, finLineIDs);
            }
            if (ViewBag.Roles.Contains(ConstantStrings.WPReviewer))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<LineItemGroup> wpLineIDs = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.CurrentStatus.Equals(ConstantStrings.SubmittedWP))
                    .Include(l => l.Contract)
                    .ToList();
                results.Add(ConstantStrings.WPReviewer, wpLineIDs);
            }
            if (ViewBag.Roles.Contains(ConstantStrings.CFMSubmitter))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<LineItemGroup> cfmLineIDs = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.CurrentStatus.Equals(ConstantStrings.CFMReady))
                    .Include(l => l.Contract)
                    .ToList();
                results.Add(ConstantStrings.CFMSubmitter, cfmLineIDs);
            }
            return results;
        }

        private List<LineItemGroup> getAdvertisedLineItemGroups()
        {
            // add  Groups that are unawarded Advertisements
            List<LineItemGroup> adGroups = _context.LineItemGroups.AsNoTracking()
                .Where(l => l.LineItemType.Equals(ConstantStrings.Advertisement))
                .Include(l => l.Contract)
                .ToList();
            List<LineItemGroup> awardGroups = _context.LineItemGroups.AsNoTracking()
                .Where(l => l.LineItemType.Equals(ConstantStrings.Award))
                .Include(l => l.Contract)
                .ToList();
            return adGroups.Except(awardGroups).ToList();
        }

        private Dictionary<int, string> getLineItemGroupAmounts(Dictionary<string, List<LineItemGroup>> encumbrances)
        {
            Dictionary<int, string> EncumbranceAmounts = new Dictionary<int, string>();
            foreach(string key in encumbrances.Keys)
            {
                List<LineItemGroup> encumbranceList = encumbrances[key];
                foreach(LineItemGroup encumbrance in encumbranceList)
                {
                    if (!EncumbranceAmounts.Keys.Contains(encumbrance.GroupID))
                    {
                        decimal amount = GetEncumbranceAmount(encumbrance);
                        string amountString = String.Format("{0:C2}", amount); ;
                        EncumbranceAmounts.Add(encumbrance.GroupID, amountString);
                    }
                }
            }
            return EncumbranceAmounts;
        }

        public JsonResult GetEncumbranceTotalAmount(string encumbranceInfo)
        {
            dynamic lookupInfo = JsonConvert.DeserializeObject(encumbranceInfo);
            int groupID = lookupInfo.GroupID;
            LineItemGroup group = _context.LineItemGroups.AsNoTracking().SingleOrDefault(e => e.GroupID == groupID);
            return Json(GetEncumbranceAmount(group));
        }

        public decimal GetEncumbranceAmount(LineItemGroup encumbrance)
        {
            decimal amount = 0.0m;
            try
            {
                List<LineItem> items = _context.LineItems.Where(l => l.LineItemGroupID == encumbrance.GroupID).ToList();
                foreach (LineItem item in items)
                {
                    amount += item.Amount;
                }
            } catch (Exception e) {
                _logger.LogError("LineItemGroupsController.GetEncumbranceAmount Error:" + e.GetBaseException());
                Log.Error("LineItemGroupsController.GetEncumbranceAmount  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
            return amount;
        }
        public JsonResult GetEncumbranceCountByType (string encumbranceInfo)
        {
            dynamic lookupInfo = JsonConvert.DeserializeObject(encumbranceInfo);
            int contractID = lookupInfo.contractID;
            string encumbranceType = lookupInfo.encumbranceType;

            int encumbranceCount = _context.LineItemGroups.Where(g => g.ContractID == contractID && g.LineItemType == encumbranceType).Count();
            return Json(encumbranceCount);
        }

        [HttpPost]
        public JsonResult GetNextLineNumber(string groupInfo)
        {
            int nextLineNumber = 0;
            dynamic lookupInfo = JsonConvert.DeserializeObject(groupInfo);
            if (lookupInfo.groupID != null && lookupInfo.groupID > 0) { 
                int groupID = lookupInfo.groupID;
                nextLineNumber = _context.LineItems.Where(g => g.LineItemGroupID == groupID).Select(g => g.LineNumber).DefaultIfEmpty(0).Max();
            }
            return Json(nextLineNumber);
        }
    }
}