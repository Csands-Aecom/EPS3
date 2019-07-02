using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EPS3.Models;
using EPS3.DataContexts;
using Microsoft.Extensions.Logging;
using EPS3.Helpers;
using Newtonsoft.Json;
using System.Data.Entity;
using Serilog;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EPS3.Controllers
{
    public class LineItemGroupStatusesController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<LineItemGroupStatusesController> _logger;
        private PermissionsUtils _pu;

        public LineItemGroupStatusesController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<LineItemGroupStatusesController>();
            _pu = new PermissionsUtils(_context, _logger);
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add (string lineItemGroupStatus)
        {
            LineItemGroupStatus newStatus = null;
            try
            {
                newStatus = JsonConvert.DeserializeObject<LineItemGroupStatus>(lineItemGroupStatus);
                newStatus.SubmittalDate = DateTime.Now;
                _context.LineItemGroupStatuses.Add(newStatus);
                _context.SaveChanges();

                int id = newStatus.StatusID;
                newStatus = _context.LineItemGroupStatuses
                    .AsNoTracking()
                    .SingleOrDefault(s => s.StatusID == id);
                // Not sure why .Include(s => s.User) leaves newStatus.User as null
                // This extra query is the workaround.
                User user = _context.Users
                    .AsNoTracking()
                    .SingleOrDefault(u => u.UserID == newStatus.UserID);
                newStatus.User = user;
            }
            catch(Exception e)
            {
                _logger.LogError("LineItemGroupStatusesController.Add Error:" + e.GetBaseException());
                Log.Error("LineItemGroupStatusesController.Add Error:" + e.GetBaseException() + "\n" + e.StackTrace);
            }
            return Json(newStatus);
        }
    }
}
