using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPS3.DataContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EPS3.Models;
using EPS3.Helpers;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq.Expressions;

namespace EPS3.Controllers
{
    public class MessagesController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<MessagesController> _logger;
        private PermissionsUtils _pu;

        public MessagesController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<MessagesController>();
            _pu = new PermissionsUtils(_context);
        }

        // GET: Messages
        [HttpGet]
        public IActionResult List()
        {
            User user = GetUser();
            if (user == null)
            {
                return RedirectToAction("List", "LineItemGroups");
            }
            ViewBag.Roles = _pu.GetUserRoles(user.UserLogin);
            var recipients = _context.MessageRecipients
                .Where(m => m.User == user)
                .AsNoTracking()
                .OrderByDescending(m => m.MessageID)
                .ToList();
            List<int> messageIDs = new List<int>();
            foreach(MessageRecipient mr in recipients)
            {
                messageIDs.Add(mr.MessageID);
            }
            var messages = _context.Messages
                .Where(Utils.BuildOrExpression<Message, int>(m => m.MessageID, messageIDs.ToArray<int>()))
                .Include(m => m.Recipients)
                .ThenInclude(r => r.User)
                .AsNoTracking()
                .OrderByDescending(m => m.MessageDate)
                .ToList();

            ViewBag.CurrentUser = user;
            return View(messages);

        }


       

        private User GetUser()
        {
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
            return pu.GetUser(pu.GetLogin(userLogin));
        } // end GetUser
    } // end MessagesController
} // end Controllers namespace
