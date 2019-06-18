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
using Microsoft.Extensions.Logging;

namespace EPS3.Controllers
{
    public class FundsController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<FundsController> _logger;
        private PermissionsUtils _pu;

        public FundsController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<FundsController>();
            _pu = new PermissionsUtils(_context, _logger);
        }

        // GET: Funds
        public async Task<IActionResult> Index()
        {
            ViewBag.Roles = _pu.GetUserRoles(GetLogin());
            return View(await _context.Funds.ToListAsync());
        }

        // GET: Funds/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fund = await _context.Funds
                .FirstOrDefaultAsync(m => m.FundID == id);
            if (fund == null)
            {
                return NotFound();
            }

            return View(fund);
        }

        // GET: Funds/Create
        public IActionResult Create()
        {
            if (!UserIsAdmin()) { return RedirectToAction("Index", "Funds"); }
            return View();
        }

        // POST: Funds/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FundID,FundCode,FundDescription")] Fund fund)
        {
            if (ModelState.IsValid)
            {
                _context.Add(fund);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fund);
        }

        // GET: Funds/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (!UserIsAdmin()) { return RedirectToAction("Index", "Funds"); }
            var fund = await _context.Funds.FindAsync(id);
            if (fund == null)
            {
                return NotFound();
            }
            return View(fund);
        }

        // POST: Funds/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FundID,FundCode,FundDescription")] Fund fund)
        {
            if (id != fund.FundID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fund);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FundExists(fund.FundID))
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
            return View(fund);
        }

        // GET: Funds/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (!UserIsAdmin()) { return RedirectToAction("Index", "Funds"); }
            var fund = await _context.Funds
                .FirstOrDefaultAsync(m => m.FundID == id);
            if (fund == null)
            {
                return NotFound();
            }

            return View(fund);
        }

        // POST: Funds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fund = await _context.Funds.FindAsync(id);
            _context.Funds.Remove(fund);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FundExists(int id)
        {
            return _context.Funds.Any(e => e.FundID == id);
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

        private bool UserIsAdmin() 
        {
            return _pu.GetUserRoles(GetLogin()).Contains(ConstantStrings.AdminRole);
        }
    }
}
