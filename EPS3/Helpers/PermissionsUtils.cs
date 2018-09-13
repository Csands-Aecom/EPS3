using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EPS3.Models;
using EPS3.DataContexts;

namespace EPS3.Helpers
{
    public class PermissionsUtils
    {
        private EPSContext _context;
        public PermissionsUtils(EPSContext context)
        {
            _context = context;
        }
        public string GetLogin(string userLogin)
        {
            // userLogin = HttpContext.User.Identity.Name;
            string TURNPIKE_DOMAIN = "TP";
            //if (userLogin.Contains(TURNPIKE_DOMAIN))
            //{
                int stop = userLogin.IndexOf("\\");
                userLogin = (stop > -1) ? userLogin.Substring(stop + 1, userLogin.Length - stop - 1) : userLogin;
                userLogin = userLogin.Substring(0, 7);
                userLogin = userLogin.ToUpper();
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

        public Contract GetContractByID(int contractID)
        {
            // returns the Contract with the specified contractID
            if (contractID > 0)
            {
                try
                {
                    Contract contract = _context.Contracts
                    .Where(c => c.ContractID == contractID)
                    .AsNoTracking()
                    .SingleOrDefault();
                    return contract;
                }catch(Exception e)
                {
                    throw e;
                }
            }
            return null;
        }

        public List<Contract> GetContractsByStatus(string status)
        {
            // returns Contracts with current status matching the specified status
            try { 
            List<Contract> contracts = (List<Contract>)_context.Contracts
                .Include(c => c.Vendor)
                .Include(c => c.ContractType)
                .Include(c => c.User)
                .Where(c => c.CurrentStatus.Contains(status))
                .AsNoTracking()
                .ToList();
            return (contracts);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public List<Contract> GetOriginatorOwnedContracts(int userID)
        {
            // returns Contracts created by the specified user
            try { 
            List<Contract> contracts = (List<Contract>)_context.Contracts
                .Include(c => c.Vendor)
                .Include(c => c.ContractType)
                .Include(c => c.User)
                .Where(c => c.UserID == userID)
                .AsNoTracking()
                .ToList();
            return (contracts);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

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
