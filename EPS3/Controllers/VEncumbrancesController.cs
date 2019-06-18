using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EPS3.DataContexts;
using EPS3.Models;
using EPS3.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;


namespace EPS3.Controllers
{
    public class VEncumbrancesController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<VEncumbrancesController> _logger;
        private PermissionsUtils _pu;

        public VEncumbrancesController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<VEncumbrancesController>();
            _pu = new PermissionsUtils(_context, _logger);
        }


        // GET: Encumbrances
        [HttpGet]
        public IActionResult List()
        {
            PopulateViewBag(0);
            User user = ViewBag.CurrentUser;
            Dictionary<string, List<VEncumbrance>> EncumbrancesMap = new Dictionary<string, List<VEncumbrance>>();
            if (ViewBag.Roles.Contains(ConstantStrings.Originator))
            {
                EncumbrancesMap.Add("MyRequests", getCurrentUserEncumbrances(user));
            }
            Dictionary<string, List<VEncumbrance>> categorizedEncumbrances = getCategorizedEncumbrances(user);
            foreach (string mapKey in categorizedEncumbrances.Keys)
            {
                EncumbrancesMap.Add(mapKey, categorizedEncumbrances[mapKey]);
            }

            if (ViewBag.Roles.Contains(ConstantStrings.Originator) || ViewBag.Roles.Contains(ConstantStrings.AdminRole))
            {
                EncumbrancesMap.Add(ConstantStrings.Advertisement, getAdvertisedEncumbrances());
                // pass a list of valid Advertisement GroupIDs for a lookup in the ViewBag
                List<int> adIDs = new List<int>();
                foreach (VEncumbrance ad in EncumbrancesMap[ConstantStrings.Advertisement])
                {
                    adIDs.Add(ad.GroupID);
                }
                ViewBag.AdIDs = adIDs;
            }

            return View(EncumbrancesMap);
        }

        private Dictionary<string, List<VEncumbrance>> getCategorizedEncumbrances(User user)
        {
            Dictionary<string, List<VEncumbrance>> results = new Dictionary<string, List<VEncumbrance>>();

            // These originally depended on roles. 
            // New approach is to return all results from non-archived contracts
            // and use roles to determine if links are included in the View
            // This method does NOT return Encumbrances that are CFMComplete
            List<VEncumbrance> allEncumbrances = new List<VEncumbrance>();
            if (user == null)
            {
                allEncumbrances = _context.VEncumbrances.AsNoTracking()
                .Where(e => e.ContractStatus != (ConstantStrings.CloseContract))
                .OrderByDescending(e => e.GroupID)
                .Take(200)
                .ToList();
            }
            else
            {
                string roles = _pu.GetUserRoles(user.UserLogin);
                // add Line IDs for Groups in Finance if user has Finance role
                List<VEncumbrance> financeEncumbrances = _context.VEncumbrances.AsNoTracking()
                    .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.SubmittedFinance))
                    .ToList();
                if (roles.Contains(ConstantStrings.FinanceReviewer))
                {
                    results.Add(ConstantStrings.SubmittedFinance, financeEncumbrances);
                }
                allEncumbrances.AddRange(financeEncumbrances);

                // add Line IDs for Groups in Work Program if user has WP role
                List<VEncumbrance> wpEncumbrances = _context.VEncumbrances.AsNoTracking()
                    .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.SubmittedWP))
                    .ToList();
                if (roles.Contains(ConstantStrings.WPReviewer))
                {
                    results.Add("WP", wpEncumbrances);
                }
                allEncumbrances.AddRange(wpEncumbrances);

                // add Line IDs for Groups in CFM Ready 
                List<VEncumbrance> cfmEncumbrances = _context.VEncumbrances.AsNoTracking()
                    .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.CFMReady))
                    .ToList();
                if (roles.Contains(ConstantStrings.CFMSubmitter))
                {
                    results.Add(ConstantStrings.CFMReady, cfmEncumbrances);
                }
                allEncumbrances.AddRange(cfmEncumbrances);

                List<VEncumbrance> origEncumbrances = new List<VEncumbrance>();
                if (roles.Contains(ConstantStrings.AdminRole))
                {
                    // add Group IDs for all Groups in Draft if user is Admin
                    origEncumbrances = _context.VEncumbrances.AsNoTracking()
                        .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.Draft))
                        .ToList();
                }
                else if (roles.Contains(ConstantStrings.Originator))
                {
                    // add Group IDs for Groups in Draft if user has the originator role and is the originator of the encumbrance
                    origEncumbrances = _context.VEncumbrances.AsNoTracking()
                        .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.Draft) && e.OriginatorUserID==(user.UserID))
                        .ToList();
                }
                results.Add(ConstantStrings.Draft, origEncumbrances);
                allEncumbrances.AddRange(origEncumbrances);

                // add Groups that have been input to CFM
                List<VEncumbrance> completeEncumbrances = _context.VEncumbrances.AsNoTracking()
                    .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.CFMComplete) && e.ContractStatus != ConstantStrings.ContractArchived)
                    .OrderByDescending(e => e.GroupID)
                    .ToList();
                results.Add("Processed", completeEncumbrances);
                allEncumbrances.AddRange(completeEncumbrances);

                // add  Groups that are closed
                List<VEncumbrance> closedEncumbrances = _context.VEncumbrances.AsNoTracking()
                        .Where(e => e.EncumbranceStatus.Contains("Closed") && e.ContractStatus != ConstantStrings.ContractArchived)
                        .OrderByDescending(e => e.GroupID)
                        .ToList();
                results.Add("Closed", closedEncumbrances);
                allEncumbrances.AddRange(closedEncumbrances);
            }
            results.Add("Complete", allEncumbrances);
            return results;
        }

        private List<VEncumbrance> getCurrentUserEncumbrances(User user)
        {
           // _context.Database.
            var foo = _context.VEncumbrances.First();

            List<VEncumbrance> myGroups = _context.VEncumbrances.AsNoTracking()
                    .Where(e => e.OriginatorUserID == user.UserID && e.EncumbranceStatus != "Closed" &&  e.ContractStatus != ConstantStrings.ContractArchived)
                    .OrderByDescending(e => e.GroupID)
                    .ToList();

            return myGroups;
        }

        private List<VEncumbrance> getAdvertisedEncumbrances()
        {
            // add  Groups that are unawarded Advertisements
            List<VEncumbrance> adGroups = _context.VEncumbrances.AsNoTracking()
                .Where(e => e.LineItemType.Equals(ConstantStrings.Advertisement)
                    && e.EncumbranceStatus.Equals(ConstantStrings.CFMComplete)
                    && e.ContractStatus != ConstantStrings.ContractArchived)
                .ToList();
            // For each adGroup, if the contract has a matching, submitted, Award group, then add it to Award groups
            List<VEncumbrance> awardedGroups = new List<VEncumbrance>();
            foreach (VEncumbrance adGroup in adGroups)
            {
                int contractID = adGroup.ContractID;
                List<VEncumbrance> awardGroups = _context.VEncumbrances.AsNoTracking()
                    .Where(e => e.LineItemType.Equals(ConstantStrings.Award) && e.ContractID == contractID)
                    .ToList();
                if (awardGroups.Count > 0)
                {
                    awardedGroups.Add(adGroup);
                }
            }
            // return adGroups minus awardedGroups, which contains Advertisement encumbrances minus those with already-submitted Awards
            return adGroups.Except(awardedGroups).ToList();
        }

        private string GetLogin()
        {
            string userLogin = "";
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                userLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            }
            else
            {
                userLogin = HttpContext.User.Identity.Name;
            }
            return _pu.GetLogin(userLogin);
        }
        public void PopulateViewBag(int contractID)
        {
            //const string sessionKey = "CurrentUser";
            string userLogin = GetLogin();

            if (userLogin != null)
            {
                try
                {
                    User currentUser = _pu.GetUser(userLogin);
                    string roles = _pu.GetUserRoles(userLogin);
                    Contract contract = _pu.GetContractByID(contractID);
                    ViewBag.Contract = contract;
                    if (contract == null)
                    {
                        ViewBag.ContractID = 0;
                    }
                    else
                    {
                        ViewBag.ContractID = contract.ContractID;
                    }
                    ViewBag.CurrentUser = currentUser;
                    ViewBag.Roles = roles;
                }
                catch (Exception e)
                {
                    _logger.LogError("LineItemGroupsController.PopulateViewBag Error:" + e.GetBaseException());
                    Log.Error("LineItemGroupsController.PopulateViewBag  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
            }
            else
            {
                RedirectToAction("Home");
            }
        }
    }
}
