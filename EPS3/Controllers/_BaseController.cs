using EPS3.DataContexts;
using EPS3.Helpers;
using EPS3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EPS3.Controllers
{
    public abstract class _BaseController : Controller
    {

        protected readonly EPSContext _context;
        protected readonly ILogger<LineItemGroupsController> _logger;

        public SmtpConfig SmtpConfig { get; }

        public _BaseController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<LineItemGroupsController>();
        }

        public _BaseController(EPSContext context, ILoggerFactory loggerFactory, IOptions<SmtpConfig> smtpConfig)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<LineItemGroupsController>();
            SmtpConfig = smtpConfig.Value;
        }
        protected string GetCurrentUserLoginName()
        {
            try
            {
                String userLogin = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development") ? System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString() : HttpContext.User.Identity.Name;
                //strips off the domain, per FTE best practice recommendation
                int stop = userLogin.IndexOf("\\");
                userLogin = (stop > -1) ? userLogin.Substring(stop + 1, userLogin.Length - stop - 1) : userLogin;
                userLogin = userLogin.Substring(0, 7);
                userLogin = userLogin.ToUpper();
                return userLogin;
            }
            catch (Exception e)
            {
                _logger.LogError("_BaseController.GetCurrentUserLoginName Error:" + e.GetBaseException());
                return "";
            }
        }

        private User _currentUser;
        private ReadOnlyCollection<String> _currentUserRoles;

        public User GetCurrentUser()
        {
            if (_currentUser == null) { 
                try
                {
                    _currentUser = (User)_context.Users
                        .Where(u => u.UserLogin == GetCurrentUserLoginName())
                        .Include(u => u.Roles)
                        .AsNoTracking()
                        .SingleOrDefault();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return _currentUser;
        }

        
        public ReadOnlyCollection<String> GetCurrentUserRoles()
        {
            // returns a string array of all of the users UserRole.Role names
            if (_currentUserRoles == null)
            {
                if (GetCurrentUser() != null)
                {
                    _currentUserRoles = GetCurrentUser().Roles.Select(r => r.Role).ToList<string>().AsReadOnly();
                } else
                {
                    _currentUserRoles = new List<string>().AsReadOnly(); //empty
                }
            }
            return _currentUserRoles;
        }

        public void PopulateUserViewBag(int? contractId)
        {
            string roles = String.Join(' ', GetCurrentUserRoles()); //for backward compatibility with older code, a space - separated list of the users roles
            Contract contract = null;
            //Certain actions will not have a contract id, e.g. LineItemGroups/Create, Contracts/Create
            if (contractId.HasValue)
            {
                contract = _context.Contracts.Find(contractId);
            }


            ViewBag.Contract = contract;
            if (contract == null)
            {
                ViewBag.ContractID = 0;
            }
            else
            {
                ViewBag.ContractID = contract.ContractID;
            }
            ViewBag.CurrentUser = GetCurrentUser();
            ViewBag.Roles = roles;
        }


    }
}
