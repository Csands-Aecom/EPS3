using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EPS3.DataContexts;
using EPS3.Models;
using EPS3.ViewModels;

namespace EPS3.Controllers
{
    public class LineItemStatusController : Controller
    {
        private readonly EPSContext _context;

        public LineItemStatusController(EPSContext context)
        {
            _context = context;
        }

        // GET: LineItemStatus
        public async Task<IActionResult> Index()
        {
            var ePSContext = _context.LineItemStatuses.Include(l => l.LineItem).Include(l => l.User);
            return View(await ePSContext.ToListAsync());
        }

        // GET: LineItemStatus/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lineItemStatus = await _context.LineItemStatuses
                .Include(l => l.LineItem)
                .Include(l => l.User)
                .SingleOrDefaultAsync(m => m.StatusID == id);
            if (lineItemStatus == null)
            {
                return NotFound();
            }

            return View(lineItemStatus);
        }

        // GET: LineItemStatus/Create
        public IActionResult Create()
        {
            ViewData["LineItemID"] = new SelectList(_context.LineItems, "LineItemID", "LineItemID");
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserID");
            return View();
        }

        // POST: LineItemStatus/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LineItemID,Amount,StatusID,UserID,CurrentStatus,SubmittalDate,Comments")] LineItemStatus lineItemStatus)
        {
            if (ModelState.IsValid)
            {
                _context.Add(lineItemStatus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LineItemID"] = new SelectList(_context.LineItems, "LineItemID", "LineItemID", lineItemStatus.LineItemID);
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserID", lineItemStatus.UserID);
            return View(lineItemStatus);
        }

        // GET: LineItemStatus/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lineItemStatus = await _context.LineItemStatuses.SingleOrDefaultAsync(m => m.StatusID == id);
            if (lineItemStatus == null)
            {
                return NotFound();
            }
            ViewData["LineItemID"] = new SelectList(_context.LineItems, "LineItemID", "LineItemID", lineItemStatus.LineItemID);
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserID", lineItemStatus.UserID);
            return View(lineItemStatus);
        }

        // POST: LineItemStatus/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LineItemID,Amount,StatusID,UserID,CurrentStatus,SubmittalDate,Comments")] LineItemStatus lineItemStatus)
        {
            if (id != lineItemStatus.StatusID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lineItemStatus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LineItemStatusExists(lineItemStatus.StatusID))
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
            ViewData["LineItemID"] = new SelectList(_context.LineItems, "LineItemID", "LineItemID", lineItemStatus.LineItemID);
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserID", lineItemStatus.UserID);
            return View(lineItemStatus);
        }

        // GET: LineItemStatus/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lineItemStatus = await _context.LineItemStatuses
                .Include(l => l.LineItem)
                .Include(l => l.User)
                .SingleOrDefaultAsync(m => m.StatusID == id);
            if (lineItemStatus == null)
            {
                return NotFound();
            }

            return View(lineItemStatus);
        }

        // POST: LineItemStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lineItemStatus = await _context.LineItemStatuses.SingleOrDefaultAsync(m => m.StatusID == id);
            _context.LineItemStatuses.Remove(lineItemStatus);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LineItemStatusExists(int id)
        {
            return _context.LineItemStatuses.Any(e => e.StatusID == id);
        }
    }
}
