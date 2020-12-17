using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EPS3.DataContexts;
using EPS3.Models;
using EPS3.Helpers;
using EPS3.ViewModels;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Serilog;

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
            _pu = new PermissionsUtils(_context, _logger);
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
                erViewModel.Contract = _pu.GetDeepContract(id);
                erViewModel.LineItemGroups = _pu.GetDeepEncumbrances(id);
                // totals for each encumbrance request in the contract
                Dictionary<int, string> groupAmounts = new Dictionary<int, string>();
                Boolean canBeClosed = !(erViewModel.Contract.CurrentStatus.Contains("Closed"));
                foreach (LineItemGroup encumbrance in erViewModel.LineItemGroups)
                {
                    decimal groupAmount = 0.0m;
                    foreach(LineItem lineItem in encumbrance.LineItems)
                    {
                        groupAmount += lineItem.Amount;
                    }
                    groupAmounts.Add(encumbrance.GroupID, Utils.FormatCurrency(groupAmount));
                    // check if all line item groups can be closed -- must be CFM complete or closed
                    if (!encumbrance.CurrentStatus.Contains(ConstantStrings.CFMComplete) && !encumbrance.CurrentStatus.Contains("Closed"))
                    {
                        canBeClosed = false;
                    }
                }
                ViewBag.CanClose = canBeClosed;
                ViewBag.GroupAmounts = groupAmounts;
                ViewBag.ContractAmount = Utils.FormatCurrency(_pu.GetTotalAmountOfAllEncumbrances(id));
            }
            catch (Exception e)
            {
                _logger.LogError("ContractsController.Edit Error:" + e.GetBaseException()); //todo once we fix the notification bug, change this to "ContractsController.Details"
                Log.Error("ContractsController.Edit Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
            return View(erViewModel);
        }



        // GET: Contracts/Create
        public IActionResult Create()
        {
            string userLogin = GetLogin();
            PopulateViewBag(0);
            // Redirect non-registered users to List page
            if (ViewBag.CurrentUser == null) { return RedirectToAction("List", "LineItemGroups"); }
            // dropdown list values
            ViewData["Procurements"] = _context.Procurements.OrderBy(p => p.ProcurementCode);
            ViewData["Compensations"] = _context.Compensations.OrderBy(c => c.CompensationID);
            ViewData["Recipients"] = _context.Recipients.OrderBy(v => v.RecipientCode);
            return View();
        }

        // POST: Contracts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserID,ContractID,ContractNumber,ContractTypeID,GovernorDeclareEmergencyNumber,RecipientID,ProcurementID,CompensationID,IsRenewable,ContractTotal,MaxLoaAmount,BudgetCeiling,VendorID,BeginningDate,EndingDate,ServiceEndingDate,DescriptionOfWork,CurrentStatus")] Contract contract)
        {
            int AD_VENDOR = 1; //If no Vendor is specified, default to AD with id value = 1
            if (contract.VendorID == 0)
            {
                contract.VendorID = AD_VENDOR; // placeholder value Advertising
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
                        contract.ContractNumber = "NEW"; // placeholder text for pending generation of ContractNumber
                    }
                    contract.ContractNumber = contract.ContractNumber.ToUpper();
                    _context.Contracts.Add(contract);
                    _context.SaveChanges();
                    //get current user
                    User currentUser = await _context.Users
                        .Include(u => u.Roles)
                        .SingleOrDefaultAsync(u => u.UserID == contract.UserID);

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

        // Main function for adding new contract from /LineItemGroups/Manage
        // JSON string sent by AJAX method saveContractModal() in site.js
        // Data comes from NewContractPartial view

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
            }
            
            if (contractVM == null)
            {
                return NotFound();
            }
            ViewBag.ContractAmount = _pu.GetTotalAmountOfAllEncumbrances(contractVM.Contract.ContractID);

            ViewData["ContractTypes"] = _context.ContractTypes.OrderBy(c => c.ContractTypeCode);
            ViewData["Procurements"] = _context.Procurements.OrderBy(p => p.ProcurementCode);
            ViewData["Compensations"] = _context.Compensations.OrderBy(c => c.CompensationID);
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
        public IActionResult Edit(int id, [Bind("ContractID,ContractNumber,ContractTypeID,GovernorDeclareEmergencyNumber,ProcurementID,CompensationID,IsRenewable,ContractTotal,MaxLoaAmount,BudgetCeiling,VendorID,RecipientID,BeginningDate,EndingDate,ServiceEndingDate,DescriptionOfWork,UserID,CurrentStatus")] Contract contract, string Comments, int CurrentUserID)
        {
            if (id != contract.ContractID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // update the contract
                    Contract oldContract = _context.Contracts.SingleOrDefault(c => c.ContractID == contract.ContractID);
                    // contractID and contractNumber do not change
                    oldContract.BeginningDate = contract.BeginningDate;
                    oldContract.BudgetCeiling = contract.BudgetCeiling;
                    oldContract.CompensationID = contract.CompensationID;
                    oldContract.ContractFunding = contract.ContractFunding;
                    oldContract.ContractTotal = contract.ContractTotal;
                    oldContract.ContractTypeID = contract.ContractTypeID;
                    oldContract.CurrentStatus = contract.CurrentStatus;
                    oldContract.DescriptionOfWork = contract.DescriptionOfWork;
                    oldContract.EndingDate = contract.EndingDate;
                    oldContract.IsRenewable = contract.IsRenewable;
                    oldContract.MaxLoaAmount = contract.MaxLoaAmount;
                    oldContract.ProcurementID = contract.ProcurementID;
                    oldContract.ModifiedDate = DateTime.Now.Date;
                    oldContract.RecipientID = contract.RecipientID;
                    oldContract.ServiceEndingDate = contract.ServiceEndingDate;
                    oldContract.UserID = contract.UserID;
                    oldContract.VendorID = contract.VendorID;

                    _context.Update(oldContract);
                    _context.SaveChanges();
                    return RedirectToAction("Details", new { id = oldContract.ContractID });

                }
                catch (Exception e)
                {
                    _logger.LogError("ContractsController.Edit Error:" + e.GetBaseException());
                    Log.Error("ContractsController.Edit Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
                ViewBag.SuccessMessage = "Contract update saved.";
                return  View(contract.ContractID);
            }
            
            ViewData["ContractTypes"] = new SelectList(_context.ContractTypes, "ContractTypeID", "ContractTypeID", contract.ContractType);
            ViewData["Compensations"] = new SelectList(_context.Compensations, "CompensationID", "CompensationID", contract.CompensationID);
            ViewData["Procurements"] = new SelectList(_context.Procurements, "ProcurementID", "ProcurementID", contract.ProcurementID);
            ViewData["Recipients"] = new SelectList(_context.Recipients, "RecipientID", "RecipientID", contract.RecipientID);
            return View(contract);
        }

        // autocomplete method for Vendor
        [HttpPost]
        public JsonResult ListVendors(string searchString)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchSTRING = searchString.ToUpper();
                List<Vendor> VendorList = _context.Vendors
                    .Where(v => (v.VendorCode.Contains(searchSTRING) || v.VendorName.ToUpper().Contains(searchSTRING)))
                    .OrderBy(v => v.VendorCode)
                    .ToList();
                return Json(VendorList);
            }
            return new JsonResult(searchString);
        }

        // autocomplete method for ContractType
        [HttpPost]
        public JsonResult ListContractTypes(string searchString)
        {
            HashSet<ContractType> contractTypes = new HashSet<ContractType>();

            if (!string.IsNullOrEmpty(searchString))
            {

                //code equals first
                foreach (var contractType in _context.ContractTypes
                    .Where(ct => ct.ContractTypeCode.Equals(searchString))
                    .OrderBy(ct => ct.ContractTypeCode))
                {
                    contractTypes.Add(contractType);
                }

                //code starts-with second; note that because we're pushing to a set duplicates are excluded
                foreach (var contractType in _context.ContractTypes
                    .Where(ct => ct.ContractTypeCode.StartsWith(searchString))
                    .OrderBy(ct => ct.ContractTypeCode))
                {
                    contractTypes.Add(contractType);
                }

                //name starts-with next
                foreach (var contractType in _context.ContractTypes
                    .Where(ct => ct.ContractTypeName.StartsWith(searchString))
                    .OrderBy(ct => ct.ContractTypeName))
                {
                    contractTypes.Add(contractType);
                }

                //name contains next
                foreach (var contractType in _context.ContractTypes
                  .Where(ct => ct.ContractTypeName.Contains(searchString))
                  .OrderBy(ct => ct.ContractTypeName))
                {
                    contractTypes.Add(contractType);
                }
                //code contains last
                foreach (var contractType in _context.ContractTypes
                    .Where(ct => ct.ContractTypeCode.Contains(searchString))
                    .OrderBy(ct => ct.ContractTypeCode))
                {
                    contractTypes.Add(contractType);
                }

                //List<ContractType> ContractTypeList = _context.ContractTypes
                //    .Where(ct => ct.ContractTypeCode.Contains(searchSTRING) || ct.ContractTypeName.ToUpper().Contains(searchSTRING))
                //    .OrderBy(ct => ct.ContractTypeCode)
                //    .ToList();
                

            }
            return Json(contractTypes);
        }

        private string GetLogin() {
            string userLogin = "";
            PermissionsUtils pu = new PermissionsUtils(_context, _logger);
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

        public ExtendedContract GetExtendedContract(int contractID)
        {
            // returns ExtendedContract object 
            Contract returnContract = _pu.GetDeepContract(contractID);
            if (returnContract == null) { return null; }
            return new ExtendedContract(returnContract);
        }

        private bool ContractExists(int id)
        {
            return _context.Contracts.Single(c => c.ContractID == id) != null;
        }


        // Test RESTful contract method
        [HttpGet]
        public JsonResult GetContractAPI(int id)
        {
            return (Json(GetExtendedContract(id)));
        }

        // unused methods
        // these were developed for a prior iteration
        // may be used in future reporting tools
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

    } // end ContractsController class
} // end namespace EPS3.Controllers
