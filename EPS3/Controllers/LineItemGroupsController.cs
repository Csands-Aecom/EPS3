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
            _pu = new PermissionsUtils(_context, _logger);
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

            return View(group);
        }
        [HttpPost]
        public async Task<IActionResult> Edit([Bind("GroupID,ContractID,LineItemType,AmendedLineItemID,FlairAmendmentID,UserAssignedID,LastEditedUserID,OriginatorUserID,CurrentStatus")] LineItemGroup group, string Comments, int CurrentUserID)
        {
            LineItemGroup existingGroup = await _context.LineItemGroups
                  .Include(l => l.OriginatorUser)
                  .Include(l => l.LastEditedUser)
                  .SingleOrDefaultAsync(l => l.GroupID == group.GroupID);

            return View(group);
        }

        [HttpGet]
        public IActionResult Manage(int? id)
        {
            int contractID = 0;
            if (id != null && id > 0)
            {
                LineItemGroup selectedEncumbrance = (LineItemGroup)_context.LineItemGroups.SingleOrDefault(g => g.GroupID == id);
                // an invalid id (no matching GroupID) will throw an error. 
                if (selectedEncumbrance != null)
                {
                    contractID = selectedEncumbrance.ContractID;
                }
                else
                {
                    // set the invalid id to 0
                    id = 0;
                }
            }
            PopulateViewBag(contractID);

            if ((id == null || id == 0) && !(ViewBag.Roles.Contains(ConstantStrings.Originator) || ViewBag.Roles.Contains(ConstantStrings.FinanceReviewer)))
            {
                //user does not have proper role to create a new request
                //redirect to list
                return RedirectToAction("List");
            }

            int groupID = (id == null) ? 0 : (int)id;

            List<LineItem> LineList = _pu.GetDeepLineItems(groupID);
            ViewBag.LineItems = LineList;
            ViewBag.LineItemCount = LineList == null ? 0 : LineList.Count();
            if (groupID > 0)
            {
                LineItemGroup Encumbrance = _pu.GetDeepEncumbrance(groupID);
                // set ViewBag.HasWPHistory
                if (Encumbrance.Statuses != null)
                {
                    foreach (LineItemGroupStatus status in Encumbrance.Statuses)
                    {
                        if (status.CurrentStatus.Equals(ConstantStrings.SubmittedWP)) { ViewBag.HasWPHistory = "WP"; }
                    }
                }

                if (Encumbrance != null)
                {
                    Contract Contract = _pu.GetDeepContract(Encumbrance.ContractID);
                    try
                    {
                        // select the LineItemTypes list to display:

                        if (AllEncumbrancesAreComplete(ViewBag.Contract)
                            && (ViewBag.CurrentUser != null && ViewBag.CurrentUser.UserID == Encumbrance.OriginatorUser.UserID))
                        {
                            // finance reviewer can close the Contract
                            ViewBag.LineItemTypes = null;
                            ViewBag.ActionItemTypes = ConstantStrings.GetRequestCloseList();
                        }
                        else
                        {
                            ViewBag.LineItemTypes = ConstantStrings.GetLineItemTypeList();
                        }
                        // ViewBag.LineItemsMap = Map of LineItemID, JSONString of serialized object
                        Dictionary<int, string> lineItemsMap = new Dictionary<int, string>();
                        foreach (LineItem lineItem in LineList)
                        {
                            lineItemsMap.Add(lineItem.LineItemID, JsonConvert.SerializeObject(lineItem));
                        }
                        ViewBag.LineItemsMap = lineItemsMap;

                        if (Encumbrance.CurrentStatus.Equals(ConstantStrings.Draft) && Encumbrance.LineItemType.Equals(ConstantStrings.Award))
                        {
                            ViewBag.AwardBanner = true;
                        }
                        if (Encumbrance.CurrentStatus.Equals(ConstantStrings.Draft) && Encumbrance.LineItemType.Equals(ConstantStrings.Amendment)
                            && Encumbrance.Description != null && Encumbrance.Description.Contains("Duplicate of Encumbrance"))
                        {
                            ViewBag.AmendBanner = true;
                        }
                        // Add to ViewBag a list of all files associated with this encumbrance request
                        List<FileAttachment> files = _context.FileAttachments.Where(f => f.GroupID == groupID).ToList();
                        ViewBag.Files = files;

                        ViewBag.Contract = Contract;
                        ViewBag.ContractAmount = _pu.GetTotalAmountOfAllEncumbrances(contractID);
                        return View(Encumbrance);
                    }
                    catch (Exception e) {
                        _logger.LogError("LineItemGroupsController.Manage Error:" + e.GetBaseException());
                        Log.Error("LineItemGroupsController.Manage  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                        return NotFound();
                    }
                }
            }
            else
            {
                ViewBag.LineItemTypes = ConstantStrings.GetLineItemTypeList();
                ViewBag.LineItemsMap = null;
            }
            return View(null);
        }
        public Boolean AllEncumbrancesAreComplete(Contract contract)
        {
            Boolean isComplete = true;
            List<LineItemGroup> encumbrances = _context.LineItemGroups.Where(g => g.ContractID == contract.ContractID).AsNoTracking().ToList();
            foreach (LineItemGroup encumbrance in encumbrances)
            {
                if (encumbrance.CurrentStatus != ConstantStrings.CFMComplete && !encumbrance.CurrentStatus.Contains("Closed"))
                {
                    isComplete = false;
                    break;
                }
            }
            return isComplete;
        }
        [HttpPost]
        public IActionResult Manage([Bind("GroupID,ContractID,LineItemType,AmendedLineItemID,FlairAmendmentID,UserAssignedID,LastEditedUserID,OriginatorUserID,CurrentStatus,Description")] LineItemGroup group, string Comments, int CurrentUserID)
        {
            if (group.ContractID > 0)
            {
                return RedirectToAction("View", "Contracts", new { id = group.ContractID });
            }
            else
            {
                return View();
            }
        }


        [HttpGet]
        public IActionResult Delete(int id)
        {
            // Delete an encumbrance and all of its statuses, its line items and their statuses
            try
            {
                LineItemGroup encumbrance =  _context.LineItemGroups
                     .Include(g => g.Statuses)
                     .Include(g => g.LineItems)
                     .Include(g => g.FileAttachments)
                     .SingleOrDefault(g => g.GroupID == id);
                if (encumbrance == null)
                {
                    return RedirectToAction("List");
                }
                //_context.Entry(encumbrance).State = EntityState.Deleted;
                _context.LineItemGroups.Remove(encumbrance);
                _context.SaveChanges();
                return RedirectToAction("List");
            }
            catch (Exception e)
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
                if (existingGroup.CurrentStatus != newStatus.CurrentStatus && newGroup.CurrentStatus != null)
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
                if (existingGroup.OriginatorUserID <= 0)
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
                if (newGroup.LastEditedUserID > 0) { status.UserID = newGroup.LastEditedUserID; }
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
            initializeMessageService();
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
            if (oldStatus == null) { oldStatus = "null"; }
            if (!oldStatus.Equals(newStatus))
            {
                //Status has changed
                //1. Draft to Submitted
                if (oldStatus.Equals(ConstantStrings.Draft) && newStatus.Equals(ConstantStrings.SubmittedFinance))
                {
                    changeType = ConstantStrings.DraftToFinance;
                }
                //1a. Draft to CFM in case of Close Line
                if (oldStatus.Equals(ConstantStrings.Draft) && newStatus.Equals(ConstantStrings.CFMReady))
                {
                    changeType = ConstantStrings.DraftToCFM;
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
                //3.1 From Finance to CFM Ready
                if (oldStatus.Equals(ConstantStrings.SubmittedFinance) && newStatus.Equals(ConstantStrings.CFMReady))
                {
                    changeType = ConstantStrings.FinanceToCFM;
                }
                //3.2 From Finance to CFM Complete
                if (oldStatus.Equals(ConstantStrings.SubmittedFinance) && newStatus.Equals(ConstantStrings.CFMComplete))
                {
                    changeType = ConstantStrings.FinanceToComplete;
                }
                //3.3 Rejected from WP to Finance
                if (oldStatus.Equals(ConstantStrings.SubmittedWP) && newStatus.Equals(ConstantStrings.SubmittedFinance))
                {
                    changeType = ConstantStrings.WPToFinance;
                }
                //4. WP Review to CFM Ready
                if (oldStatus.Equals(ConstantStrings.SubmittedWP) && newStatus.Equals(ConstantStrings.CFMReady))
                {
                    changeType = ConstantStrings.WPToCFM;
                }
                //5. CFM Ready back to WP Review
                if (oldStatus.Equals(ConstantStrings.CFMReady) && newStatus.Equals(ConstantStrings.SubmittedWP))
                {
                    changeType = ConstantStrings.CFMToWP;
                }
                //5.1 CFM Ready back returned to Draft
                if (oldStatus.Equals(ConstantStrings.CFMReady) && newStatus.Equals(ConstantStrings.Draft))
                {
                    changeType = ConstantStrings.CFMToDraft;
                }
                //5.2 CFM Ready back returned to Finance
                if (oldStatus.Equals(ConstantStrings.CFMReady) && newStatus.Equals(ConstantStrings.SubmittedFinance))
                {
                    changeType = ConstantStrings.CFMToFinance;
                }
                //6. CFM Complete
                if ((oldStatus.Equals(ConstantStrings.CFMReady)) && newStatus.Equals(ConstantStrings.CFMComplete))
                {
                    changeType = ConstantStrings.CFMToComplete;
                }
                //7. CFM Complete back to Draft
                if ((oldStatus.Equals(ConstantStrings.CFMComplete)) && newStatus.Equals(ConstantStrings.Draft))
                {
                    changeType = ConstantStrings.CompleteToDraft;
                }
            }
            return changeType;
        }

        [HttpGet]
        public IActionResult Search(SearchForm search)
        {
            if(search == null)
            {
                return View();
            }
            List<LineItemGroup> results = new List<LineItemGroup>();

            // Contract Only Search
            if(search.SearchContractNumber != null)
            {
                Contract contract = _context.Contracts.AsNoTracking().SingleOrDefault(c => c.ContractNumber == search.SearchContractNumber);
                if (contract != null)
                {
                    results = _pu.GetDeepEncumbrances(contract.ContractID);
                }
            }

            // Date Range Search
            if(search.SearchStartDate != null || search.SearchEndDate != null)
            {
                if(search.SearchStartDate == null ) { search.SearchStartDate = new DateTime(2001, 1, 1);  }
                if(search.SearchEndDate == null ) { search.SearchEndDate = DateTime.Now;  }
                List<LineItemGroup> dateResults = _context.LineItemGroups
                    .AsNoTracking()
                    .Where(e => e.OriginatedDate >= search.SearchStartDate && e.OriginatedDate <= search.SearchEndDate)
                    .ToList();
                foreach(LineItemGroup item in dateResults)
                {
                    results.Add(_pu.GetDeepEncumbrance(item.GroupID));
                }
            }

            // Dollar Amount Search
            if(search.SearchMinAmount != null || search.SearchMaxAmount != null)
            {
                // For now, select on Encumbrance Total from VEncumbrances
                List<int> encumbranceIDs = new List<int>();
                List<LineItemGroup> dollarResults = new List<LineItemGroup>();
                if(search.SearchMaxAmount == null)
                {
                    // Amount >= search.searchMinAmount
                    encumbranceIDs = _context.VEncumbrances
                        .AsNoTracking()
                        .Where(e => e.TotalAmount >= search.SearchMinAmount)
                        .Select(e => e.GroupID)
                        .ToList();
                }
                if(search.SearchMinAmount == null)
                {
                    // Amount <= search.searchMaxAmount
                    encumbranceIDs = _context.VEncumbrances
                        .AsNoTracking()
                        .Where(e => e.TotalAmount <= search.SearchMaxAmount)
                        .Select(e => e.GroupID)
                        .ToList();
                }
                 if(search.SearchMinAmount != null && search.SearchMaxAmount != null)
                {
                    // Amount >= search.SearchMinAmount && Amount <= search.SearchMaxAmount
                    
                    encumbranceIDs = _context.VEncumbrances
                        .AsNoTracking()
                        .Where(e => e.TotalAmount >= search.SearchMinAmount && e.TotalAmount <= search.SearchMaxAmount)
                        .Select(e => e.GroupID)
                        .ToList();
                }
                dollarResults = _context.LineItemGroups
                    .AsNoTracking()
                    .Where(e => encumbranceIDs.Contains(e.GroupID))
                    .ToList();
                foreach(LineItemGroup item in dollarResults)
                {
                    results.Add(_pu.GetDeepEncumbrance(item.GroupID));
                }
            }

            // Get Total Amount for each encumbrance request
            if (results != null && results.Count > 0)
            {
                Dictionary<int, string> encumbranceAmounts = getLineItemGroupAmounts(results);
                ViewBag.EncumbranceAmounts = encumbranceAmounts;
            }
            return View(results);
        }

        //[HttpPost]
        //public IActionResult Search(string contractNumber)
        //{
        //    List<int> contractIDs = _context.Contracts.Where(c => c.ContractNumber == contractNumber).Select(c => c.ContractID).ToList();
        //    List<Contract> contracts = new List<Contract>();
        //    foreach (int contractID in contractIDs)
        //    {
        //        Contract contract = _pu.GetDeepContract(contractID);
        //        contracts.Add(contract);
        //    }
        //    return View(contracts);
        //}
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
                    if (contract == null)
                    {
                        ViewBag.ContractID = 0;
                    }
                    else
                    {
                        ViewBag.ContractID = contract.ContractID;
                    }
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
            if (contract != null)
            {
                return PartialView("NewLineItemPartial", contract);
            }
            return PartialView("NewLineItemPartial");
        }

        [HttpPost]
        public JsonResult AddNewEncumbrance(string lineItemGroup, string comments)
        {
            string statusChange = ConstantStrings.NoChange;
            EncumbranceComment newComment = JsonConvert.DeserializeObject<EncumbranceComment>(comments);
            LineItemGroup newLineItemGroup = JsonConvert.DeserializeObject<LineItemGroup>(lineItemGroup);
            if (newLineItemGroup.LineItemType.Equals(ConstantStrings.Award) && !newComment.status.Equals(ConstantStrings.Draft))
            {
                // TODO: Complete the overloaded Award method before re-enabling this method call
                ManuallyAwardContract(newLineItemGroup);
            }
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
                    oldStatus = existingGroup.CurrentStatus;
                    // If submission is from WP or CFM Ready, 
                    // do not update Contract, LineItemGroup, or LineItems. 
                    // Only 1. update LineItemGroup.CurrentStatus
                    // and  2. Add new Status record.
                    if (!oldStatus.Equals(ConstantStrings.SubmittedWP))
                    {
                        existingGroup.AmendedLineItemID = newLineItemGroup.AmendedLineItemID;
                        existingGroup.ContractID = newLineItemGroup.ContractID;
                        existingGroup.Description = newLineItemGroup.Description;
                        existingGroup.FlairAmendmentID = newLineItemGroup.FlairAmendmentID;
                        existingGroup.IncludesContract = newLineItemGroup.IncludesContract;
                        existingGroup.IsEditable = newLineItemGroup.IsEditable;
                        existingGroup.UserAssignedID = newLineItemGroup.UserAssignedID;
                        existingGroup.LettingDate = newLineItemGroup.LettingDate;
                        existingGroup.AdvertisedDate = newLineItemGroup.AdvertisedDate;
                        existingGroup.RenewalDate = newLineItemGroup.RenewalDate;
                        existingGroup.AmendedFlairLOAID = newLineItemGroup.AmendedFlairLOAID;
                        if (!newLineItemGroup.LineItemType.Equals(existingGroup.LineItemType))
                        {
                            existingGroup.LineItemType = newLineItemGroup.LineItemType;
                        }
                        existingGroup.Contract = contract;
                    }
                    // Update CurrentStatus, even for WP or CFM review
                    existingGroup.LastEditedDate = DateTime.Now;
                    existingGroup.CurrentStatus = newComment.status;
                    existingGroup.LastEditedUserID = newLineItemGroup.LastEditedUserID;
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
                    SubmittalDate = DateTime.Now,
                    ItemReduced = newComment.itemReduced,
                    AmountReduced = newComment.amountReduced
                };
                _context.LineItemGroupStatuses.Add(newStatus);
                _context.SaveChanges();

                // send receipt and notification if Encumbrance is submitted for review
                statusChange = AssessStatusChange(newLineItemGroup.CurrentStatus, oldStatus);
                int msgID = 0;
                // initialize the message service if necessary
                initializeMessageService();
                // send the default notification
                // if notify checkbox is checked, cc: the originator

                List<int> ccIDs = new List<int>();
                if (newComment.notify)
                {
                    ccIDs.Add(newLineItemGroup.OriginatorUserID);
                }
                    
                msgID = _messageService.AddMessage(statusChange, newLineItemGroup, newComment.comments, newComment.wpIDs, ccIDs);
                _messageService.SendEmailMessage(msgID);
                
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
            // return to List if no change in status
            if (statusChange != ConstantStrings.NoChange)
            {
                string url = this.Request.Scheme + "://" + this.Request.Host;
                string redirectURL = url + "/LineItemGroups/List/";
                string redirect = "{\"redirect\" : \"" + redirectURL + "\"}";
                return Json(redirect);
            }
            else
            {
                // return complete, updated object (with GroupID, if new)
                // avoid self-referential loop with unnecessary information in Statuses
                newLineItemGroup.Statuses = null;
                string result = JsonConvert.SerializeObject(newLineItemGroup);
                return Json(result);
            }
        }

        private void ManuallyAwardContract(LineItemGroup award) {
            // If there is an unawarded advertisement for this Contract, 
            // update the Advertisement to zero out the values
            // Get the advertisement. This is a list in case there are multiple. There should be only one.
            // If there is more than one, zero them all out.
            // 
            List<LineItemGroup> ads = _context.LineItemGroups
                    .AsNoTracking()
                    .Where(g => g.ContractID == award.ContractID && g.LineItemType.Equals(ConstantStrings.Advertisement))
                    .ToList();
            foreach(LineItemGroup ad in ads)
            {
                List<LineItem> lineItems = _context.LineItems
                    .AsNoTracking()
                    .Where(l => l.LineItemGroupID == ad.GroupID)
                    .ToList();
                decimal adAmount = 0.00M;
                foreach(LineItem item in lineItems)
                {
                    adAmount += item.Amount;
                }
                if(adAmount > 0) { Award(ad.GroupID, award); }
            }
        }

        public IActionResult Award(int id)
        {
            // Overload of the Award method that gets called from the List page.
            // This is an enhancement to handle a new Award encumbrance from the AddNewEncumbrance method called by the Request page
            // This redirects to the List page when complete
            // 1. Use id to lookup the advertisement
            // 2. Duplicate each LineItem in the Advertisement with identical information except NEGATIVE values for the Amounts
            // 3. Add a LineItemStatus to the advertisement in the Award
            Award(id, null);

            return RedirectToAction("List");
        }

        [HttpGet]
        public IActionResult Award(int id, LineItemGroup award)
        {
            /*  The id is GroupID for the Advertisement Encumbrance request
            *   This method Awards that request in the following way:
            *   1. Create a new encumbrance with the same contract of type ConstantStrings.Award
            *   2. Duplicate each LineItem in the Advertisement with identical information except NEGATIVE values for the Amounts
            *   3. Duplicate each line item of the Advertisement in the Award
            *   4. Open the new Award encumbrance in the Manage page.
            *   5. Show the Award banner warning the user to update the Vendor and Amounts
            *   This Award will replicate the Advertisement, but counter all of it's amounts, leaving the net balance on the contract at $0.00
            */
            bool awardHasLineItems = (award != null && award.GroupID > 0);

            /* Verify there is an existing advertisement */
            LineItemGroup advertisement = _context.LineItemGroups.AsNoTracking().SingleOrDefault(g => g.GroupID == id);
            if(advertisement == null)
            {
                ViewBag.AwardMessage = "This is not a valid advertisement.";
                return RedirectToAction("List");
            }
            /* Verify the contract has not already been awarded.
             * If it has, open Manage() to the prior award. */
            List<LineItemGroup> priorAwards = _context.LineItemGroups.AsNoTracking()
                .Where(g => g.ContractID == advertisement.ContractID && g.LineItemType.Equals(ConstantStrings.Award))
                .ToList();
            if(priorAwards != null && priorAwards.Count() > 0)
            {
                LineItemGroup priorAward = priorAwards[0];
                return RedirectToAction("Manage", new { id = priorAward.GroupID } );
            }

            if (award == null || award.GroupID == 0) {
                // If the award passed in to this method
                int newAwardID = 0;
                try
                {
                    // Make the new Award LineItemGroup record
                    LineItemGroup newAward = new LineItemGroup(advertisement.ContractID, _pu.GetUser(GetLogin()).UserID);
                    newAward.LineItemType = ConstantStrings.Award;
                    newAward.CurrentStatus = ConstantStrings.Draft;
                    newAward.IncludesContract = 1;

                    _context.LineItemGroups.Add(newAward);
                    _context.SaveChanges();
                    newAwardID = newAward.GroupID;
                    award = newAward;
                }
                catch (Exception e)
                {
                    _logger.LogError("LineItemGroupsController.Award Error:" + e.GetBaseException());
                    Log.Error("LineItemGroupsController.Award Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
            }
            Contract contract = _context.Contracts.AsNoTracking()
                .SingleOrDefault(c => c.ContractID == award.ContractID);
            // Make a new Status record for the Award referencing the Advertisement ID
            LineItemGroupStatus awStatus = new LineItemGroupStatus()
            {
                CurrentStatus = ConstantStrings.Draft,
                LineItemGroupID = award.GroupID,
                SubmittalDate = DateTime.Now,
                Comments = "New Award Encumbrance for Contract " + contract.ContractNumber + " for Advertisement #" + advertisement.GroupID + ".",
                UserID = award.OriginatorUserID
            };
            _context.LineItemGroupStatuses.Add(awStatus);

            // Make a new Ststus record for the Advertisement with the Award ID
            LineItemGroupStatus adStatus = new LineItemGroupStatus()
            {
                CurrentStatus = ConstantStrings.CFMComplete,
                LineItemGroupID = advertisement.GroupID,
                SubmittalDate = DateTime.Now,
                Comments = "New Award Encumbrance for Contract " + contract.ContractNumber + " Award #" + award.GroupID + ".",
                UserID = award.OriginatorUserID
            };
            _context.LineItemGroupStatuses.Add(adStatus);

            _context.SaveChanges();

            // Make LineItems with negative values to cancel the value of the Advertisement
            List<LineItem> priorLines = _context.LineItems.AsNoTracking().Where(l => l.LineItemGroupID == id).ToList();
            foreach(LineItem priorLine in priorLines)
            {
                LineItem newLine = new LineItem(priorLine);
                newLine.Amount = priorLine.Amount * (-1);
                newLine.LineItemGroupID = advertisement.GroupID;
                newLine.LineItemType = ConstantStrings.Advertisement;
                _context.LineItems.Add(newLine);
                _context.SaveChanges();
            }
            if (awardHasLineItems) 
            { 
                return RedirectToAction("List");
            }else{
                // Add prior lines to Award
                foreach (LineItem priorLine in priorLines)
                {
                    LineItem newLine = new LineItem(priorLine);
                    newLine.LineItemGroupID = award.GroupID;
                    newLine.LineItemType = ConstantStrings.Award;
                    _context.LineItems.Add(newLine);
                    _context.SaveChanges();
                }

                // Show the award banner on the Manage page
                ViewBag.AwardBanner = true;

                return RedirectToAction("Manage", new { id = award.GroupID });
            }
        }

        [HttpGet]
        public IActionResult Amend(int id)
        {
            /*  The id is GroupID for the Advertisement Encumbrance request
            *   This method creates a new request that is a duplicate of the original in the following way:
            *   1. Create a new encumbrance with the same contract of type ConstantStrings.Amendmen
            *   2. Duplicate each LineItem in the original encumbrance with identical information
            *   3. Open the new Amendment encumbrance in the Manage page.
            *   5. Show the Amend banner warning the user to update the Vendor and Amounts
            */

            /* Select the existing encumbrance */
            LineItemGroup original = _context.LineItemGroups.AsNoTracking().SingleOrDefault(g => g.GroupID == id);
            if (original == null)
            {
                ViewBag.AwardMessage = "This is not a valid encumbrnace.";
                return RedirectToAction("List");
            }

            int newAmendID = 0;
            try
            {
                // Make the new Amendment LineItemGroup record
                LineItemGroup newRequest = new LineItemGroup(original.ContractID, _pu.GetUser(GetLogin()).UserID);
                newRequest.LineItemType = ConstantStrings.Amendment;
                newRequest.CurrentStatus = ConstantStrings.Draft;
                newRequest.IncludesContract = 1;
                newRequest.Description = "Duplicate of Encumbrance #" + original.GroupID + ". " + original.Description;
                _context.LineItemGroups.Add(newRequest);
                _context.SaveChanges();
                newAmendID = newRequest.GroupID;

                Contract contract = _context.Contracts.AsNoTracking()
                    .SingleOrDefault(c => c.ContractID == newRequest.ContractID);
                // Make a new Status record for the Award referencing the Advertisement ID
                LineItemGroupStatus newStatus = new LineItemGroupStatus()
                {
                    CurrentStatus = ConstantStrings.Draft,
                    LineItemGroupID = newRequest.GroupID,
                    SubmittalDate = DateTime.Now,
                    Comments = "New Encumbrance for Contract " + contract.ContractNumber + ", duplicate of Encumbrance #" + original.GroupID + ".",
                    UserID = newRequest.OriginatorUserID
                };
                _context.LineItemGroupStatuses.Add(newStatus);
                _context.SaveChanges();

                // Make copies of LineItems from the original in the newRequest
                List<LineItem> priorLines = _context.LineItems.AsNoTracking().Where(l => l.LineItemGroupID == id).ToList();
                foreach (LineItem priorLine in priorLines)
                {
                    LineItem newLine = new LineItem(priorLine);
                    newLine.LineItemGroupID = newRequest.GroupID;
                    newLine.LineItemType = ConstantStrings.Amendment;
                    _context.LineItems.Add(newLine);
                    _context.SaveChanges();
                }

                // Show the amendment banner on the Manage page
                ViewBag.AmendBanner = true;
            }
            catch (Exception e)
            {
                _logger.LogError("LineItemGroupsController.Amend Error:" + e.GetBaseException());
                Log.Error("LineItemGroupsController.Amend Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
            return RedirectToAction("Manage", new { id = newAmendID });
        }

        [HttpGet]
        public IActionResult GetHistory(string groupID)
        {
            int id = int.Parse(groupID);
            IEnumerable<LineItemGroupStatus> statuses = _pu.GetDeepEncumbranceStatuses(id);
            return Json(statuses);
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

        [HttpPost]
        public JsonResult ExactMatchContract(string searchString)
        {
            var searchSTRING = searchString.ToUpper();
            if (!string.IsNullOrEmpty(searchString))
            {
                List<Contract> ContractList = _context.Contracts
                    .Where(c => (c.ContractNumber.ToUpper().Equals(searchSTRING)))
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
            PopulateViewBag(0);
            User user = ViewBag.CurrentUser;
            Dictionary<string, List<LineItemGroup>> lineItemGroupsMap = new Dictionary<string, List<LineItemGroup>>();
            if (ViewBag.Roles.Contains(ConstantStrings.Originator))
            {
                lineItemGroupsMap.Add("MyRequests", getCurrentUserLineItemGroups(user));
            }
            Dictionary<string, List<LineItemGroup>> categorizedLineItemGroups = getCategorizedLineItemGroups(user);
            foreach(string mapKey in categorizedLineItemGroups.Keys)
            {
                lineItemGroupsMap.Add(mapKey, categorizedLineItemGroups[mapKey]);
            }

            if (ViewBag.Roles.Contains(ConstantStrings.Originator) || ViewBag.Roles.Contains(ConstantStrings.AdminRole))
            {
                lineItemGroupsMap.Add(ConstantStrings.Advertisement, getAdvertisedLineItemGroups());
                // pass a list of valid Advertisement GroupIDs for a lookup in the ViewBag
                List<int> adIDs = new List<int>();
                foreach (LineItemGroup ad in lineItemGroupsMap[ConstantStrings.Advertisement])
                {
                    adIDs.Add(ad.GroupID);
                }
                ViewBag.AdIDs = adIDs;
            }

            Dictionary<int, string> lineItemGroupAmounts = getLineItemGroupAmountsFromMap(lineItemGroupsMap);
            ViewBag.EncumbranceAmounts = lineItemGroupAmounts;
            return View(lineItemGroupsMap);
        }

        [HttpGet]
        public IActionResult GetEncumbranceIDsByStatus(string status)
        {
            string encumbranceStatus = JsonConvert.DeserializeObject<string>(status);
            List<int> encIDs = _context.LineItemGroups
                                .AsNoTracking()
                                .Where(g => g.CurrentStatus.Equals(encumbranceStatus))
                                .OrderBy(g => g.GroupID)
                                .Select(g => g.GroupID)
                                .ToList();
            return Json(encIDs);                                
        }

        [HttpGet]
        public IActionResult GetEncumbranceIDsByStatusAndOriginator(string statusAndID)
        {
            ToDoRequest searchValues = JsonConvert.DeserializeObject<ToDoRequest>(statusAndID);
            string encumbranceStatus = searchValues.status;
            int userID = int.Parse(searchValues.userID);
            List<int> encIDs = _context.LineItemGroups
                                .AsNoTracking()
                                .Where(g => g.CurrentStatus.Equals(encumbranceStatus) && g.OriginatorUserID == userID)
                                .OrderBy(g => g.GroupID)
                                .Select(g => g.GroupID)
                                .ToList();
            return Json(encIDs);
        }
        private Dictionary<string, List<LineItemGroup>> getCategorizedLineItemGroups(User user)
        {
            Dictionary<string, List<LineItemGroup>> results = new Dictionary<string, List<LineItemGroup>>();

            // These originally depended on roles. 
            // New approach is to return all results from non-archived contracts
            // and use roles to determine if links are included in the View
            // This method does NOT return Encumbrances that are CFMComplete
            List<LineItemGroup> allLineIDs = new List<LineItemGroup>();
            if (user == null)
            {
                allLineIDs = _context.LineItemGroups.AsNoTracking()
                .Where(l => l.Contract.CurrentStatus != (ConstantStrings.CloseContract))
                .Include(l => l.Contract)
                .Include(l => l.LineItems)
                .Include(l => l.OriginatorUser)
                .OrderByDescending(l => l.GroupID)
                .Take(200)
                .ToList();
            }
            else
            {
                string roles = _pu.GetUserRoles(user.UserLogin);
                // add Line IDs for Groups in Finance if user has Finance role
                List<LineItemGroup> finLineIDs = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.CurrentStatus.Equals(ConstantStrings.SubmittedFinance))
                    .Include(l => l.Contract)
                    .Include(l => l.LineItems)
                    .Include(l => l.OriginatorUser)
                    .ToList();
                if (roles.Contains(ConstantStrings.FinanceReviewer))
                {
                    results.Add(ConstantStrings.SubmittedFinance, finLineIDs);
                }
                allLineIDs.AddRange(finLineIDs);

                // add Line IDs for Groups in Work Program if user has WP role
                List<LineItemGroup> wpLineIDs = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.CurrentStatus.Equals(ConstantStrings.SubmittedWP))
                    .Include(l => l.Contract)
                    .Include(l => l.LineItems)
                    .Include(l => l.OriginatorUser)
                    .ToList();
                if (roles.Contains(ConstantStrings.WPReviewer))
                {
                    results.Add("WP", wpLineIDs);
                }
                allLineIDs.AddRange(wpLineIDs);

                // add Line IDs for Groups in CFM Ready 
                List<LineItemGroup> cfmLineIDs = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.CurrentStatus.Equals(ConstantStrings.CFMReady))
                    .Include(l => l.Contract)
                    .Include(l => l.LineItems)
                    .Include(l => l.OriginatorUser)
                    .ToList();
                if (roles.Contains(ConstantStrings.CFMSubmitter))
                {
                    results.Add(ConstantStrings.CFMReady, cfmLineIDs);
                }
                allLineIDs.AddRange(cfmLineIDs);

                List<LineItemGroup> origLineIDs = new List<LineItemGroup>();
                if (roles.Contains(ConstantStrings.AdminRole))
                {
                    // add Group IDs for all Groups in Draft if user is Admin
                    origLineIDs = _context.LineItemGroups.AsNoTracking()
                        .Where(l => l.CurrentStatus.Equals(ConstantStrings.Draft))
                        .Include(l => l.Contract)
                        .Include(l => l.LineItems)
                        .Include(l => l.OriginatorUser)
                        .ToList();
                }
                else if(roles.Contains(ConstantStrings.Originator))
                {
                    // add Group IDs for Groups in Draft if user has the originator role and is the originator of the encumbrance
                    origLineIDs = _context.LineItemGroups.AsNoTracking()
                        .Where(l => l.CurrentStatus.Equals(ConstantStrings.Draft))
                        .Where(l => l.OriginatorUser.UserLogin.Equals(user.UserLogin))
                        .Include(l => l.Contract)
                        .Include(l => l.LineItems)
                        .Include(l => l.OriginatorUser)
                        .ToList();
                }
                results.Add(ConstantStrings.Draft, origLineIDs);
                allLineIDs.AddRange(origLineIDs);

                // add Groups that have been input to CFM
                List<LineItemGroup> cfmGroups = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.CurrentStatus.Equals(ConstantStrings.CFMComplete))
                    .Include(l => l.LineItems)
                    .Include(l => l.Contract)
                    .Include(l => l.OriginatorUser)
                    .Where(l => l.Contract.CurrentStatus != ConstantStrings.ContractArchived)
                    .OrderByDescending(l => l.GroupID)
                    .Take(200)
                    .ToList();
                results.Add("Processed", cfmGroups);
                allLineIDs.AddRange(cfmGroups);

                // add  Groups that are closed
                List<LineItemGroup> closeGroups = _context.LineItemGroups.AsNoTracking()
                        .Where(l => l.CurrentStatus.Contains("Closed"))
                        .Include(l => l.LineItems)
                        .Include(l => l.Contract)
                        .Include(l => l.OriginatorUser)
                        .Where(l => l.Contract.CurrentStatus != ConstantStrings.ContractArchived)
                        .OrderByDescending(l => l.GroupID)
                        .Take(200)
                        .ToList();
                results.Add("Closed", closeGroups);
                allLineIDs.AddRange(closeGroups);
            }
            results.Add("Complete", allLineIDs);
            return results;
        }

        private List<LineItemGroup> getCurrentUserLineItemGroups(User user)
        {
            List<LineItemGroup> myGroups = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.OriginatorUser.UserID == user.UserID && l.CurrentStatus != "Closed")
                    .Include(l => l.Contract)
                    .Where(l => l.Contract.CurrentStatus != ConstantStrings.ContractArchived)
                    .Include(l => l.LineItems)
                    .Include(l => l.OriginatorUser)
                    .OrderByDescending(l => l.GroupID)
                    .Take(200)
                    .ToList();

            return myGroups;
        }

        private List<LineItemGroup> getAdvertisedLineItemGroups()
        {
            // add  Groups that are unawarded Advertisements
            List<LineItemGroup> adGroups = _context.LineItemGroups.AsNoTracking()
                .Where(l => l.LineItemType.Equals(ConstantStrings.Advertisement) 
                    &&  l.CurrentStatus.Equals(ConstantStrings.CFMComplete)
                    && l.Contract.CurrentStatus != ConstantStrings.ContractArchived)
                .Include(l => l.Contract)
                .Include(l => l.LineItems)
                .Include(l => l.OriginatorUser)
                .ToList();
            // For each adGroup, if the contract has a matching, submitted, Award group, then add it to Award groups
            List<LineItemGroup> awardedGroups = new List<LineItemGroup>();
            foreach (LineItemGroup adGroup in adGroups)
            {
                int contractID = adGroup.ContractID;
                List<LineItemGroup> awardGroups = _context.LineItemGroups.AsNoTracking()
                    .Where(l => l.LineItemType.Equals(ConstantStrings.Award) && l.ContractID == contractID)
                    .ToList();
                if (awardGroups.Count > 0)
                {
                    awardedGroups.Add(adGroup);
                }
            }
            // return adGroups minus awardedGroups, which contains Advertisement encumbrances minus those with already-submitted Awards
            return adGroups.Except(awardedGroups).ToList();
        }


        private Dictionary<int, string> getLineItemGroupAmountsFromMap(Dictionary<string, List<LineItemGroup>> encumbrances)
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
                        string amountString = Utils.FormatCurrency((decimal)amount); ;
                        EncumbranceAmounts.Add(encumbrance.GroupID, amountString);
                    }
                }
            }
            return EncumbranceAmounts;
        }
        public string FormatCurrency(decimal amount)
        {
            return Utils.FormatCurrency(amount);
        }

        private Dictionary<int, string> getLineItemGroupAmounts(List<LineItemGroup> encumbranceList)
        {
            Dictionary<int, string> EncumbranceAmounts = new Dictionary<int, string>();
            foreach(LineItemGroup encumbrance in encumbranceList)
            {
                if (!EncumbranceAmounts.Keys.Contains(encumbrance.GroupID))
                {
                    decimal amount = GetEncumbranceAmount(encumbrance);
                    string amountString = Utils.FormatCurrency((decimal)amount); ;
                    EncumbranceAmounts.Add(encumbrance.GroupID, amountString);
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

        [HttpPost]
        public JsonResult CloseContract(string closeContract)
        {
            if(closeContract == null) { return Json("{ \"fail\" : \"No information\"}"); }
            ContractClosure closure = JsonConvert.DeserializeObject<ContractClosure>(closeContract);

            string response = "";
            int contractID = int.Parse(closure.ContractID);
            // TODO: Group ID doesn't matter. This closes the contract. Line closures are handled as new encumbrances
            //int groupID = int.Parse(closure.LineItemGroupID);
            PopulateViewBag(contractID);
            User user = ViewBag.CurrentUser;
            //LineItemGroup encumbrance = null;
            Contract contract = null;

            try
            {
                // get contract
                contract = (Contract)_context.Contracts
                    .SingleOrDefault(c => c.ContractID == contractID);
                string closedStatus = closure.ClosureType.Contains("98") ? ConstantStrings.ContractComplete98 : ConstantStrings.ContractComplete50;
                                
                
                /* This is a contract closing, 
                * 1. select all encumbrances for the contract
                * 2. add an encumbrance status Closed 50 or Closed 98 for each encumbrance
                * 3. Set the current status each encumbrance Closed 50 or Closed 98
                * 4. add a contract status Closed 50 or Closed 98
                * 5. set the contract's current status to Closed 50 or Closed 98
                * 6. Send one notification to the Central Office requesting to close the contract
                */
                              
                    List<LineItemGroup> encumbrances = _context.LineItemGroups.Where(li => li.ContractID == contract.ContractID).ToList();
                    foreach(LineItemGroup enc in encumbrances)
                    {
                        enc.CurrentStatus = closedStatus;
                        AddEncumbranceStatus(enc, closedStatus);
                    }
                    contract.CurrentStatus = closedStatus;
                    //save changes to the database
                    _context.Contracts.Update(contract);
                    _context.SaveChanges();
                

                // Send Close Contract/Encumbrance Request to Closers
                initializeMessageService();
                _messageService.SendClosingRequest(closure, user);
                response = "{\"Request Sent\" : \"Closure request sent to closers.\"}";
            }
            catch(Exception e)
            {
                _logger.LogError("LineItemGroupsController.CloseContract Error:" + e.GetBaseException());
                Log.Error("LineItemGroupsController.CloseContract  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
            return Json(response);
        }

        private void AddEncumbranceStatus(LineItemGroup encumbrance, string newStatus)
        {
            LineItemGroupStatus status = new LineItemGroupStatus(encumbrance)
            {
                CurrentStatus = newStatus,
                SubmittalDate = DateTime.Now,
                UserID = ViewBag.CurrentUser.UserID,
            };
            _context.LineItemGroupStatuses.Add(status);
            _context.SaveChanges();
        }

        private void initializeMessageService()
        {
            if (_messageService == null)
            {
                string url = this.Request.Scheme + "://" + this.Request.Host;
                _messageService = new MessageService(_context, SmtpConfig, _logger, url);
            }
        }
    }
}