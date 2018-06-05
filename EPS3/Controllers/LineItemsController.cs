using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EPS3.DataContexts;
using EPS3.Models;

namespace EPS3.Controllers
{
    public class LineItemsController : Controller
    {
        private readonly EPSContext _context;

        public LineItemsController(EPSContext context)
        {
            _context = context;
        }

        // GET: LineItems
        public async Task<IActionResult> Index()
        {
            var lineItems = _context.LineItems
                .Include(l => l.OCA)
                .Include(l => l.Fund)
                .Include(l => l.Category)
                .Include(l => l.StateProgram);
            return View(await lineItems.ToListAsync());
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
        public IActionResult Create(int? id)
        {
            ViewBag.contractID = id;
            ViewBag.currentFiscalYear = CurrentFiscalYear();
            ViewData["Categories"] = _context.Categories.OrderBy(v => v.CategoryCode);
            ViewData["StatePrograms"] = _context.StatePrograms.OrderBy(v => v.ProgramCode);
            return View();
        }

        // POST: LineItems/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LineItemID,ContractID,OrgCode,ExpansionObject,FlairObject,FinancialProjectNumber,FundID,Amount,StateProgramID,OCAID,WorkActivity,CategoryID,FiscalYear")] LineItem lineItem)
        {
            // remove "55-" prefix from OrgCode value before saving
            lineItem.OrgCode = CleanOrgCode(lineItem.OrgCode);
            if (ModelState.IsValid)
            {
                _context.Add(lineItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lineItem);
        }

        // GET: LineItems/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lineItem = await _context.LineItems.SingleOrDefaultAsync(m => m.LineItemID == id);
            var contract = await _context.Contracts.SingleOrDefaultAsync(c => c.ContractID == lineItem.ContractID);
            if (lineItem == null)
            {
                return NotFound();
            }
            ViewData["Categories"] = _context.Categories.OrderBy(v => v.CategoryCode);
            ViewData["StatePrograms"] = _context.StatePrograms.OrderBy(v => v.ProgramCode);
            ViewBag.myOCA = _context.OCAs.SingleOrDefault(c => c.OCAID == lineItem.OCAID);
            ViewBag.myFund = _context.Funds.SingleOrDefault(f => f.FundID== lineItem.FundID);
            ViewBag.myCategory = _context.Categories.SingleOrDefault(c => c.CategoryID == lineItem.CategoryID);
            ViewBag.myStateProgram = _context.StatePrograms.SingleOrDefault(p => p.ProgramID == lineItem.StateProgramID);
            ViewBag.LineItemID = lineItem.LineItemID;
            ViewBag.ContractID = contract.ContractID;
            return View(lineItem);
        }

        // POST: LineItems/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LineItemID, ContractID, OrgCode, FinancialProjectNumber, StateProgramID, CategoryID, WorkActivity, OCAID, ExpansionObject, FlairObject, FundID, FiscalYear, Amount")] LineItem lineItem)
        {
            if (id != lineItem.LineItemID)
            {
                return NotFound();
            }
            // remove "55-" prefix from OrgCode value before saving
            lineItem.OrgCode = CleanOrgCode(lineItem.OrgCode);
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lineItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LineItemExists(lineItem.LineItemID))
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
            _context.LineItems.Remove(lineItem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
                var EOList = (from N in _context.LineItems.ToList()
                              where N.ExpansionObject.ToUpper().StartsWith(searchSTRING)
                              orderby N.ExpansionObject
                              select new { N.ExpansionObject }).Distinct();
                return Json(EOList);
            }
            return new JsonResult(searchString);
        }
        [HttpPost]
        public JsonResult ListWorkActivities(string searchString)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                var WorkActivityList = (from N in _context.LineItems.ToList()
                              where N.WorkActivity.StartsWith(searchString)
                              orderby N.WorkActivity
                              select new { N.WorkActivity }).Distinct();
                return Json(WorkActivityList);
            }
            return new JsonResult(searchString);
        }
        
        [HttpPost]
        public JsonResult ListFlairObj(string searchString)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                var FlairObjList = (from N in _context.LineItems.ToList()
                              where N.FlairObject.StartsWith(searchString)
                              orderby N.FlairObject
                              select new { N.FlairObject }).Distinct();
                return Json(FlairObjList);
            }
            return new JsonResult(searchString);
        }

        [HttpPost]
        public JsonResult ListFinProjNums(string searchString)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                var FinProjList = (from N in _context.LineItems.ToList()
                                    where N.FinancialProjectNumber.StartsWith(searchString)
                                    orderby N.FinancialProjectNumber
                                    select new { N.FinancialProjectNumber }).Distinct();
                return Json(FinProjList);
            }
            return new JsonResult(searchString);
        }

        [HttpPost]
        public JsonResult GetFundName(string fundId)
        {
            if(!string.IsNullOrEmpty(fundId))
            {
                var fundInfo = (from N in _context.Funds.ToList()
                                where N.FundID.Equals(fundId)
                                select new { N.FundCode, N.FundDescription });
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
            var OCAList = (from N in _context.OCAs.ToList()
                            where N.OCACode.Contains(searchSTRING) ||
                            N.OCAName.ToUpper().Contains(searchSTRING)
                            orderby N.OCACode
                            select new { N.OCAID, N.OCACode, N.OCAName });

            return Json(OCAList);
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
                var FundsList = (from N in _context.Funds.ToList()
                                      where N.FundCode.Contains(searchSTRING) ||
                                      N.FundDescription.ToUpper().Contains(searchSTRING)
                                      orderby N.FundCode
                                      select new { N.FundID, N.FundCode, N.FundDescription });

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
    } // end class

}
