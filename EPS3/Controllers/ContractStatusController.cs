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
    public class ContractStatusController : Controller
    {
        private readonly EPSContext _context;

        public ContractStatusController(EPSContext context)
        {
            _context = context;
        }

        // GET: ContractStatus
        public async Task<IActionResult> Index()
        {
            var ePSContext = _context.ContractStatuses.Include(c => c.Contract).Include(c => c.User);
            return View(await ePSContext.ToListAsync());
        }

        // GET: ContractStatus/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contractStatus = await _context.ContractStatuses
                .Include(c => c.Contract)
                .Include(c => c.User)
                .SingleOrDefaultAsync(m => m.StatusID == id);
            if (contractStatus == null)
            {
                return NotFound();
            }

            return View(contractStatus);
        }

        // GET: ContractStatus/Create
        public IActionResult Create(int? id)
        {
            Contract contract =  (Contract) _context.Contracts
                .AsNoTracking()
                .SingleOrDefault(c => c.ContractID== id);
            string CurrentUser = HttpContext.User.Identity.Name;
            if (CurrentUser == null) { CurrentUser = "KNAEC01"; }
            User user = (User)_context.Users
                .AsNoTracking()
                .SingleOrDefault(u => u.UserLogin == CurrentUser);
            List<UserRole> rolesList = _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserID == user.UserID)
                .ToList<UserRole>();
            string roles = "";
            for (int i = 0; i < rolesList.Count; i++)
            {
                roles += rolesList[i].Role;
            }
            ViewBag.Contract = contract;
            ViewBag.CurrentUser = user;
            ViewBag.Roles = roles;
            return View();
        }

        // POST: ContractStatus/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContractID,StatusID,UserID,CurrentStatus,SubmittalDate,Comments")] ContractStatus contractStatus)
        {
            if (ModelState.IsValid)
            {
                contractStatus.SubmittalDate = DateTime.Now;
                _context.Add(contractStatus);
                await _context.SaveChangesAsync();
                return RedirectToAction("Edit", "Contracts", new { id = contractStatus.ContractID });
            }
            return View();
        }

        // GET: ContractStatus/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contractStatus = await _context.ContractStatuses.SingleOrDefaultAsync(m => m.StatusID == id);
            if (contractStatus == null)
            {
                return NotFound();
            }
            ViewData["ContractID"] = new SelectList(_context.Contracts, "ContractID", "ContractID", contractStatus.ContractID);
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserID", contractStatus.UserID);
            return View(contractStatus);
        }

        // POST: ContractStatus/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContractID,StatusID,UserID,CurrentStatus,SubmittalDate,Comments")] ContractStatus contractStatus)
        {
            if (id != contractStatus.StatusID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contractStatus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContractStatusExists(contractStatus.StatusID))
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
            ViewData["ContractID"] = new SelectList(_context.Contracts, "ContractID", "ContractID", contractStatus.ContractID);
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserID", contractStatus.UserID);
            return View(contractStatus);
        }

        // GET: ContractStatus/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contractStatus = await _context.ContractStatuses
                .Include(c => c.Contract)
                .Include(c => c.User)
                .SingleOrDefaultAsync(m => m.StatusID == id);
            if (contractStatus == null)
            {
                return NotFound();
            }

            return View(contractStatus);
        }

        // POST: ContractStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contractStatus = await _context.ContractStatuses.SingleOrDefaultAsync(m => m.StatusID == id);
            _context.ContractStatuses.Remove(contractStatus);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContractStatusExists(int id)
        {
            return _context.ContractStatuses.Any(e => e.StatusID == id);
        }
    }
}
