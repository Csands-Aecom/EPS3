using System;
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
using EPS3.Helpers;
using EPS3.ViewModels;
using Newtonsoft.Json;
using System.Security.Principal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Net.Mail;
using System.Net;

namespace EPS3.Controllers
{
    public class ContractsController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<ContractsController> _logger;
        private PermissionsUtils _pu;

        public ContractsController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<ContractsController>();
            _pu = new PermissionsUtils(_context);
        }

        // GET: Contracts
        public async Task<IActionResult> Index()
        {
            string userLogin = GetLogin();
            PopulateViewBag(0);

            var contracts = _context.Contracts
                .Include(c => c.ContractFunding)
                .Include(c => c.MethodOfProcurement)
                .Include(c => c.Vendor)
                .Include(c => c.Recipient)
                .Include(c => c.ContractType);
            User currentUser = ViewBag.CurrentUser;
            if (currentUser == null)
            {
                RedirectToRoutePermanent("Contact");
            }
            return View(await contracts.ToListAsync());
        }
        // GET: ALL Contracts
        [HttpGet]
        public IActionResult ListAll()
        {
            string userLogin = GetLogin();
            PopulateViewBag(0);
            var CurrentUser = (User)ViewBag.CurrentUser;
            List<Contract> contracts = GetActiveContracts();
            return View( contracts);
        }

        // GET: Contracts
        [HttpGet]
        public IActionResult List()
        {
            string userLogin = GetLogin();
            PopulateViewBag(0);
            var CurrentUser = (User)ViewBag.CurrentUser;
            List<Contract> contracts  = GetContracts(CurrentUser);
            return View(contracts);
        }
        // GET: Contracts/Details/5
        public  IActionResult Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }
            EncumbranceRequestViewModel erViewModel = new EncumbranceRequestViewModel();

            string userLogin = GetLogin();
            PopulateViewBag(id);
            try
            {
                Contract contract = _pu.GetDeepContract(id);
                List<LineItemGroup> lineItemGroups =  _context.LineItemGroups
                    .Where(l => l.ContractID == id)
                    .Include(l => l.LastEditedUser)
                    .Include(l => l.OriginatorUser)
                    .Include(l => l.LineItems).ThenInclude(li => li.OCA)
                    .Include(l => l.LineItems).ThenInclude(li => li.Category)
                    .Include(l => l.LineItems).ThenInclude(li => li.StateProgram)
                    .Include(l => l.LineItems).ThenInclude(li => li.Fund)
                    .Include(l => l.LineItems).ThenInclude(li => li.Statuses).ThenInclude(lst => lst.User)
                    .Include(l => l.Statuses).ThenInclude(gst => gst.User)
                    .AsNoTracking()
                    .ToList();
                erViewModel.Contract = contract;
                erViewModel.LineItemGroups = lineItemGroups;

                Dictionary<int, decimal> groupAmounts = new Dictionary<int, decimal>();
                decimal contractAmount = 0.0m;
                foreach (LineItemGroup encumbrance in lineItemGroups)
                {
                    decimal groupAmount = 0.0m;
                    foreach(LineItem lineItem in encumbrance.LineItems)
                    {
                        groupAmount += lineItem.Amount;
                    }
                    groupAmounts.Add(encumbrance.GroupID, groupAmount);
                    contractAmount += groupAmount;
                }
                ViewBag.GroupAmounts = groupAmounts;
                ViewBag.ContractAmount = contractAmount;
            }
            catch (Exception e)
            {
                _logger.LogError("ContractsController.Edit Error:" + e.GetBaseException());
                Log.Error("ContractsController.Edit Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
            return View(erViewModel);
        }

        // GET: Contracts/Create
        public IActionResult Create()
        {
            string userLogin = GetLogin();
            PopulateViewBag(0);
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
            if (contract.VendorID == 0)
            {
                contract.VendorID = 1; // placeholder value Advertising
            }
            if (ModelState.IsValid)
            {
                try
                {
                    contract.CurrentStatus = ConstantStrings.ContractDrafted;
                    contract.CreatedDate = DateTime.Now.Date;
                    contract.ModifiedDate = DateTime.Now.Date;
                    
                    if (contract.ContractNumber.IsNullOrEmpty())
                    {
                        contract.ContractNumber = "New"; // placeholder text for pending generation of ContractNumber
                    }
                    contract.ContractNumber = contract.ContractNumber.ToUpper();
                    _context.Contracts.Add(contract);
                    _context.SaveChanges();
                    //get current user
                    User currentUser = await _context.Users
                        .Include(u => u.Roles)
                        .SingleOrDefaultAsync(u => u.UserID == contract.UserID);

                    //Add a ContractStatus record for a new Contract
                    ContractStatus newStatus = new ContractStatus(currentUser, contract, ConstantStrings.ContractNew);
                    newStatus.Comments = "Contract " + contract.ContractNumber + " originated on " + contract.CreatedDate + " by " + currentUser.FullName + ".";
                    newStatus.ContractID = contract.ContractID;
                    _context.ContractStatuses.Add(newStatus);
                    _context.SaveChanges();
                    PopulateViewBag(contract.ContractID);
                    return RedirectToAction("Details", new { id = contract.ContractID });
                }catch(Exception e)
                {
                    _logger.LogError("ContractsController.Create Error:" + e.GetBaseException());
                    Log.Error("ContractsController.Create Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
            }
            else
            {
                PopulateViewBag(contract.ContractID);
                Log.Information("Model state not valid: ");
                var errors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                Log.Information(errors);
                
            }
            return View(contract);
        }

        [HttpPost]
        public JsonResult AddNewContract(string contract)
        {
            Contract newContract = JsonConvert.DeserializeObject<Contract>(contract);
            if(newContract.ServiceEndingDate < new DateTime(2000, 01, 01))
            {
                // ServiceEndingDate is empty or prior to Y2k so set to null
                newContract.ServiceEndingDate = null;
            }
            
            newContract.ContractNumber = newContract.ContractNumber.ToUpper();
            try
            {
                if (newContract.ContractNumber == null || newContract.ContractNumber == "")
                {
                    newContract.ContractNumber = "NEW";
                }
                // set Vendor
                Vendor vendor = _context.Vendors.SingleOrDefault(v => v.VendorID == newContract.VendorID);
                newContract.Vendor = vendor;

                // check if contract exists, if so, update it.
                // if not, add it
                if (newContract.ContractID > 0)
                {
                    Contract existingContract = _context.Contracts.AsNoTracking()
                        .Include(c => c.ContractType)
                        .Include(c => c.Vendor)
                        .SingleOrDefault(c => c.ContractID == newContract.ContractID);

                    UpdateExistingContract(existingContract, newContract);
                    existingContract.ModifiedDate = DateTime.Now;
                    _context.Contracts.Update(existingContract);
                    _context.SaveChanges();
                }
                else if (newContract.UserID > 0)
                {
                    newContract.CreatedDate = DateTime.Now;
                    newContract.ModifiedDate = DateTime.Now;
                    _context.Contracts.Add(newContract);
                    _context.SaveChanges();
                }
                else
                {
                    return (Json("{\"success\": \"false\"}"));
                }
            }
            catch (Exception e) {
                _logger.LogError("ContractsController.AddNewContract Error:" + e.GetBaseException());
            }
            ExtendedContract wrapperContract = GetExtendedContract(newContract.ContractID);
            string result = JsonConvert.SerializeObject(wrapperContract);
            return Json(result);
        }

        private Contract UpdateExistingContract(Contract existingContract, Contract newContract)
        {
            // TODO: for each property, copy from newContract to existingContract
            if (existingContract.ContractID == newContract.ContractID)
            {
                existingContract.BeginningDate = newContract.BeginningDate;
                existingContract.BudgetCeiling = newContract.BudgetCeiling;
                existingContract.CompensationID = newContract.CompensationID;
                existingContract.ContractFunding = newContract.ContractFunding; //
                existingContract.ContractNumber = newContract.ContractNumber;
                existingContract.ContractTotal = newContract.ContractTotal;
                //existingContract.ContractType = newContract.ContractType;
                existingContract.ContractTypeID = newContract.ContractTypeID;
                existingContract.CurrentStatus = newContract.CurrentStatus;
                existingContract.DescriptionOfWork = newContract.DescriptionOfWork;
                existingContract.EndingDate = newContract.EndingDate;
                existingContract.IsRenewable = newContract.IsRenewable;
                existingContract.MaxLoaAmount = newContract.MaxLoaAmount;
                //existingContract.MethodOfProcurement = newContract.MethodOfProcurement;
                existingContract.ModifiedDate = newContract.ModifiedDate;
                existingContract.ProcurementID = newContract.ProcurementID;
                //existingContract.Recipient = newContract.Recipient;
                existingContract.RecipientID = newContract.RecipientID;
                existingContract.VendorID = newContract.VendorID;
                existingContract.ServiceEndingDate = newContract.ServiceEndingDate;
            }
            return existingContract;
        }

        [HttpPost]
        public JsonResult GetDisplayContract(int contractID)
        {
            return Json(JsonConvert.SerializeObject(GetExtendedContract(contractID)));
        }

        [HttpGet]
        // GET: Contracts/Review/5
        public async Task<IActionResult> Review(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }
            string userLogin = GetLogin();
            PopulateViewBag(id);
            var contractVM = new ContractViewModel()
            {
                Contract = await _context.Contracts.SingleOrDefaultAsync(m => m.ContractID == id)
            };
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

        [HttpPost]
        public JsonResult GetContractAmountTotal(string contractInfo)
        {
            dynamic lookupInfo = JsonConvert.DeserializeObject(contractInfo);
            if (contractInfo == "{}") { return Json(0.00m); }
            int contractID = lookupInfo.contractID;

            List<LineItem> lineItems = _context.LineItems.AsNoTracking().Where(l => l.ContractID == contractID).ToList();
            decimal total = 0.00M;
            // TODO: This does not account for Advertisements and Awards. Depending how they are input, we may need to exclude Advertisements.
            foreach (LineItem item in lineItems)
            {
                total += item.Amount;
            }
            return Json(total);
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
        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }
            EncumbranceRequestViewModel erViewModel = new EncumbranceRequestViewModel();

            string userLogin = GetLogin();
            PopulateViewBag(id);
            try
            {
                Contract contract = _context.Contracts
                    .Include(c => c.ContractType)
                    .Include(c => c.MethodOfProcurement)
                    .Include(c => c.ContractFunding)
                    .Include(c => c.User)
                    .Include(c => c.Vendor)
                    .Include(c => c.Recipient)
                    .SingleOrDefault(c => c.ContractID == id);
                erViewModel.Contract = contract;
                List<LineItemGroup> lineItemGroups = await _context.LineItemGroups
                    .Where(l => l.ContractID == id)
                    .Include(l => l.LastEditedUser)
                    .Include(l => l.OriginatorUser)
                    .Include(l => l.LineItems).ThenInclude(li => li.OCA)
                    .Include(l => l.LineItems).ThenInclude(li => li.Category)
                    .Include(l => l.LineItems).ThenInclude(li => li.StateProgram)
                    .Include(l => l.LineItems).ThenInclude(li => li.Fund)
                    .Include(l => l.LineItems).ThenInclude(li => li.Statuses).ThenInclude(lst => lst.User)
                    .Include(l => l.Statuses).ThenInclude(gst => gst.User)
                    .AsNoTracking()
                    .ToListAsync();
                erViewModel.LineItemGroups = lineItemGroups;

                // get amount totals for contract and line item group
                decimal contractSum = 0M;
                Dictionary<int, decimal> groupSums = new Dictionary<int, decimal>();
                foreach(LineItemGroup lig in lineItemGroups)
                {
                    decimal groupSum = 0M;
                    foreach(LineItem li in lig.LineItems)
                    {
                        contractSum += li.Amount;
                        groupSum += li.Amount;
                    }
                    groupSums.Add(lig.GroupID, groupSum);
                }
                ViewBag.ContractTotalAmount = contractSum;
                ViewBag.GroupTotalAmounts = groupSums;
                ViewBag.LineItemTypes = ConstantStrings.GetLineItemTypeList();
                ViewBag.ContractStatusSelectionList = GetContractStatusList(contract, ViewBag.Roles);
                ViewBag.SelectStatusDropdown = _pu.GetStatusDropdown(contract, (User) ViewBag.CurrentUser);
                ViewBag.WPReviewers = GetWpReviewersList();
            }
            catch (Exception e)
            {
                _logger.LogError("ContractsController.Edit Error:" + e.GetBaseException());
                Log.Error("ContractsController.Edit Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
            return View(erViewModel);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> View()
        //{

        //}
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }
            string userLogin = GetLogin();
            PopulateViewBag(id);
            var contractVM = new ContractViewModel();
            contractVM.Contract = await _context.Contracts
                .Include(m => m.User)
                .SingleOrDefaultAsync(m => m.ContractID == id);
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
            ViewBag.ContractStatusSelectionList = GetContractStatusList(contractVM.Contract, ViewBag.Roles);
            return View(contractVM);
        }

        // POST: Contracts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContractID,ContractNumber,ContractTypeID,ProcurementID,CompensationID,IsRenewable,ContractTotal,MaxLoaAmount,BudgetCeiling,VendorID,RecipientID,BeginningDate,EndingDate,ServiceEndingDate,DescriptionOfWork,UserID,CurrentStatus")] Contract contract, string Comments, int CurrentUserID)
        {
            if (id != contract.ContractID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // compare old and new contract.CurrentStatus. If it has changed:
                    Contract oldContract = _context.Contracts.AsNoTracking().SingleOrDefault(c => c.ContractID == contract.ContractID);
                    // 1. ConstantStrings.LookupConstant( contract.CurrentStatus)
                    contract.CurrentStatus = ConstantStrings.LookupConstant(contract.CurrentStatus);
                    bool statusChanged = oldContract.CurrentStatus != contract.CurrentStatus;
                    if(statusChanged)
                    {
                        User currentUser = _context.Users.AsNoTracking().SingleOrDefault(u => u.UserID == CurrentUserID);
                        // 2. add a new Contract Status record
                        ContractStatus newStatus = new ContractStatus
                        {
                            Comments = Comments,
                            Contract = contract,
                            CurrentStatus = contract.CurrentStatus,
                            User = currentUser,
                            SubmittalDate = DateTime.Now
                        };
                        _context.ContractStatuses.Add(newStatus);
                        _context.SaveChanges();
                    }
                    contract.ModifiedDate = DateTime.Now.Date;
                    _context.Update(contract);
                    await _context.SaveChangesAsync();

                    // 3. send any appropriate emails
                    if (statusChanged)
                    {
                        if (contract.CurrentStatus.Equals(ConstantStrings.ContractInFinance))
                        {
                            // send receipt to Originator
                            // send notice to Finance
                        }
                        if (contract.CurrentStatus.Equals(ConstantStrings.ContractInWP))
                        {
                            // send notice to WP users
                            // TODO: change to selected users
                        }
                        if (contract.CurrentStatus.Equals(ConstantStrings.ContractInCFM))
                        {
                            // send notice to CFM users
                        }
                        if (contract.CurrentStatus.Equals(ConstantStrings.ContractComplete50) ||
                            contract.CurrentStatus.Equals(ConstantStrings.ContractComplete50) ||
                            contract.CurrentStatus.Equals(ConstantStrings.ContractComplete50))
                        {
                            // send notice to constatus@dot.state.fl.us
                        }
                    }
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
                catch (Exception e)
                {
                    _logger.LogError("ContractsController.Edit Error:" + e.GetBaseException());
                    Log.Error("ContractsController.Edit Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
                ViewBag.SuccessMessage = "Contract update saved.";
                return RedirectToAction("Details", new { id = contract.ContractID });
            }
            else
            {
                Log.Information("Model state not valid: ");
                var errors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                Log.Information(errors);

            }
            ViewData["ContractTypes"] = new SelectList(_context.ContractTypes, "ContractTypeID", "ContractTypeID", contract.ContractType);
            ViewData["Compensations"] = new SelectList(_context.Compensations, "CompensationID", "CompensationID", contract.CompensationID);
            ViewData["Procurements"] = new SelectList(_context.Procurements, "ProcurementID", "ProcurementID", contract.ProcurementID);
            ViewData["Recipients"] = new SelectList(_context.Recipients, "RecipientID", "RecipientID", contract.RecipientID);
            ViewData["Vendors"] = new SelectList(_context.Vendors, "VendorID", "VendorID", contract.VendorID);
            return View(contract);
        }

        [HttpPost]
        public string UpdateStatus(string contractStatus)
        {
            string response = "";
            try
            {
                ContractStatus newStatus = JsonConvert.DeserializeObject<ContractStatus>(contractStatus);
                newStatus.SubmittalDate = DateTime.Now;
                newStatus.CurrentStatus = ConstantStrings.LookupConstant(newStatus.CurrentStatus);
                _context.ContractStatuses.Add(newStatus);

                Contract contract = _context.Contracts.Where(c => c.ContractID == newStatus.ContractID).SingleOrDefault();
                contract.CurrentStatus = newStatus.CurrentStatus;
                _context.Contracts.Update(contract);
                _context.SaveChanges();
                //response = "{\"success\" : \"The Contract Status has been successfully updated.\"}";
                response = contract.CurrentStatus;
            }
            catch (Exception e)
            {
                Log.Error(e.StackTrace);
            }

            return response;
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
                List<Vendor> VendorList = _context.Vendors
                    .Where(v => (v.VendorCode.Contains(searchSTRING) || v.VendorName.ToUpper().Contains(searchSTRING)))
                    .OrderBy(v => v.VendorCode)
                    .ToList();
                return Json(VendorList);
            }
            return new JsonResult(searchString);
        }
        [HttpPost]
        public JsonResult ListContractTypes(string searchString)
        {
            var searchSTRING = searchString.ToUpper();
            if (!string.IsNullOrEmpty(searchString) && !searchSTRING.Equals("NEW"))
            {
                List<ContractType> ContractTypeList = _context.ContractTypes
                    .Where(ct => ct.ContractTypeCode.Contains(searchSTRING) || ct.ContractTypeName.ToUpper().Contains(searchSTRING))
                    .OrderBy(ct => ct.ContractTypeCode)
                    .ToList();

                return Json(ContractTypeList);
            }
            return new JsonResult(searchString);
        }

        private string GetLogin() {
            string userLogin = "";
            PermissionsUtils pu = new PermissionsUtils(_context);
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                userLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            }
            else
            {
                userLogin = HttpContext.User.Identity.Name;
            }
            return pu.GetLogin(userLogin);
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
                    Log.Error("ContractsController.PopulateViewBag Error:" + e.GetBaseException() +  "\n" + e.StackTrace);
                }
            }
            else
            {
                RedirectToAction("Home");
            }
        }

        public List<Contract> GetContracts(User user)
        {

            List<Contract> contracts = new List<Contract>();
            string rolesList = ViewBag.Roles;
            try
            {
                if (rolesList.Contains(ConstantStrings.AdminRole))
                {
                    contracts = GetActiveContracts();
                }
                else
                {
                    if (rolesList.Contains(ConstantStrings.Originator))
                    {
                        var originatorContracts = _context.Contracts
                            .Include(c => c.Vendor)
                            .Include(c => c.ContractType)
                            .Include(c => c.User)
                            .Where(c => c.UserID == user.UserID);
                        contracts.AddRange(originatorContracts);
                    }
                    if (rolesList.Contains(ConstantStrings.FinanceReviewer))
                    {
                        contracts.AddRange(_pu.GetContractsByStatus(ConstantStrings.SubmittedFinance));
                    }
                    if (rolesList.Contains(ConstantStrings.WPReviewer))
                    {
                        contracts.AddRange(_pu.GetContractsByStatus(ConstantStrings.SubmittedWP));
                    }
                    if (rolesList.Contains(ConstantStrings.CFMSubmitter))
                    {
                        contracts.AddRange(_pu.GetContractsByStatus(ConstantStrings.CFMReady));
                    }
                }
            }catch(Exception e)
            {
                throw e;
            }
            return contracts;
        }

        public List<Contract> GetActiveContracts()
        {
            try
            {
                // excludes archived contracts (c => c.CurrentStatus == ConstantStrings.ContractArchived)
                List<Contract> activeContracts = _context.Contracts
                    .Include(c => c.Vendor)
                    .Include(c => c.ContractType)
                    .Include(c => c.User)
                    .Where(c => c.CurrentStatus != ConstantStrings.ContractArchived)
                    .ToList();
                return activeContracts;
            }
            catch (Exception e)
            {
                Log.Error("ContractsController.GetActiveContracts Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                return null;
            }
        }
        public List<Contract> GetArchivedContracts()
        {
            try
            {
                // returns only archived contracts (c => c.CurrentStatus == ConstantStrings.Archived)
                List<Contract> archiveContracts = _context.Contracts
                    .Include(c => c.Vendor)
                    .Include(c => c.ContractType)
                    .Include(c => c.User)
                    .Where(c => c.CurrentStatus == ConstantStrings.ContractArchived)
                    .ToList();
                return archiveContracts;
            }
            catch (Exception e)
            {
                Log.Error("ContractsController.GetArchivedContracts Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                return null;
            }
        }

        private List<User> GetWpReviewersList()
        {
            return _pu.GetUsersByRole(ConstantStrings.WPReviewer);
        }

        [HttpPost]
        public JsonResult GetHistory(string contractInfo)
        {
            Dictionary<string, int> info = JsonConvert.DeserializeObject<Dictionary<string, int>>(contractInfo);
            int contractID = info["ID"];
            List<ContractStatus> statuses = _context.ContractStatuses
                .Where(c => c.ContractID == contractID)
                .Include(c => c.User)
                .AsNoTracking()
                .OrderByDescending(c => c.SubmittalDate)
                .ToList();
            return Json(statuses);
        }

        public List<SelectListItem> GetContractStatusList(Contract contract, string roles)
        {
            List<SelectListItem> typeList = new List<SelectListItem>();
            if (roles.Contains(ConstantStrings.Originator)) {
                switch (contract.CurrentStatus) {
                    case ConstantStrings.ContractNew:
                    case ConstantStrings.ContractDrafted:
                        typeList.Add(new SelectListItem { Value = "ContractDrafted", Text = "Save Contract as Draft" });
                        typeList.Add(new SelectListItem { Value = "ContractInFinance", Text = "Submit Contract to Finance" });
                        break;
                    case ConstantStrings.ContractInCFM:
                        typeList.Add(new SelectListItem { Value = "ContractRequest50", Text = "Request contract complete, status 50" });
                        typeList.Add(new SelectListItem { Value = "ContractRequest52", Text = "Request contract complete, status 52" });
                        typeList.Add(new SelectListItem { Value = "ContractRequest98", Text = "Request contract complete, status 98" });
                        break;
                } // end switch
            } // end if Originator
            if (roles.Contains(ConstantStrings.FinanceReviewer))
            {
                switch (contract.CurrentStatus)
                {
                    case ConstantStrings.ContractInFinance:
                        typeList.Add(new SelectListItem { Value = "ContractDrafted", Text = "Return to Draft" });
                        typeList.Add(new SelectListItem { Value = "ContractInFinance", Text = "Keep in Work Finance" });
                        typeList.Add(new SelectListItem { Value = "ContractInWP", Text = "Submit to Work Program" });
                        typeList.Add(new SelectListItem { Value = "ContractInCFM", Text = "Ready for CFM" });
                        break;
                    case ConstantStrings.ContractInCFM:
                        typeList.Add(new SelectListItem { Value = "ContractComplete50", Text = "Contract is complete with status 50" });
                        typeList.Add(new SelectListItem { Value = "ContractComplete52", Text = "Contract is complete with status 52" });
                        typeList.Add(new SelectListItem { Value = "ContractComplete98", Text = "Contract is complete with status 98" });
                        typeList.Add(new SelectListItem { Value = "ContractArchived", Text = "Contract has been archived" });
                        break;
                } // end switch
            } // end if FinanceReviewer
            if (roles.Contains(ConstantStrings.WPReviewer))
            {
                switch (contract.CurrentStatus)
                {
                    case ConstantStrings.ContractInWP:
                        typeList.Add(new SelectListItem { Value = "ContractInFinance", Text = "Return to Finance" });
                        typeList.Add(new SelectListItem { Value = "ContractInWP", Text = "Keep in Work Program" });
                        typeList.Add(new SelectListItem { Value = "ContractInCFM", Text = "Ready for CFM" });
                        break;
                } // end switch
            } // end if WPReviewer
            if (roles.Contains(ConstantStrings.CFMSubmitter))
            {
                switch (contract.CurrentStatus)
                {
                    case ConstantStrings.ContractInCFM:
                        typeList.Add(new SelectListItem { Value = "ContractDrafted", Text = "Return to Draft" });
                        typeList.Add(new SelectListItem { Value = "ContractInWP", Text = "Return to Work Program" });
                        typeList.Add(new SelectListItem { Value = "ContractComplete50", Text = "Contract is complete with status 50" });
                        typeList.Add(new SelectListItem { Value = "ContractComplete52", Text = "Contract is complete with status 52" });
                        typeList.Add(new SelectListItem { Value = "ContractComplete98", Text = "Contract is complete with status 98" });
                        typeList.Add(new SelectListItem { Value = "ContractArchived", Text = "Contract has been archived" }); break;
                } // end switch
            } // end if CFMSubmitter
            return typeList;
        }

        public ExtendedContract GetExtendedContract(int contractID)
        {
            Contract returnContract = _pu.GetDeepContract(contractID);
            if(returnContract == null) { return null; }
            return new ExtendedContract(returnContract);
        }

    } // end ContractsController class
} // end namespace EPS3.Controllers
