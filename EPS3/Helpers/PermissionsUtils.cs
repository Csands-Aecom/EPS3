using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EPS3.Models;
using EPS3.DataContexts;
using Serilog;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Microsoft.AspNetCore.Http;

namespace EPS3.Helpers
{
    public class PermissionsUtils
    {
        private EPSContext _context;
        private readonly ILogger<Object> _logger;
        private readonly HttpContext _httpContext;
        public PermissionsUtils(EPSContext context, ILogger callingLogger)
        {
            _context = context;
            _logger = (ILogger<Object>)callingLogger;
        }

        public PermissionsUtils(EPSContext context, ILogger callingLogger, HttpContext httpContext) : this(context, callingLogger)
        {
            _httpContext = httpContext;
        }


        public string GetLogin(string userLogin)
        {
            // userLogin = HttpContext.User.Identity.Name;
            //string TURNPIKE_DOMAIN = "TP";
            //if (userLogin.Contains(TURNPIKE_DOMAIN))
            //{
            try
            {
                //strips off the domain, per FTE best practice recommendation
                int stop = userLogin.IndexOf("\\");
                userLogin = (stop > -1) ? userLogin.Substring(stop + 1, userLogin.Length - stop - 1) : userLogin;
                userLogin = userLogin.Substring(0, 7);
                userLogin = userLogin.ToUpper();
            }catch(Exception e){
                _logger.LogError("PermissionsUtils.GetLogin Error:" + e.GetBaseException());
            }
            //}
            return userLogin;
        }
        public User GetUser(string userLoginName)
        {
            if (userLoginName != null)
            {
                try
                {
                    User currentUser = (User)_context.Users
                    .Where(u => u.UserLogin == userLoginName)
                    .AsNoTracking()
                    .SingleOrDefault();
                    return currentUser;
                } catch (Exception e)
                {
                    throw e;
                }
            }
            return null;
        }

        

        public string GetUserRoles(string userLoginName)
        {
            // returns a string concatenation of all of the users UserRole.Role names
            string roles = "";
            User user = GetUser(userLoginName);
            try {
                if (user != null)
                {
                    List<UserRole> rolesList = _context.UserRoles
                           .Where(ur => ur.UserID == user.UserID)
                           .AsNoTracking()
                           .ToList();
                    for (int i = 0; i < rolesList.Count; i++)
                    {
                        roles += rolesList[i].Role;
                    }
                    return roles;
                }
            }catch (Exception e)
            {
                throw e;
            }
            return roles;
        }
        public List<User> GetUsersByRole(string role)
        {
            try{
                //TODO: Exclude users where IsDisabled == 1
                List<int> userIDs = _context.Users.AsNoTracking()
                    .SelectMany(u => u.Roles.Where(r => r.Role.Equals(role))).Select(r => r.UserID).ToList();
                List<User> roleUsers = _context.Users.AsNoTracking().Where(Utils.BuildOrExpression<User, int>(u => u.UserID, userIDs.ToArray<int>())).ToList();

                return roleUsers;
            }catch(Exception e)
            {
                Log.Error("PermissionsUtils.GetUsersByRole Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetUsersByRole Error:" + e.GetBaseException());
                return null;
            }
        }

        public bool UserIsAdmin(string userLogin)
        {
            User currentUser = _context.Users
                .Include(u => u.Roles)
                .Where(u => u.IsDisabled == 0)
                .SingleOrDefault(u => u.UserLogin == userLogin);
            if (currentUser == null) { return false; }
            foreach (UserRole role in currentUser.Roles)
            {
                if (role.Role.Equals(ConstantStrings.AdminRole))
                {
                    return true;
                }
            }
            return false;
        }

        //todo this belongs somewhere else
        public static string GetCurrentFiscalYear()
        {
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            if (month > 6)
            {
                return year.ToString() + " - " + (year + 1).ToString();
            }
            else
            {
                return (year - 1).ToString() + " - " + year.ToString();
            }
        }

        [Obsolete ("Not used")]
        public string GetStatusDropdown(Contract contract, User user)
        {
            string dropdown = "";
            string userRoles = GetUserRoles(user.UserLogin);
            if(userRoles.Contains(ConstantStrings.AdminRole)
                || userRoles.Contains(ConstantStrings.FinanceReviewer)
                || (userRoles.Contains(ConstantStrings.Originator) && user.UserID == contract.UserID))
            {
                dropdown += "<option value = 'Draft'> " + ConstantStrings.Draft + "</option>";
                dropdown += "<option value = 'SubmittedFinance'> " + ConstantStrings.SubmittedFinance + "</option>";
            }
            if(contract.CurrentStatus.Equals(ConstantStrings.SubmittedFinance)
                && ((userRoles.Contains(ConstantStrings.AdminRole)
                || (userRoles.Contains(ConstantStrings.FinanceReviewer) || userRoles.StartsWith(ConstantStrings.FinanceReviewer) || userRoles.Equals(ConstantStrings.FinanceReviewer)))))
            {
                dropdown += "<option value = 'SubmittedWP'> " + ConstantStrings.SubmittedWP + "</option>";
            }
            if(contract.CurrentStatus == ConstantStrings.SubmittedWP
                && (userRoles.Contains(ConstantStrings.AdminRole)
                || userRoles.Contains(ConstantStrings.FinanceReviewer)
                || userRoles.Contains(ConstantStrings.WPReviewer)))
            {
                dropdown += "<option value = 'CFMReady'> Bypass Work Program</option>";
            }
            if(userRoles.Contains(ConstantStrings.AdminRole)
                || userRoles.Contains(ConstantStrings.FinanceReviewer))
            {
                dropdown += "<option value = 'CFMReady'>" +ConstantStrings.CFMReady+ "</option>";
            }
            if((contract.CurrentStatus == ConstantStrings.CFMReady)
                && (userRoles.Contains(ConstantStrings.AdminRole)
                || userRoles.Contains(ConstantStrings.FinanceReviewer)))
            {
                dropdown += "<option value = 'CFMComplete'> " + ConstantStrings.CFMComplete + "</option>";
            }
            return dropdown;
        }

        

 
    }
}
