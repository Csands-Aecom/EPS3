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

namespace EPS3.Helpers
{
    public class PermissionsUtils
    {
        private EPSContext _context;
        private readonly ILogger<Object> _logger;
        public PermissionsUtils(EPSContext context, ILogger callingLogger)
        {
            _context = context;
            _logger = (ILogger<Object>)callingLogger;
        }


        public string GetLogin(string userLogin)
        {
            // userLogin = HttpContext.User.Identity.Name;
            //string TURNPIKE_DOMAIN = "TP";
            //if (userLogin.Contains(TURNPIKE_DOMAIN))
            //{
            try
            {
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

        public User GetUserByID(int userID)
        {
            if (userID > 0)
            {
                try
                {
                    User currentUser = (User)_context.Users
                    .Where(u => u.UserID == userID)
                    .AsNoTracking()
                    .SingleOrDefault();
                    return currentUser;
                }
                catch (Exception e)
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

        public Contract GetContractByEncumbranceID(int encumbranceID)
        {
            try
            {
                int contractID = _context.LineItemGroups
                                    .AsNoTracking()
                                    .Where(e => e.GroupID == encumbranceID)
                                    .Select(e => e.ContractID)
                                    .SingleOrDefault();
                return GetContractByID(contractID);
            }
            catch (Exception e)
            {
                Log.Error("PermissionsUtils.GetContractByEncumbranceID Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetContractByEncumbranceID Error:" + e.GetBaseException());
                throw e;
            }
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
                    Log.Error("PermissionsUtils.GetContractByID Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                    _logger.LogError("PermissionsUtils.GetContractByID Error:" + e.GetBaseException());
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
                Log.Error("PermissionsUtils.GetContractsByStatus Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetContractByStatus Error:" + e.GetBaseException());
                return null;
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
                Log.Error("PermissionsUtils.GetOriginatorOwnedContracts Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetOriginatorOwnedContract Error:" + e.GetBaseException());
                return null;
            }
        }
        public string GetCurrentFiscalYear()
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

        public bool IsShallowContract(Contract contract)
        {
              // return true if contract does not include child elements
            if(contract.LineItems == null && HasLineItems(contract)) { return true; }
            if(contract.ProcurementID > 0 && contract.MethodOfProcurement == null) { return true; }
            return false;
        }
        public bool IsShallowEncumbrance(LineItemGroup encumbrance)
        {
            // return true if encumbrance does not include child elements (i.e., LineItems)
            if(encumbrance.LineItems == null && HasLineItems(encumbrance)) { return true; }
            if(encumbrance.OriginatorUserID > 0 && encumbrance.OriginatorUser == null) { return true; }
            return false;
        }

        public bool HasLineItems(Contract contract)
        {
            int itemCount = _context.LineItems.Where(li => li.ContractID == contract.ContractID).Count();
            return (itemCount > 0);
        }
        public bool HasLineItems(LineItemGroup encumbrance)
        {
            int itemCount = _context.LineItems.Where(li => li.LineItemGroupID == encumbrance.GroupID).Count();
            return (itemCount > 0);
        }

        public Contract GetDeepContract(int contractID)
        {
            try
            {
                Contract contract = _context.Contracts.AsNoTracking()
                    .Include(c => c.ContractFunding)
                    .Include(c => c.MethodOfProcurement)
                    .Include(c => c.Vendor)
                    .Include(c => c.User)
                    .Include(c => c.Recipient)
                    .Include(c => c.ContractType)
                    .SingleOrDefault(c => c.ContractID == contractID);
                return contract;
            }
            catch (Exception e)
            {
                Log.Error("PermissionsUtils.GetDeepContract Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetDeppContract Error:" + e.GetBaseException());
                return null;
            }
        }

        public LineItemGroup GetDeepEncumbrance(int groupID)
        {
            try
            {
                LineItemGroup encumbrance = _context.LineItemGroups.AsNoTracking()
                    .Include(l => l.LastEditedUser)
                    .Include(l => l.OriginatorUser)
                    .Include(l => l.Contract)
                    .Include(l => l.FileAttachments)
                    .Include(l => l.LineItems).ThenInclude(li => li.OCA)
                    .Include(l => l.LineItems).ThenInclude(li => li.Category)
                    .Include(l => l.LineItems).ThenInclude(li => li.StateProgram)
                    .Include(l => l.LineItems).ThenInclude(li => li.Fund)
                    .Include(l => l.Statuses).ThenInclude(gst => gst.User)
                    .SingleOrDefault(l => l.GroupID == groupID);
                return encumbrance;
            }
            catch (Exception e)
            {
                Log.Error("PermissionsUtils.GetDeepEncumbrance Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetDeepEncumbrance Error:" + e.GetBaseException());
                return null;
            }
        }

        public List<LineItemGroup> GetDeepEncumbrances(int contractID)
        {
            try
            {
                List<LineItemGroup> encumbrances = _context.LineItemGroups.AsNoTracking()
                    .Include(l => l.Contract)
                    .Include(l => l.LastEditedUser)
                    .Include(l => l.OriginatorUser)
                    .Include(l => l.LineItems).ThenInclude(li => li.OCA)
                    .Include(l => l.LineItems).ThenInclude(li => li.Category)
                    .Include(l => l.LineItems).ThenInclude(li => li.StateProgram)
                    .Include(l => l.LineItems).ThenInclude(li => li.Fund)
                    .Include(l => l.Statuses).ThenInclude(gst => gst.User)
                    .Where(l => l.ContractID == contractID)
                    .ToList();
                return encumbrances;
            }
            catch (Exception e)
            {
                Log.Error("PermissionsUtils.GetDeepEncumbrances Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetDeepEncumbrances Error:" + e.GetBaseException());
                return null;
            }
        }
        public LineItem GetDeepLineItem(int lineItemID)
        {
            try
            {
                LineItem item = _context.LineItems.AsNoTracking()
                    .Include(l => l.Category)
                    .Include(l => l.Fund)
                    .Include(l => l.OCA)
                    .Include(l => l.StateProgram)
                    .OrderBy(l => l.LineNumber)
                    .SingleOrDefault(l => l.LineItemID == lineItemID);
                return item;
            }catch(Exception e)
            {
                Log.Error("PermissionsUtils.GetDeepLineItem Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetDeepLineItem Error:" + e.GetBaseException());
                return null;
            }
        }

        public List<LineItem> GetDeepLineItems(int groupID)
        {
            try
            {
                List<LineItem> items = _context.LineItems.AsNoTracking()
                    .Include(l => l.Category)
                    .Include(l => l.Fund)
                    .Include(l => l.OCA)
                    .Include(l => l.StateProgram)
                    .OrderBy(l => l.LineNumber)
                    .Where(l => l.LineItemGroupID == groupID)
                    .ToList();
                return items;
            }
            catch(Exception e)
            {
                Log.Error("PermissionsUtils.GetDeepLineItems Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetDeepLineItems Error:" + e.GetBaseException());
                return null;
            }
        }

        public Dictionary<int, List<LineItemGroupStatus>> GetDeepContractEncumbranceStatusMap(int contractID)
        {
            try
            {
                Dictionary<int, List<LineItemGroupStatus>> resultMap = new Dictionary<int, List<LineItemGroupStatus>>();
                List<int> encumbranceIDs = _context.LineItemGroups
                    .AsNoTracking()
                    .Where(e => e.ContractID == contractID)
                    .Select(e => e.GroupID)
                    .ToList();
                foreach (int groupID in encumbranceIDs)
                {
                    resultMap.Add(groupID, GetDeepEncumbranceStatuses(groupID));
                }
                return resultMap;
            }
            catch(Exception e)
            {
                Log.Error("PermissionsUtils.GetDeepContractEncumbranceStatusMap Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetDeepContractEncumbranceStatusMap Error:" + e.GetBaseException());
                return null;
            }
        }
        public List<LineItemGroupStatus> GetDeepEncumbranceStatuses(int groupID)
        {
            try
            {
                List<LineItemGroupStatus> resultList = _context.LineItemGroupStatuses
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Where(s => s.LineItemGroupID == groupID)
                    .OrderBy(s => s.SubmittalDate)
                    .ToList();

                return resultList;
            }catch(Exception e)
            {
                Log.Error("PermissionsUtils.GetDeepEncumbranceStatuses Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                _logger.LogError("PermissionsUtils.GetDeepEncumbranceStatuses Error:" + e.GetBaseException());
                return null;
            }
        }

        public decimal GetTotalAmountOfAllEncumbrances(int ContractID)
        {
            List<LineItemGroup> encumbrances = _context.LineItemGroups.AsNoTracking()
                .Include(l => l.LineItems)
                .Where(l => l.ContractID == ContractID).ToList();
            decimal totalAmount = 0.0m;
            foreach (LineItemGroup encumbrance in encumbrances)
            {
                if (encumbrance.LineItemType != ConstantStrings.Advertisement)
                {
                    foreach (LineItem lineitem in encumbrance.LineItems)
                    {
                        totalAmount += lineitem.Amount;
                    }
                }
            }
            return totalAmount;
        }
    }
}
