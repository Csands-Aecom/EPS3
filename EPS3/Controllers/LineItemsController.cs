using System;
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
using Newtonsoft.Json;

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
            _pu = new PermissionsUtils(_context, _logger);
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
            //string userLogin = GetLogin();
            PopulateViewBag(contractID);
            if (groupID > 0)
            {
                try
                {
                    // id is the LineItemGroup.GroupID 
                    var group = _context.LineItemGroups.Where(lig => lig.GroupID == groupID).SingleOrDefault();
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

                    // also the View does not use this to populate a selection list yet.
                    //ViewData["FlairLineIDs"] = GetAmendmentsList(contractID);
                }
                catch (Exception e)
                {

                    _logger.LogError("LineItemsController.Create Error:" + e.GetBaseException());
                    Log.Error("LineItemsController.Create Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
            }
            ViewBag.LineItemTypes = ConstantStrings.GetLineItemTypeList();
            ViewBag.currentFiscalYear = PermissionsUtils.GetCurrentFiscalYear();
            ViewData["Categories"] = _context.Categories.OrderBy(v => v.CategoryCode);
            ViewData["StatePrograms"] = _context.StatePrograms.OrderBy(v => v.ProgramCode);
            return View();
        }


        [HttpPost]
        public JsonResult AddNewLineItem(LineItem newLineItem)
        {
            try {
                _logger.LogDebug("New line item for Contract " + newLineItem.ContractID);
                // set linenumber correctly
                if (newLineItem.LineNumber == 0)
                {
                    int nextLineNumber = _context.LineItems.Where(g => g.LineItemGroupID == newLineItem.LineItemGroupID).Select(g => g.LineNumber).DefaultIfEmpty(0).Max();
                    newLineItem.LineNumber = nextLineNumber + 1;
                }
                if (newLineItem.OrgCode.Length > 9 && newLineItem.OrgCode.Contains("55-"))
                {
                    newLineItem.OrgCode = newLineItem.OrgCode.Replace("55-", "");
                }
                // Uppercase some fields
                newLineItem.FinancialProjectNumber = newLineItem.FinancialProjectNumber.ToUpper();
                newLineItem.ExpansionObject = newLineItem.ExpansionObject.ToUpper();

                //Update LineItemGroup if LineItem has FlairAmendmentID and LineID6S but LineItemGroup does not
                //TODO: new method 

                if (newLineItem.LineItemID > 0)
                {
                    //LineItem already exists. This is an update.
                    _context.LineItems.Update(newLineItem);
                }
                else
                {
                    // set lineNumber
                    int numberOfLineItems = _context.LineItems.Where(l => l.LineItemGroupID == newLineItem.LineItemGroupID).Count();
                    //New Line Item. Add it.
                    _context.LineItems.Add(newLineItem);
                }
                _context.SaveChanges();
            } catch (Exception e) {
                _logger.LogError("LineItemsController.AddNewLineItem Error:" + e.GetBaseException());
            }
            if (newLineItem.LineItemType.Equals(ConstantStrings.NewContract) || newLineItem.LineItemType.Equals(ConstantStrings.Award) || newLineItem.LineItemType.Equals(ConstantStrings.Advertisement))
            {
                UpdateContractTotal(newLineItem);
            }
            ExtendedLineItem wrapperLineItem = GetExtendedLineItem(newLineItem.LineItemID);
            string result = JsonConvert.SerializeObject(wrapperLineItem);
            return Json(result);
        }


        [HttpPost]
        public JsonResult DeleteLineItem(int LineItemID)
        {
            try
            {
                var lineItem = _context.LineItems.SingleOrDefault(m => m.LineItemID == LineItemID);
                _context.LineItems.Remove(lineItem);
                _context.SaveChanges();
            }catch(Exception e)
            {
                _logger.LogError("LineItemsController.DeleteLineItem Error:" + e.GetBaseException());
                Log.Error("LineItemsController.DeleteLineItem  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                return Json("{\"fail\" : \"Delete failed.\"}");
            }
            return Json("{\"success\": \"Line " + LineItemID.ToString() + " successfully deleted.\"}");
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
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchSTRING = searchString.ToUpper();
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
        if (!string.IsNullOrEmpty(searchString))
        {
            var searchSTRING = searchString.ToUpper();
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
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchSTRING = searchString.ToUpper();
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
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchSTRING = searchString.ToUpper();
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
            return "TODO"; // _pu.GetLogin();
            //string userLogin = "";
            //if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            //{
            //    userLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            //}
            //else
            //{
            //    userLogin = HttpContext.User.Identity.Name;
            //}
            //return _pu.GetLogin(userLogin);
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
                    Contract contract = _context.GetContractByID(contractID);
                    ViewBag.Contract = contract;
                    ViewBag.CurrentUser = currentUser;
                    ViewBag.Roles = roles;
                }
                catch (Exception e)
                {
                    _logger.LogError("LineItemsController.PopulateViewBag Error:" + e.GetBaseException());
                    Log.Error("LineItemsController.PopulateViewBag Error:" + e.GetBaseException() + "\n" + e.StackTrace);
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

        private void UpdateContractTotal(LineItem lineItem)
        {
            // A line item has been added with LineItemType = {Award, Advertisement, New Contract}
            // Reset the parent Contract's ContractTotal value to the total of the new encumbrance
            LineItemGroup encumbrance = _context.LineItemGroups
                .Include(g => g.LineItems)
                .AsNoTracking()
                .SingleOrDefault(g => g.GroupID == lineItem.LineItemGroupID);
            //verify the encumbrance line item type is one of the three selected
            if (encumbrance.LineItemType.Equals(ConstantStrings.NewContract) || encumbrance.LineItemType.Equals(ConstantStrings.Award) || encumbrance.LineItemType.Equals(ConstantStrings.Advertisement))
            {
                // get the total of all LineItems in this encumbrance
                decimal sum = 0.00M;
                foreach(LineItem item in encumbrance.LineItems)
                {
                    sum += item.Amount;
                }
                Contract contract = _context.Contracts.SingleOrDefault(c => c.ContractID == encumbrance.ContractID);
                contract.ContractTotal = sum;
                _context.Contracts.Update(contract);
                _context.SaveChanges();
            }
        }

        public ExtendedLineItem GetExtendedLineItem(int lineItemID)
        {
            LineItem returnLineItem = _context.GetDeepLineItem(lineItemID);
            if (returnLineItem == null) { return null; }
            return new ExtendedLineItem(returnLineItem);
        }
    } // end class

}
