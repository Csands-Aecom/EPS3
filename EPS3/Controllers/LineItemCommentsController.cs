using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPS3.DataContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EPS3.Models;

namespace EPS3.Controllers
{
    public class LineItemCommentsController : Controller
    {
        private readonly EPSContext _context;

        public LineItemCommentsController(EPSContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Create([Bind("LineItemID,UserID,SubmittalDate,Comments")] LineItemComment comment, string source)
        {
            if (ModelState.IsValid)
            {
                comment.SubmittalDate = DateTime.Now;
                _context.Add(comment);
                await _context.SaveChangesAsync();
                return RedirectToAction(source, "LineItems", new { id = comment.LineItemID });
            }
            ViewData["LineItemID"] = new SelectList(_context.LineItems, "LineItemID", "LineItemID", comment.LineItemID);
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserID", comment.UserID);
            return View(comment);
        }

        // GET: LineItemStatus
        public async Task<IActionResult> Index()
        {
            var ePSContext = _context.LineItemComments.Include(l => l.LineItem).Include(l => l.User);
            return View(await ePSContext.ToListAsync());
        }
    }
}
