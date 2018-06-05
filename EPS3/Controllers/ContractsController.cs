﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EPS3.DataContexts;
using EPS3.Models;
using EPS3.ViewModels;
using Newtonsoft.Json;
using System.Security.Principal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace EPS3.Controllers
{
    public class ContractsController : Controller
    {
        private readonly EPSContext _context;

        public ContractsController(EPSContext context)
        {
            _context = context;
        }

        // GET: Contracts
        public async Task<IActionResult> Index()
        {
            PopulateViewBag("Index", 0);

            var contracts = _context.Contracts
                .Include(c => c.ContractFunding)
                .Include(c => c.MethodOfProcurement)
                .Include(c => c.Vendor)
                .Include(c => c.Recipient)
                .Include(c => c.ContractType);
            string CurrentUserName = "KNAECCS";
            if (this.User.Identity.Name != null)
            {
                CurrentUserName = this.User.Identity.Name.ToUpper();
            }
            var CurrentUser =  await _context.Users
                .Where(u => u.UserLogin == CurrentUserName)
                .AsNoTracking()
                .SingleOrDefaultAsync();
            ViewBag.CurrentUser = CurrentUser;
            return View(await contracts.ToListAsync());
        }
        // GET: Contracts
        public async Task<IActionResult> List()
        {
            PopulateViewBag("List", 0);
            if (TempData["CurrentUser"] == null)            { 
                string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
                User currentUser = (User) _context.Users
                    .Include(u => u.Roles)
                    .SingleOrDefault(u => u.UserLogin.Equals(username));
                if (currentUser != null)
                {
                    User user = (User)TempData["CurrentUser"];
                }
            }
           
            var CurrentUser = (User)ViewBag.CurrentUser;
            var userID = CurrentUser.UserID;
            var contracts = _context.Contracts
                .Include(c => c.Vendor)
                .Include(c => c.ContractType)
                .Include(c => c.User)
                .Where(c => c.UserID == userID);
            return View(await contracts.ToListAsync());
        }

        // GET: Contracts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            PopulateViewBag("Details", id);
            var contract = await _context.Contracts
                .Include(c => c.ContractFunding)
                .Include(c => c.MethodOfProcurement)
                .Include(c => c.Vendor)
                .Include(c => c.ContractType)
                .Include(c => c.Recipient)
                .SingleOrDefaultAsync(c => c.ContractID == id);

            var lineItems =  _context.LineItems
                .Include(l => l.Category)
                .Include(l => l.Fund)
                .Include(l => l.OCA)
                .Include(l => l.StateProgram)
                .Where(l => l.ContractID == id)
                .AsNoTracking();

            var statuses = _context.ContractStatuses
                .Include(cs => cs.User)
                .Where(cs => cs.ContractID == id)
                .OrderByDescending(cs => cs.SubmittalDate)
                .AsNoTracking();
            if (contract == null)
            {
                return NotFound();
            }

            ViewData["LineItems"] = lineItems;
            ViewData["Statuses"] = statuses;
            return View(contract);
            //return View(await lineItems.AsNoTracking().ToListAsync());
        }

        // GET: Contracts/Create
        public IActionResult Create()
        {
            PopulateViewBag("Create", 0);
            ViewData["Procurements"] = _context.Procurements.OrderBy(p => p.ProcurementCode);
            ViewData["Compensations"] = _context.Compensations.OrderBy(c => c.CompensationID);
            ViewData["Vendors"] = _context.Vendors.OrderBy(v => v.VendorName);
            ViewData["Recipients"] = _context.Recipients.OrderBy(v => v.RecipientCode);
            return View();
        }

        // POST: Contracts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserID,ContractID,ContractNumber,ContractTypeID,RecipientID,ProcurementID,CompensationID,IsRenewable,ContractTotal,MaxLoaAmount,BudgetCeiling,VendorID,BeginningDate,EndingDate,ServiceEndingDate,DescriptionOfWork,CurrentStatus")] Contract contract)
        {
            if (ModelState.IsValid)
            {
                contract.CurrentStatus = StatusType.Draft.ToString();
                contract.CreatedDate = DateTime.Now.Date;
                //get current user
                User currentUser = await _context.Users
                    .Include(u => u.Roles)
                    .SingleOrDefaultAsync(u => u.UserID == contract.UserID);
                 //Session.["UserLogin"] = currentUser.UserLogin;

                //Add a ContractStatus record for a new Contract
                ContractStatus newStatus = new ContractStatus(currentUser, contract, ConstantStrings.NewContract);
                if (contract.Statuses == null)
                {
                    contract.Statuses = new List<ContractStatus>();
                }
                contract.Statuses.Add(newStatus);
                _context.Add(contract);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contract);
        }

        // GET: Contracts/Review/5
        public async Task<IActionResult> Review(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            PopulateViewBag("Review", id);
            var contractVM = new ContractViewModel();
            contractVM.Contract = await _context.Contracts.SingleOrDefaultAsync(m => m.ContractID == id);
            if (contractVM.Contract != null)
            {
                var lineitems = _context.LineItems
                    .Include(li => li.StateProgram)
                    .Include(li => li.Category)
                    .Include(li => li.OCA)
                    .Include(li => li.Fund)
                    .Where(li => li.ContractID == id);
                contractVM.LineItems = await lineitems.ToListAsync();
                var statuses = _context.ContractStatuses
                    .Include(s => s.User)
                    .Where(s => s.ContractID == id)
                    .OrderByDescending(s => s.SubmittalDate);
                contractVM.Statuses = await statuses.ToListAsync();
            }
            if (contractVM == null)
            {
                return NotFound();
            }
            ViewBag.myContractType = _context.ContractTypes.SingleOrDefault(c => c.ContractTypeID == contractVM.Contract.ContractTypeID);
            ViewBag.myVendor = _context.Vendors.SingleOrDefault(v => v.VendorID == contractVM.Contract.VendorID);
            return View(contractVM);
        }
        // POST: Contracts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int contractID, int userID, string currentStatus, string comments)
        {
            if (contractID <= 0)
            {
                return NotFound();
            }
            Contract contract = _context.Contracts.SingleOrDefault(c => c.ContractID == contractID);

            if (ModelState.IsValid)
            {
                try
                {
                    //Create new status record for Contract
                    User currentUser = await _context.Users
                        .AsNoTracking()
                        .SingleOrDefaultAsync(u => u.UserID == userID);
                    ContractStatus newStatus = new ContractStatus(currentUser, contract, currentStatus);
                    newStatus.Comments = comments;

                    contract.CurrentStatus = currentStatus;
                    contract.ModifiedDate = DateTime.Now;
                    //contract.Statuses.Add(newStatus);
                    _context.Update(newStatus);
                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContractExists(contract.ContractID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContractTypes"] = new SelectList(_context.ContractTypes, "ContractTypeID", "ContractTypeID", contract.ContractType);
            ViewData["Compensations"] = new SelectList(_context.Compensations, "CompensationID", "CompensationID", contract.CompensationID);
            ViewData["Procurements"] = new SelectList(_context.Procurements, "ProcurementID", "ProcurementID", contract.ProcurementID);
            ViewData["Recipients"] = new SelectList(_context.Recipients, "RecipientID", "RecipientID", contract.RecipientID);
            ViewData["Vendors"] = new SelectList(_context.Vendors, "VendorID", "VendorID", contract.VendorID);
            return View(contract);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            PopulateViewBag("Edit", id);
            var contractVM = new ContractViewModel();
            contractVM.Contract = await _context.Contracts.SingleOrDefaultAsync(m => m.ContractID == id);
            if (contractVM.Contract != null)
            {
                var lineitems = _context.LineItems
                    .Include(li => li.StateProgram)
                    .Include(li => li.Category)
                    .Include(li => li.OCA)
                    .Include(li => li.Fund)
                    .Where(li => li.ContractID == id);
                contractVM.LineItems = await lineitems.ToListAsync();
                var statuses = _context.ContractStatuses
                    .Include(s => s.User)
                    .Where(s => s.ContractID == id)
                    .OrderByDescending(s => s.SubmittalDate);
                contractVM.Statuses = await statuses.ToListAsync();
            }
            
            if (contractVM == null)
            {
                return NotFound();
            }
            ViewData["ContractTypes"] = _context.ContractTypes.OrderBy(c => c.ContractTypeCode);
            ViewData["Procurements"] = _context.Procurements.OrderBy(p => p.ProcurementCode);
            ViewData["Compensations"] = _context.Compensations.OrderBy(c => c.CompensationID);
            ViewData["Vendors"] = _context.Vendors.OrderBy(v => v.VendorName);
            ViewData["Recipients"] = _context.Recipients.OrderBy(v => v.RecipientName);

            ViewData["Categories"] = _context.Categories.OrderBy(v => v.CategoryCode);
            ViewData["StatePrograms"] = _context.StatePrograms.OrderBy(v => v.ProgramCode);
            ViewBag.myContractType = _context.ContractTypes.SingleOrDefault(c => c.ContractTypeID == contractVM.Contract.ContractTypeID);
            ViewBag.myVendor = _context.Vendors.SingleOrDefault(v => v.VendorID == contractVM.Contract.VendorID);
            return View(contractVM);
        }

        // POST: Contracts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContractID,ContractNumber,ContractTypeID,ProcurementID,CompensationID,IsRenewable,ContractTotal,MaxLoaAmount,BudgetCeiling,VendorID,RecipientID,BeginningDate,EndingDate,ServiceEndingDate,DescriptionOfWork,UserID,CurrentStatus")] Contract contract, string Comments)
        {
            if (id != contract.ContractID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    contract.ModifiedDate = DateTime.Now.Date;
                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContractExists(contract.ContractID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContractTypes"] = new SelectList(_context.ContractTypes, "ContractTypeID", "ContractTypeID", contract.ContractType);
            ViewData["Compensations"] = new SelectList(_context.Compensations, "CompensationID", "CompensationID", contract.CompensationID);
            ViewData["Procurements"] = new SelectList(_context.Procurements, "ProcurementID", "ProcurementID", contract.ProcurementID);
            ViewData["Recipients"] = new SelectList(_context.Recipients, "RecipientID", "RecipientID", contract.RecipientID);
            ViewData["Vendors"] = new SelectList(_context.Vendors, "VendorID", "VendorID", contract.VendorID);
            return View(contract);
        }

        // GET: Contracts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Include(c => c.ContractFunding)
                .Include(c => c.MethodOfProcurement)
                .Include(c => c.Vendor)
                .SingleOrDefaultAsync(m => m.ContractID == id);
            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }

        // POST: Contracts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.SingleOrDefaultAsync(m => m.ContractID == id);
            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContractExists(int id)
        {
            return _context.Contracts.Any(e => e.ContractID == id);
        }
        [HttpPost]
        public JsonResult ListVendors(string searchString)
        {
            var searchSTRING = searchString.ToUpper();
            if (!string.IsNullOrEmpty(searchString))
            {
                var VendorList = (from N in _context.Vendors.ToList()
                                  where N.VendorCode.Contains(searchSTRING) ||
                                  N.VendorName.ToUpper().Contains(searchSTRING)
                                  orderby N.VendorCode
                              select new { N.VendorID, N.VendorCode, N.VendorName });
                return Json(VendorList);
            }
            return new JsonResult(searchString);
        }
        [HttpPost]
        public JsonResult ListContractTypes(string searchString)
        {
            var searchSTRING = searchString.ToUpper();
            if (!string.IsNullOrEmpty(searchString))
            {
                var ContractTypeList = (from N in _context.ContractTypes.ToList()
                                        where N.ContractTypeCode.Contains(searchSTRING) ||
                                        N.ContractTypeName.ToUpper().Contains(searchSTRING)
                                        orderby N.ContractTypeCode
                                        select new { N.ContractTypeID, N.ContractTypeCode, N.ContractTypeName });

                return Json(ContractTypeList);
            }
            return new JsonResult(searchString);
        }
        public  void PopulateViewBag(string action, int? contractID)
        {
            //const string sessionKey = "CurrentUser";
            string userLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            if (userLogin == null || userLogin.Contains("sands"))
            {
                userLogin = "KNAECCS";
            }
            //string value = HttpContext.Session.GetString(sessionKey)==null ? null :  JsonConvert.ToString(HttpContext.Session.GetString(sessionKey));
            if (userLogin != null)
            {
                User currentUser =  _context.Users
                .Where(u => u.UserLogin == userLogin)
                .AsNoTracking()
                .SingleOrDefault();

                    
                //if (currentUser == null)
                //{
                //    RedirectToAction("Home");
                //}else
                //{
                    List<UserRole> rolesList =  _context.UserRoles
                    .Where(ur => ur.UserID == currentUser.UserID)
                    .AsNoTracking()
                    .ToList();
                    int userID = currentUser.UserID;
                    string roles = "";
                    for(int i = 0; i < rolesList.Count; i++)
                    {
                        roles += rolesList[i].Role;
                    }
                    var serialisedUser = JsonConvert.SerializeObject(currentUser);
                    //HttpContext.Session.SetString(sessionKey, serialisedUser.ToString());

                    int selectedContractID = contractID != null ? (int)contractID : 0;
                    string status = "";
                    int contractOriginator = 0;
                Contract contract = null;
                if (selectedContractID > 0)
                    {
                        contract =  _context.Contracts
                        .Where(c => c.ContractID == selectedContractID)
                        .AsNoTracking()
                        .SingleOrDefault();
                        status = contract.CurrentStatus;
                        contractOriginator = contract.UserID;
                    }
                ViewBag.Contract = contract;
                ViewBag.CurrentUser = currentUser;
                ViewBag.Roles = roles;
                //RedirectAllActions(action, userID, selectedContractID, contractOriginator, roles, status);
                // }
            }
            else
            {
                RedirectToAction("Home");

            }

        }

        private void RedirectAllActions(string action, int userID, int contractID, int contractOriginator, string roles, string status)
        {
            List<string> AllowedActions = new List<string>();
            string Message = "";
            ViewBag.Roles = roles;
            AllowedActions.Add("Create");
            switch (action)
            {
                case "Index":

                    if (roles.Contains(ConstantStrings.Originator))
                    {
                        AllowedActions.Add("Create"); //enable Add New Contract link
                        AllowedActions.Add("Edit"); //enable Edit links for projects with same UserID
                    }
                    else
                    {
                        AllowedActions.Add("Details"); // show only Details links
                    }
                    break;
                case "Edit":

                    if (roles.Contains(ConstantStrings.Originator) && userID == contractOriginator && status == ConstantStrings.Draft)
                    {
                        AllowedActions.Add("Edit"); //enable the form
                    }
                    if (status == ConstantStrings.Submitted && roles.Contains(ConstantStrings.FinanceReviewer))
                    {
                        ViewBag.AllowedActions = "Finance, Review";
                        RedirectToAction("Review");
                    }
                    if (status == ConstantStrings.FinanceApproved && roles.Contains(ConstantStrings.WPReviewer))
                    {
                        ViewBag.AllowedActions = "WorkProgram, Review";
                        RedirectToAction("Review");
                    }
                    if (status == ConstantStrings.WPApproved && roles.Contains(ConstantStrings.CFMSubmitter))
                    {
                        ViewBag.AllowedActions = "CFM, Review";
                        RedirectToAction("Review");
                    }
                    if ((status == ConstantStrings.CompleteInvalid || status == ConstantStrings.CompleteWorkDone) && roles.Contains(ConstantStrings.FinanceReviewer))
                    {
                        ViewBag.AllowedActions = "Complete";
                        RedirectToAction("Details");
                    }
                    else
                    {
                        RedirectToAction("Details");
                        Message = "You can not edit this contract at this time.";
                    }
                    break;
                case "Create":

                    if (roles.Contains(ConstantStrings.Originator))
                    {
                        AllowedActions.Add("Create");
                    }
                    else
                    {
                        RedirectToAction("Index");
                        Message = "You do not have permission to create a new contract.";
                    }
                    break;
                case "Review":

                    if (status == ConstantStrings.Submitted && roles.Contains(ConstantStrings.FinanceReviewer))
                    {
                        AllowedActions.Add("Finance"); //enable finance review and editing the form
                    }
                    if (status == ConstantStrings.FinanceApproved && roles.Contains(ConstantStrings.WPReviewer))
                    {
                        AllowedActions.Add("WorkProgram");
                    }
                    if (status == ConstantStrings.CFMReady && roles.Contains(ConstantStrings.CFMSubmitter))
                    {
                        AllowedActions.Add("CFM");
                    }
                    if (status == ConstantStrings.CFMReady && roles.Contains(ConstantStrings.FinanceReviewer))
                    {
                        AllowedActions.Add("Complete");
                    }
                    break;
                 default:
                    break;

            }
            if (roles.Contains(ConstantStrings.AdminRole))
            {
                AllowedActions.Add("Create");
                AllowedActions.Add("Edit");
                AllowedActions.Add("Finance");
                AllowedActions.Add("WorkProgram");
                AllowedActions.Add("CFM");
            }
            ViewBag.AllowedActions = AllowedActions.ToString();
            ViewBag.Message = Message;
        }
    }
}
