﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EPS3.DataContexts;
using EPS3.Models;
using EPS3.Helpers;
using EPS3.ViewModels;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EPS3.Controllers
{
    public class LineItemsController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<LineItemsController> _logger;
        private PermissionsUtils _pu;
        public LineItemsController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<LineItemsController>();
            _pu = new PermissionsUtils(_context);
        }

        // GET: LineItems
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<int> userSpecificLineIDs = getUserSpecificLineIDs();
            var lineItems = _context.LineItems
                .Include(l => l.OCA)
                .Include(l => l.Fund)
                .Include(l => l.Category)
                .Include(l => l.StateProgram)
                .Include(l => l.Contract)
                .Include(l => l.LineItemGroup)
                .Where(Utils.BuildOrExpression<LineItem, int>(l => l.LineItemID, userSpecificLineIDs.ToArray<int>()));
            return View(await lineItems.ToListAsync());
        }

        // GET: LineItems
        [HttpGet]
        public IActionResult List()
        {
            Dictionary<string, List<LineItem>> categorizedLineItems = getCategorizedLineItems();

            return View(categorizedLineItems);
        }

        // GET: LineItems/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lineItem = await _context.LineItems
                .SingleOrDefaultAsync(m => m.LineItemID == id);
            if (lineItem == null)
            {
                return NotFound();
            }

            return View(lineItem);
        }

        // GET: LineItems/Create
        [HttpGet]
        public IActionResult Create(int contractID, int groupID)
        {
            string userLogin = GetLogin();
            PopulateViewBag(contractID);

            try
            {
                // id is the LineItemGroup.GroupID 
                var group =  _context.LineItemGroups.Where(lig => lig.GroupID == groupID).SingleOrDefault();
                if (group == null)
                {
                    // if it is zero or does not exist, create a new LineItemGroup and set id = its GroupID
                    LineItemGroup newGroup = new LineItemGroup(ViewBag.Contract, ViewBag.CurrentUser)
                    {
                        CurrentStatus = ConstantStrings.Draft,
                        LastEditedUserID = ViewBag.CurrentUser.UserID,
                        OriginatorUserID = ViewBag.CurrentUser.UserID
                    };
                    _context.LineItemGroups.Add(newGroup);
                    _context.SaveChanges();

                    group = newGroup;
                }
                ViewBag.lineItemGroup = group;
                ViewBag.contractID = ViewBag.Contract.ContractID;

                ViewBag.lineItemTypes = ConstantStrings.GetLineItemTypeList();
                ViewBag.currentFiscalYear = CurrentFiscalYear();
                ViewData["Categories"] = _context.Categories.OrderBy(v => v.CategoryCode);
                ViewData["StatePrograms"] = _context.StatePrograms.OrderBy(v => v.ProgramCode);
                // TODO: This method fails when no related line items are found
                // also the View does not use this to populate a selection list yet.
                //ViewData["FlairLineIDs"] = GetAmendmentsList(contractID);
            }
            catch (Exception e)
            {

                _logger.LogError("ContractsController.Create Error:" + e.GetBaseException());
                Log.Error("ContractsController.Create Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
            return View();
        }

        // POST: LineItems/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LineItemID,ContractID,OrgCode,ExpansionObject,FlairObject,FinancialProjectNumber,FundID,Amount,StateProgramID,OCAID,WorkActivity,CategoryID,FiscalYear,LineItemType,FlairAmendmentID,UserAssignedID,AmendedLineItemID,LineItemGroupID,Comments")] LineItem lineItem)
        {
            // remove "55-" prefix from OrgCode value before saving
            lineItem.OrgCode = CleanOrgCode(lineItem.OrgCode);
            if (ModelState.IsValid)
            {
                try
                {
                    User currentUser = _pu.GetUser(GetLogin());
                    int lineCount = _context.LineItems.Where(li => li.LineItemGroupID == lineItem.LineItemGroupID).Count();
                    lineItem.LineNumber = lineCount + 1;
                    _context.Add(lineItem);
                    await _context.SaveChangesAsync();
                    if(lineItem.Comments != null && lineItem.Comments.Length > 0)
                    {
                        //create new line item comment
                        LineItemStatus lineItemStatus = new LineItemStatus()
                        {
                            LineItemID = lineItem.LineItemID,
                            Comments = lineItem.Comments,
                            SubmittalDate = DateTime.Now,
                            UserID = currentUser.UserID,
                            ContractID = lineItem.ContractID,
                            CurrentStatus = "none"
                        };
                        _context.Add(lineItemStatus);
                        await _context.SaveChangesAsync();
                    }

                    // Update the LineItemGroup with selected values
                    LineItemGroup existingLineItemGroup = _context.LineItemGroups
                        .SingleOrDefault(li => li.GroupID == lineItem.LineItemGroupID);
                    if (!(lineItem.LineItemType.IsNullOrEmpty()))
                    {
                        existingLineItemGroup.LineItemType = lineItem.LineItemType;
                    }

                    if (!(lineItem.FlairAmendmentID.IsNullOrEmpty()))
                    {
                        existingLineItemGroup.FlairAmendmentID = lineItem.FlairAmendmentID;
                    }

                    if (!(lineItem.UserAssignedID.IsNullOrEmpty()))
                    {
                        existingLineItemGroup.UserAssignedID = lineItem.UserAssignedID;
                    }

                    if (!(lineItem.AmendedLineItemID.IsNullOrEmpty()))
                    {
                        existingLineItemGroup.AmendedLineItemID = lineItem.AmendedLineItemID;
                    }
                    _context.LineItemGroups.Update(existingLineItemGroup);
                    _context.SaveChanges();


                    return RedirectToAction("View", "Contracts", new { id = lineItem.ContractID });
                }
                catch (Exception e)
                {
                    _logger.LogError("LineItemsController.Create Error:" + e.GetBaseException());
                    Log.Error("LineItemsController.Create  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
            }
            else
            {
                Log.Information("Model state not valid: ");
                var errors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                Log.Information(errors);

            }
            PopulateViewBag(lineItem.ContractID);
            return View(lineItem);
        }




        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (ViewBag.duplicate == null)
            {
                ViewBag.duplicate = "false";
                if (Request.QueryString.ToString().Contains("duplicate=true")) { ViewBag.duplicate = "true"; }
                if (RouteData.Values.Keys.Contains("duplicate") && RouteData.Values["duplicate"].ToString() == "true") { ViewBag.duplicate = "true";}
            }
            ViewBag.lineItemTypes = ConstantStrings.GetLineItemTypeList();
            var lineItemVM = new LineItemViewModel();
            var lineItem = await _context.LineItems.SingleOrDefaultAsync(m => m.LineItemID == id);
            var contract = await _context.Contracts.SingleOrDefaultAsync(c => c.ContractID == lineItem.ContractID);
            var lineItemGroup = await _context.LineItemGroups.SingleOrDefaultAsync(l => l.GroupID == lineItem.LineItemGroupID);
            if (lineItem == null)
            {
                return NotFound();
            }
            string userLogin = GetLogin();
            try { 
            PopulateViewBag(lineItem.ContractID);
            lineItemVM.LineItem = lineItem;
            lineItemVM.LineItemGroup = lineItemGroup;
            List<LineItemStatus> comments = await _context.LineItemStatuses
                .Where(lis => lis.LineItemID == id)
                .Include(lis => lis.User)
                .OrderByDescending(lis => lis.SubmittalDate)
                .ToListAsync();
            lineItemVM.Comments = comments;

            List<FileAttachment> files = await _context.FileAttachments
                .Where(fa => fa.LineItemID == id)
                .OrderByDescending(fa => fa.AttachmentID)
                .ToListAsync();
            lineItemVM.FileAttachments = files;

            //ViewData["Categories"] = _context.Categories.OrderBy(v => v.CategoryCode);
            ViewData["StatePrograms"] = _context.StatePrograms.OrderBy(v => v.ProgramCode);
            ViewData["FlairLineIDs"] = GetAmendmentsList(id);

            ViewBag.myOCA = _context.OCAs.SingleOrDefault(c => c.OCAID == lineItem.OCAID);
            ViewBag.myFund = _context.Funds.SingleOrDefault(f => f.FundID == lineItem.FundID);
            ViewBag.myCategory = _context.Categories.SingleOrDefault(c => c.CategoryID == lineItem.CategoryID);
            ViewBag.myStateProgram = _context.StatePrograms.SingleOrDefault(p => p.ProgramID == lineItem.StateProgramID);
            ViewBag.LineItemID = lineItem.LineItemID;
            ViewBag.ContractID = contract.ContractID;
            ViewBag.currentFiscalYear = CurrentFiscalYear();
            }
            catch (Exception e)
            {
                _logger.LogError("LineItemsController.Edit Error:" + e.GetBaseException());
                Log.Error("LineItemsController.Edit  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
            return View(lineItemVM);
        }

        // POST: LineItems/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LineItemID, ContractID, OrgCode, FinancialProjectNumber, StateProgramID, CategoryID, WorkActivity, OCAID, ExpansionObject, FlairObject, FundID, FiscalYear, Amount, LineItemType, FlairAmendmentID, UserAssignedID, AmendedLineItemID, LineItemGroupID, LineNumber, Comments")] LineItem lineItem, string comments, int userID, string duplicate)
        {
            // remove "55-" prefix from OrgCode value before saving
            lineItem.OrgCode = CleanOrgCode(lineItem.OrgCode);
            
            if (ModelState.IsValid)
            {
                try
                {
                    int groupID = 0;
                    bool isDirty = false;
                    LineItemGroup lineItemGroup = await _context.LineItemGroups
                            .SingleOrDefaultAsync(g => g.GroupID == lineItem.LineItemGroupID);
                    if(lineItemGroup == null)
                    {
                        lineItemGroup = new LineItemGroup();
                        isDirty = true;
                    }
                    if (lineItemGroup.AmendedLineItemID.IsNullOrEmpty() && !lineItem.AmendedLineItemID.IsNullOrEmpty())
                    {
                        lineItemGroup.AmendedLineItemID = lineItem.AmendedLineItemID;
                        isDirty = true;
                    }
                    if (lineItemGroup.FlairAmendmentID.IsNullOrEmpty() && !lineItem.FlairAmendmentID.IsNullOrEmpty())
                    {
                        lineItemGroup.FlairAmendmentID = lineItem.FlairAmendmentID;
                        isDirty = true;
                    }
                    if (lineItemGroup.UserAssignedID.IsNullOrEmpty() && !lineItem.UserAssignedID.IsNullOrEmpty())
                    {
                        lineItemGroup.UserAssignedID = lineItem.UserAssignedID;
                        isDirty = true;
                    }
                    if (lineItemGroup.OriginatorUserID == 0)
                    {
                        lineItemGroup.OriginatorUserID = userID;
                        isDirty = true;
                    }
                    if (isDirty)
                    {
                        lineItemGroup.IsEditable = 1;
                        lineItemGroup.LastEditedUserID = userID;
                        lineItemGroup.LastEditedDate = DateTime.Now;
                    }
                    if (lineItem.LineItemGroupID != 0)
                    {
                        if (isDirty)
                        {
                            // update appropriately then save

                            _context.LineItemGroups.Update(lineItemGroup);
                            _context.SaveChanges();
                        }
                    }
                    else
                    {
                        // populate all of the properties of the object then save
                        lineItemGroup.ContractID = lineItem.ContractID;
                        lineItemGroup.CurrentStatus = ConstantStrings.Draft;
                        _context.LineItemGroups.Add(lineItemGroup);
                        _context.SaveChanges();
                    }
                    groupID = lineItemGroup.GroupID;

                    // if duplcate is true, then add a new record instead of update existing
                    if (duplicate != null && duplicate.Equals("true"))
                    {
                        lineItem.LineItemID = 0;
                        lineItem.LineItemGroupID = groupID;
                        lineItem.LineNumber = _context.LineItems.Where(li => li.LineItemGroupID == lineItem.LineItemGroupID).Count() + 1;
                        _context.LineItems.Add(lineItem);
                        _context.SaveChanges();
                    }
                    else
                    {
                        _context.LineItems.Update(lineItem);
                        _context.SaveChanges();
                    }

                    // capture Comments in a new LineItemStatus object
                    if(comments != null)
                    {
                        LineItemStatus status = new LineItemStatus()
                        {
                            LineItemID = lineItem.LineItemID,
                            UserID = userID,
                            ContractID = lineItem.ContractID,
                            SubmittalDate = DateTime.Now,
                            Comments = comments,
                            CurrentStatus = "LineItem"
                        };
                        _context.LineItemStatuses.Add(status);
                        _context.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("LineItemsController.Edit Error:" + e.GetBaseException());
                    Log.Error("LineItemsController.Edit  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
                return RedirectToAction("View", "Contracts", new { id = lineItem.ContractID });
            }
            else
            {
                Log.Information("Model state not valid: ");
                var errors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                Log.Information(errors);

            }
            return View(lineItem);
        }

        // GET: LineItems/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lineItem = await _context.LineItems
                .Include(m => m.Category)
                .Include(m => m.Fund)
                .Include(m => m.OCA)
                .Include(m => m.StateProgram)
                .SingleOrDefaultAsync(m => m.LineItemID == id);
            if (lineItem == null)
            {
                return NotFound();
            }

            return View(lineItem);
        }

        // POST: LineItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lineItem = await _context.LineItems.SingleOrDefaultAsync(m => m.LineItemID == id);
            int contractID = lineItem.ContractID;
            _context.LineItems.Remove(lineItem);
            await _context.SaveChangesAsync();
            return RedirectToAction("View", "Contracts", new { id = contractID });
        }

        private bool LineItemExists(int id)
        {
            return _context.LineItems.Any(e => e.LineItemID == id);
        }

        [HttpPost]
        public JsonResult ListEOs(string searchString)
        {
            var searchSTRING = searchString.ToUpper();

            if (!string.IsNullOrEmpty(searchString))
            {
                List<string> EOList = _context.LineItems
                    .Where(l => l.ExpansionObject.ToUpper().StartsWith(searchSTRING))
                    .OrderBy(l => l.ExpansionObject)
                    .Select(l => l.ExpansionObject)
                    .ToList();
                return Json(EOList);
            }
            return new JsonResult(searchString);
        }
        [HttpPost]
        public JsonResult ListWorkActivities(string searchString)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                List<string> WorkActivityList = _context.LineItems
                    .Where(l => l.WorkActivity.ToUpper().StartsWith(searchString))
                    .OrderBy(l => l.WorkActivity)
                    .Select(l => l.WorkActivity)
                    .ToList();
                return Json(WorkActivityList);
            }
            return new JsonResult(searchString);
        }
        
        [HttpPost]
        public JsonResult ListFlairObj(string searchString)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                List<string> FlairObjList = _context.LineItems
                    .Where(l => l.FlairObject.StartsWith(searchString))
                    .OrderBy(l => l.FlairObject)
                    .Select(l => l.FlairObject)
                    .ToList();
                return Json(FlairObjList);
            }
            return new JsonResult(searchString);
        }

        [HttpPost]
        public JsonResult ListFinProjNums(string searchString)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                List<string> FinProjList = _context.LineItems
                    .Where(l => l.FinancialProjectNumber.StartsWith(searchString))
                    .OrderBy(l => l.FinancialProjectNumber)
                    .Select(l => l.FinancialProjectNumber)
                    .Distinct()
                    .ToList();
                JsonResult result =  Json(FinProjList);
                return result;
            }
            return new JsonResult(searchString);
        }

        [HttpPost]
        public JsonResult GetFundName(string fundId)
        {
            if(!string.IsNullOrEmpty(fundId))
            {
                List<Fund> fundInfo = _context.Funds
                    .Where(l => l.FundID.Equals(fundId))
                    .OrderBy(l => l.FundID)
                    .ToList();
                return Json(fundInfo);
            }
            return new JsonResult(fundId);
        }

    //ListOCAs
    [HttpPost]
    public JsonResult ListOCAs(string searchString)
    {
        var searchSTRING = searchString.ToUpper();
        if (!string.IsNullOrEmpty(searchString))
        {
            List<OCA> OCAList = _context.OCAs
                    .Where(o => o.OCACode.Contains(searchSTRING) || o.OCAName.ToUpper().Contains(searchSTRING))
                    .OrderBy(o => o.OCACode)
                    .ToList();

            return Json(OCAList);
        }
        return new JsonResult(searchString);
    }
        //ListCategories
        [HttpPost]
        public JsonResult ListCategories(string searchString)
        {
            var searchSTRING = searchString.ToUpper();
            if (!string.IsNullOrEmpty(searchString))
            {
                List<Category> CategoryList = _context.Categories
                    .Where(c => c.CategoryCode.Contains(searchSTRING) || c.CategoryName.ToUpper().Contains(searchSTRING))
                    .OrderBy(c => c.CategoryCode)
                    .ToList();

                return Json(CategoryList);
            }
            return new JsonResult(searchString);
        }
        //List Funds
        [HttpPost]
        public JsonResult ListFunds(string searchString)
        {
            var searchSTRING = searchString.ToUpper();
            if (!string.IsNullOrEmpty(searchString))
            {
                List<Fund> FundsList = _context.Funds
                    .Where(f => f.FundCode.Contains(searchSTRING))
                    .OrderBy(f => f.FundCode)
                    .ToList();

                return Json(FundsList);
            }
            return new JsonResult(searchString);
        }
        private string CleanOrgCode(string orgCode)
        {
            while (orgCode.Substring(0, 1) == "5" || orgCode.Substring(0, 1) == "-")
            {
                orgCode = orgCode.Substring(1);
            }
            return orgCode;
        }

        private string CurrentFiscalYear()
        {
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            if (month >= 6)
            {
                return year.ToString() + " - " + (year + 1).ToString();
            }
            else
            {
                return (year-1).ToString() + " - " + year.ToString();
            }
        }

        private List<SelectListItem> GetAmendmentsList(int? id)
        {
            List<LineItem> relatedLineItems =  _context.LineItems.Where(v => v.ContractID == id).OrderBy(v => v.FlairAmendmentID).ToList<LineItem>();
            List<SelectListItem> amendmentIDs = new List<SelectListItem>();
            foreach (LineItem item in relatedLineItems)
            {
                SelectListItem selectListItem = new SelectListItem()
                {
                    Text = item.FlairAmendmentID,
                    Value = item.FlairAmendmentID
                };
                amendmentIDs.Add(selectListItem);
            }
            return amendmentIDs;
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
                    Log.Error("ContractsController.PopulateViewBag Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
            }
            else
            {
                RedirectToAction("Home");
            }
        }

        private List<int> getUserSpecificLineIDs()
        {
            List<int> userLineIDs = new List<int>();
            PopulateViewBag(0);
            User user = ViewBag.CurrentUser;

            if (ViewBag.Roles.Contains(ConstantStrings.Originator))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<int> origLineIDs = _context.LineItems.AsNoTracking()
                    .Where(l => l.LineItemGroup.CurrentStatus.Equals(ConstantStrings.Draft))
                    .Where(l => l.Contract.User.UserLogin.Equals(user.UserLogin))
                    .Select(l => l.LineItemID).ToList();
                userLineIDs.AddRange(origLineIDs);
            }
            if (ViewBag.Roles.Contains(ConstantStrings.FinanceReviewer))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<int> finLineIDs = _context.LineItems.AsNoTracking()
                    .Where(l => l.LineItemGroup.CurrentStatus.Equals(ConstantStrings.SubmittedFinance))
                    .Select(l => l.LineItemID).ToList();
                userLineIDs.AddRange(finLineIDs);
            }
            if (ViewBag.Roles.Contains(ConstantStrings.WPReviewer))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<int> wpLineIDs = _context.LineItems.AsNoTracking()
                    .Where(l => l.LineItemGroup.CurrentStatus.Equals(ConstantStrings.SubmittedWP))
                    .Select(l => l.LineItemID).ToList();
                userLineIDs.AddRange(wpLineIDs);
            }
            if (ViewBag.Roles.Contains(ConstantStrings.CFMSubmitter))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<int> cfmLineIDs = _context.LineItems.AsNoTracking()
                    .Where(l => l.LineItemGroup.CurrentStatus.Equals(ConstantStrings.CFMReady))
                    .Select(l => l.LineItemID).ToList();
                userLineIDs.AddRange(cfmLineIDs);
            }
            return userLineIDs;
        }
        private Dictionary<string, List<LineItem>> getCategorizedLineItems()
        {
            Dictionary<string, List<LineItem>> results = new Dictionary<string, List<LineItem>>();
            PopulateViewBag(0);
            User user = ViewBag.CurrentUser;

            if (ViewBag.Roles.Contains(ConstantStrings.Originator))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<LineItem> origLineIDs = _context.LineItems.AsNoTracking()
                    .Where(l => l.LineItemGroup.CurrentStatus.Equals(ConstantStrings.Draft))
                    .Where(l => l.Contract.User.UserLogin.Equals(user.UserLogin))
                    .Include(l => l.OCA)
                    .Include(l => l.Fund)
                    .Include(l => l.Category)
                    .Include(l => l.StateProgram)
                    .Include(l => l.Contract)
                    .Include(l => l.LineItemGroup)
                    .ToList();
                results.Add(ConstantStrings.Originator, origLineIDs);
            }
            if (ViewBag.Roles.Contains(ConstantStrings.FinanceReviewer))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<LineItem> finLineIDs = _context.LineItems.AsNoTracking()
                    .Where(l => l.LineItemGroup.CurrentStatus.Equals(ConstantStrings.SubmittedFinance))
                    .Include(l => l.OCA)
                    .Include(l => l.Fund)
                    .Include(l => l.Category)
                    .Include(l => l.StateProgram)
                    .Include(l => l.Contract)
                    .Include(l => l.LineItemGroup)
                    .ToList();
                results.Add(ConstantStrings.FinanceReviewer, finLineIDs);
            }
            if (ViewBag.Roles.Contains(ConstantStrings.WPReviewer))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<LineItem> wpLineIDs = _context.LineItems.AsNoTracking()
                    .Where(l => l.LineItemGroup.CurrentStatus.Equals(ConstantStrings.SubmittedWP))
                    .Include(l => l.OCA)
                    .Include(l => l.Fund)
                    .Include(l => l.Category)
                    .Include(l => l.StateProgram)
                    .Include(l => l.Contract)
                    .Include(l => l.LineItemGroup)
                    .ToList();
                results.Add(ConstantStrings.WPReviewer, wpLineIDs);
            }
            if (ViewBag.Roles.Contains(ConstantStrings.CFMSubmitter))
            {
                // add Line IDs for Groups in Draft where user is the originator
                List<LineItem> cfmLineIDs = _context.LineItems.AsNoTracking()
                    .Where(l => l.LineItemGroup.CurrentStatus.Equals(ConstantStrings.CFMReady))
                    .Include(l => l.OCA)
                    .Include(l => l.Fund)
                    .Include(l => l.Category)
                    .Include(l => l.StateProgram)
                    .Include(l => l.Contract)
                    .Include(l => l.LineItemGroup)
                    .ToList();
                results.Add(ConstantStrings.CFMSubmitter, cfmLineIDs);
            }
            return results;
        }
    } // end class

}
