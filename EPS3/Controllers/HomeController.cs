using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EPS3.Models;
using EPS3.Helpers;
using EPS3.DataContexts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPS3.Controllers
{
    public class HomeController : Controller
    {

        private EPSContext _context;
        private readonly ILogger<LineItemGroupsController> _logger;
        private PermissionsUtils _pu;
        public SmtpConfig SmtpConfig { get; }
        private MessageService _messenger;

        public HomeController(EPSContext context, ILoggerFactory loggerFactory, IOptions<SmtpConfig> smtpConfig)
        {
            _context = context;
            _pu = new PermissionsUtils(context);
            _logger = loggerFactory.CreateLogger<LineItemGroupsController>();
            SmtpConfig = smtpConfig.Value;
        }
        public IActionResult Index()
        {
            string userLogin = GetLogin();
            User currentUser = _context.Users.SingleOrDefault(u => u.UserLogin == userLogin);
            // If userLogin is not authorized, redirect to About page
            // If userLogin is an authorized user, redirect to Contracts list
            ViewBag.Roles = _pu.GetUserRoles(userLogin);
            if (currentUser == null)
            {
                return RedirectToAction("About");
            }
            else
            {
                //return RedirectToAction("List", "Contracts");
                return RedirectToAction("List", "LineItems");
            }
            //return View();
        }

        public IActionResult About()
        {
           

            string userLogin = GetLogin();
            User currentUser = _context.Users.SingleOrDefault(u => u.UserLogin == userLogin);
            if (currentUser == null)
            {
                ViewData["Message"] = "You are not currently an authorized user of the EPS application.";
            }
            ViewBag.CurrentUser = currentUser;
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            string url = this.Request.Scheme + "://" + this.Request.Host;
            _messenger = new MessageService(_context, SmtpConfig, url);
            _messenger.SendErrorNotification(HttpContext.Response.ToString());
            ViewBag.Environment=Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public void OnAuthentication()
        {

        }
        private string GetLogin()
        {
            string userLogin = "";
            PermissionsUtils pu = new PermissionsUtils(_context);
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")=="Development")
            {
                userLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            }
            else
            {
                userLogin = HttpContext.User.Identity.Name;
            }
            return pu.GetLogin(userLogin);
        }

        private void NotifyAdmin()
        {
            // Send an email to the EPS2 Administrator to notify that an error has occurred
            // Include as much debugging information as possible.
        }

        public IActionResult FAQ()
        {
            ViewData["Title"] = "Frequently Asked Questions";
            ViewData["Message"] = "Your FAQ page.";

            return View();
        }

        public IActionResult Help()
        {
            ViewData["Title"] = "EPS Help";
            ViewData["Message"] = "Your help page.";

            return View();
        }
    }
}
