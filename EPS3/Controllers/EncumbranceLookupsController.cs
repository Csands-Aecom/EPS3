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
using System;

namespace EPS3.Controllers
{
    public class EncumbranceLookupsController : Controller
    { 
        private readonly EPSContext _context;
        private readonly ILogger<EncumbranceLookupsController> _logger;
        private PermissionsUtils _pu;

        public EncumbranceLookupsController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<EncumbranceLookupsController>();
            _pu = new PermissionsUtils(_context, _logger);
        }


        // GET: Encumbrances
        [HttpGet]
        public IActionResult List()
        {
            PopulateViewBag(0);
            User user = ViewBag.CurrentUser;
            Dictionary<string, List<EncumbranceLookup>> EncumbrancesMap = new Dictionary<string, List<EncumbranceLookup>>();
            if (ViewBag.Roles.Contains(ConstantStrings.Originator))
            {
                EncumbrancesMap.Add("MyRequests", getCurrentUserEncumbrances(user));
            }
            Dictionary<string, List<EncumbranceLookup>> categorizedEncumbrances = getCategorizedEncumbrances(user);
            foreach (string mapKey in categorizedEncumbrances.Keys)
            {
                EncumbrancesMap.Add(mapKey, categorizedEncumbrances[mapKey]);
            }

            if (ViewBag.Roles.Contains(ConstantStrings.Originator) || ViewBag.Roles.Contains(ConstantStrings.AdminRole))
            {
                EncumbrancesMap.Add(ConstantStrings.Advertisement, getAdvertisedEncumbrances());
                // pass a list of valid Advertisement GroupIDs for a lookup in the ViewBag
                List<int> adIDs = new List<int>();
                foreach (EncumbranceLookup ad in EncumbrancesMap[ConstantStrings.Advertisement])
                {
                    adIDs.Add(ad.GroupID);
                }
                ViewBag.AdIDs = adIDs;
            }

            return View(EncumbrancesMap);
        }

        private Dictionary<string, List<EncumbranceLookup>> getCategorizedEncumbrances(User user)
        {
            Dictionary<string, List<EncumbranceLookup>> results = new Dictionary<string, List<EncumbranceLookup>>();

            // These originally depended on roles. 
            // New approach is to return all results from non-archived contracts
            // and use roles to determine if links are included in the View
            // This method does NOT return Encumbrances that are CFMComplete
            List<EncumbranceLookup> allEncumbrances = new List<EncumbranceLookup>();
            if (user == null)
            {
                allEncumbrances = _context.EncumbranceLookups.AsNoTracking()
                .Where(e => e.ContractStatus != (ConstantStrings.CloseContract))
                .OrderByDescending(e => e.GroupID)
                .Take(200)
                .ToList();
            }
            else
            {
                string roles = _pu.GetUserRoles(user.UserLogin);
                // add Line IDs for Groups in Finance if user has Finance role
                List<EncumbranceLookup> financeEncumbrances = _context.EncumbranceLookups.AsNoTracking()
                    .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.SubmittedFinance))
                    .ToList();
                if (roles.Contains(ConstantStrings.FinanceReviewer))
                {
                    results.Add(ConstantStrings.SubmittedFinance, financeEncumbrances);
                }
                allEncumbrances.AddRange(financeEncumbrances);

                // add Line IDs for Groups in Work Program if user has WP role
                List<EncumbranceLookup> wpEncumbrances = _context.EncumbranceLookups.AsNoTracking()
                    .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.SubmittedWP))
                    .ToList();
                if (roles.Contains(ConstantStrings.WPReviewer))
                {
                    results.Add("WP", wpEncumbrances);
                }
                allEncumbrances.AddRange(wpEncumbrances);

                // add Line IDs for Groups in CFM Ready 
                List<EncumbranceLookup> cfmEncumbrances = _context.EncumbranceLookups.AsNoTracking()
                    .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.CFMReady))
                    .ToList();
                if (roles.Contains(ConstantStrings.CFMSubmitter))
                {
                    results.Add(ConstantStrings.CFMReady, cfmEncumbrances);
                }
                allEncumbrances.AddRange(cfmEncumbrances);

                List<EncumbranceLookup> origEncumbrances = new List<EncumbranceLookup>();
                if (roles.Contains(ConstantStrings.AdminRole))
                {
                    // add Group IDs for all Groups in Draft if user is Admin
                    origEncumbrances = _context.EncumbranceLookups.AsNoTracking()
                        .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.Draft))
                        .ToList();
                }
                else if (roles.Contains(ConstantStrings.Originator))
                {
                    // add Group IDs for Groups in Draft if user has the originator role and is the originator of the encumbrance
                    origEncumbrances = _context.EncumbranceLookups.AsNoTracking()
                        .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.Draft) && e.OriginatorUserID==(user.UserID))
                        .ToList();
                }
                results.Add(ConstantStrings.Draft, origEncumbrances);
                allEncumbrances.AddRange(origEncumbrances);

                // add Groups that have been input to CFM
                List<EncumbranceLookup> completeEncumbrances = _context.EncumbranceLookups.AsNoTracking()
                    .Where(e => e.EncumbranceStatus.Equals(ConstantStrings.CFMComplete) && e.ContractStatus != ConstantStrings.ContractArchived)
                    .OrderByDescending(e => e.GroupID)
                    .ToList();
                results.Add("Processed", completeEncumbrances);
                allEncumbrances.AddRange(completeEncumbrances);

                // add  Groups that are closed
                List<EncumbranceLookup> closedEncumbrances = _context.EncumbranceLookups.AsNoTracking()
                        .Where(e => e.EncumbranceStatus.Contains("Closed") && e.ContractStatus != ConstantStrings.ContractArchived)
                        .OrderByDescending(e => e.GroupID)
                        .ToList();
                results.Add("Closed", closedEncumbrances);
                allEncumbrances.AddRange(closedEncumbrances);
            }
            results.Add("Complete", allEncumbrances);
            return results;
        }

        private List<EncumbranceLookup> getCurrentUserEncumbrances(User user)
        {
           // _context.Database.
            var foo = _context.EncumbranceLookups.First();

            List<EncumbranceLookup> myGroups = _context.EncumbranceLookups.AsNoTracking()
                    .Where(e => e.OriginatorUserID == user.UserID && e.EncumbranceStatus != "Closed" &&  e.ContractStatus != ConstantStrings.ContractArchived)
                    .OrderByDescending(e => e.GroupID)
                    .ToList();

            return myGroups;
        }

        private List<EncumbranceLookup> getAdvertisedEncumbrances()
        {
            // add  Groups that are unawarded Advertisements
            List<EncumbranceLookup> adGroups = _context.EncumbranceLookups.AsNoTracking()
                .Where(e => e.LineItemType.Equals(ConstantStrings.Advertisement)
                    && e.EncumbranceStatus.Equals(ConstantStrings.CFMComplete)
                    && e.ContractStatus != ConstantStrings.ContractArchived)
                .ToList();
            // For each adGroup, if the contract has a matching, submitted, Award group, then add it to Award groups
            List<EncumbranceLookup> awardedGroups = new List<EncumbranceLookup>();
            foreach (EncumbranceLookup adGroup in adGroups)
            {
                int contractID = adGroup.ContractID;
                List<EncumbranceLookup> awardGroups = _context.EncumbranceLookups.AsNoTracking()
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
            if (System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
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

        
        [HttpGet]
        public IActionResult Search(SearchForm search)
        {
            if(search == null)
            {
                return View();
            }
            int MAX_RESULT_LIMIT = 5000; // Maximum number of LineItems selectable before query crashes
            int INCREMENT_SIZE = 10;
            if (search != null && search.SearchContractNumber != null) { search.SearchContractNumber = search.SearchContractNumber.ToUpper(); }
            // All searches are ANDed together (intersection of lists for each type of search)
            // results are deduped just before being returned to the view (Distinct)
            List<EncumbranceLookup> results = new List<EncumbranceLookup>();
            //dollarResults = new List<EncumbranceLookup>();
            List<EncumbranceLookup>[] resultsArray = new List<EncumbranceLookup>[4]; // size is 4 for four potential lists to be ANDed together
            int arrayIndex = 0;
            ViewBag.SearchCriteria = getSearchCriteria(search);
            // Contract Only Search
            if(search.SearchContractNumber != null)
            {
                List<EncumbranceLookup> contractResults = _context.EncumbranceLookups
                    .AsNoTracking()
                    .Where(c => c.ContractNumber == search.SearchContractNumber)
                    .ToList();
                resultsArray[arrayIndex] = contractResults;
                arrayIndex++;
            }
            
            // Status Only Search
            if(search.SearchCurrentStatus != null && !search.SearchCurrentStatus.Equals("None"))
            {
                List<EncumbranceLookup> statusResults = _context.EncumbranceLookups
                    .AsNoTracking()
                    .Where(c => c.EncumbranceStatus.Equals(search.SearchCurrentStatus))
                    .ToList();
                resultsArray[arrayIndex] = statusResults;
                arrayIndex++;

            }

            // Date Range Search
            if(search.SearchStartDate != null || search.SearchEndDate != null)
            {
                if(search.SearchStartDate == null ) { search.SearchStartDate = new DateTime(2001, 1, 1);  }
                if(search.SearchEndDate == null ) { search.SearchEndDate = DateTime.Now;  }
                List<EncumbranceLookup> dateResults = _context.EncumbranceLookups
                    .AsNoTracking()
                    .Where(e => e.OriginatedDate >= search.SearchStartDate && e.OriginatedDate <= search.SearchEndDate)
                    .ToList();
                resultsArray[arrayIndex] = dateResults;
                arrayIndex++;
            }

            // If a Dollar Amount is selected but no checkbox (IsContractAmount = IsEncumbranceAmount = IsLineItemAmount = false)
            // default the search to LineItemAmount
            if((search.SearchMinAmount != null || search.SearchMaxAmount != null) && 
                (!search.IsContractAmount && !search.IsEncumbranceAmount && !search.IsLineItemAmount))
            {
                search.IsLineItemAmount = true;
            }
            // Dollar Amount Search : EncumbranceAmount
            if((search.SearchMinAmount != null || search.SearchMaxAmount != null) && search.IsEncumbranceAmount)
            {
                // For now, select on Encumbrance Total from EncumbranceLookups
                if(search.SearchMaxAmount == null)
                {
                    // Amount >= search.searchMinAmount
                    List<EncumbranceLookup> dollarResults = _context.EncumbranceLookups
                        .AsNoTracking()
                        .Where(e => e.EncumbranceAmount >= search.SearchMinAmount)
                        .ToList();
                    resultsArray[arrayIndex] = dollarResults;
                    arrayIndex++;
                }
                if(search.SearchMinAmount == null)
                {
                    // Amount <= search.searchMaxAmount
                    List<EncumbranceLookup> dollarResults = _context.EncumbranceLookups
                        .AsNoTracking()
                        .Where(e => e.EncumbranceAmount <= search.SearchMaxAmount)
                        .ToList();
                    resultsArray[arrayIndex] = dollarResults;
                    arrayIndex++;
                }
                if(search.SearchMinAmount != null && search.SearchMaxAmount != null)
                {
                    // Amount >= search.SearchMinAmount && Amount <= search.SearchMaxAmount
                    List<EncumbranceLookup> dollarResults = _context.EncumbranceLookups
                        .AsNoTracking()
                        .Where(e => e.EncumbranceAmount >= search.SearchMinAmount && e.EncumbranceAmount <= search.SearchMaxAmount)
                        .ToList();
                    resultsArray[arrayIndex] = dollarResults;
                    arrayIndex++;
                }
            }

            // Dollar Amount Search : ContractAmount
            if((search.SearchMinAmount != null || search.SearchMaxAmount != null) && search.IsContractAmount)
            {
                // For now, select on Encumbrance Total from EncumbranceLookup
                if(search.SearchMaxAmount == null)
                {
                    // Amount >= search.searchMinAmount
                    List<EncumbranceLookup> dollarResults = _context.EncumbranceLookups
                        .AsNoTracking()
                        .Where(e => e.ContractAmount >= search.SearchMinAmount)
                        .ToList();
                    resultsArray[arrayIndex] = dollarResults;
                    arrayIndex++;
                }
                if(search.SearchMinAmount == null)
                {
                    // Amount <= search.searchMaxAmount
                   List<EncumbranceLookup>  dollarResults = _context.EncumbranceLookups
                        .AsNoTracking()
                        .Where(e => e.ContractAmount <= search.SearchMaxAmount)
                        .ToList();
                    resultsArray[arrayIndex] = dollarResults;
                    arrayIndex++;
                }
                if(search.SearchMinAmount != null && search.SearchMaxAmount != null)
                {
                    // Amount >= search.SearchMinAmount && Amount <= search.SearchMaxAmount
                   List<EncumbranceLookup> dollarResults = _context.EncumbranceLookups
                        .AsNoTracking()
                        .Where(e => e.ContractAmount >= search.SearchMinAmount && e.ContractAmount <= search.SearchMaxAmount)
                        .ToList();
                    resultsArray[arrayIndex] = dollarResults;
                    arrayIndex++;
                }
            }
            // Dollar Amount Search : Line Item Amount
            if ((search.SearchMinAmount != null || search.SearchMaxAmount != null) && search.IsLineItemAmount)
            {
                // For now, select on Encumbrance Total from EncumbranceLookup
                List<int> dollarItems = new List<int>();
                if (search.SearchMaxAmount == null)
                {
                    // Amount >= search.searchMinAmount
                    dollarItems = _context.LineItems
                        .AsNoTracking()
                        .Where(l => l.Amount >= search.SearchMinAmount)
                        .Select(l => l.LineItemGroupID)
                        .ToList();
                }
                if (search.SearchMinAmount == null)
                {
                    // Amount <= search.searchMaxAmount
                    dollarItems = _context.LineItems
                        .AsNoTracking()
                        .Where(l => l.Amount <= search.SearchMaxAmount)
                        .Select(l => l.LineItemGroupID)
                        .ToList();
                }
                if (search.SearchMinAmount != null && search.SearchMaxAmount != null)
                {
                    // Amount >= search.SearchMinAmount && Amount <= search.SearchMaxAmount
                    dollarItems = _context.LineItems
                        .AsNoTracking()
                        .Where(l => l.Amount >= search.SearchMinAmount && l.Amount <= search.SearchMaxAmount)
                        .Select(l => l.LineItemGroupID)
                        .ToList();
                    dollarItems = dollarItems.Distinct().ToList();
                }
                if (dollarItems.Count > MAX_RESULT_LIMIT) {
                    ViewBag.SearchCriteria += "Too many Line Items to return complete results.";
                } else {
                    // For loop, use subset of dollarItems, 100 at a time
                    int startIndex = 0; // range selection starting index
                    int increment = INCREMENT_SIZE -1; // number of elements to select
                    List<EncumbranceLookup> dollarResults = new List<EncumbranceLookup>();
                    while (startIndex <= dollarItems.Count)
                    {
                        // tail cannot exceed dollarItems.Count
                        increment = (dollarItems.Count - startIndex < increment) ? dollarItems.Count - startIndex : increment;
                        List<int> theseItems = dollarItems.GetRange(startIndex, increment);
                        List<EncumbranceLookup> theseResults = _context.EncumbranceLookups
                            .AsNoTracking()
                            .Where(Utils.BuildOrExpression<EncumbranceLookup, int>(l => l.GroupID, theseItems.ToArray<int>()))
                            .ToList();
                        dollarResults.AddRange(theseResults);
                        startIndex += INCREMENT_SIZE;
                    }
                    resultsArray[arrayIndex] = dollarResults;
                    arrayIndex++;
                }
            }

            ViewBag.SearchParams = search;
            // AND all result sets together
            results = AndResultsTogether(resultsArray);
            results = results.Distinct().ToList();
            return View(results);
        }

        private List<EncumbranceLookup> AndResultsTogether(List<EncumbranceLookup>[] resultsArray)
        {
            List<EncumbranceLookup> results = new List<EncumbranceLookup>();
            foreach(List<EncumbranceLookup> r in resultsArray)
            {
                if(r != null)
                {
                    if(results.Count == 0){
                        results.AddRange(r);
                    }
                    else
                    {
                        var newResult = results.Intersect (r);
                        results = (List<EncumbranceLookup>)newResult.ToList();
                    }
                }
            }
            return results;
        }

        private string getSearchCriteria(SearchForm search)
        {
            // return a string describing the search criteria provided in the search
            string searchString = "";
            if(search.SearchContractNumber != null)
            {
                searchString += "Contract: '" + search.SearchContractNumber + "'<br />";
            }
            if(search.SearchCurrentStatus != null)
            {
                searchString += "Status: '" + search.SearchCurrentStatus + "'<br />";
            }
            if(search.SearchStartDate != null)
            {
                searchString += "Submitted after: " + String.Format("{0:MM/dd/yyyy}",search.SearchStartDate) + "<br />"; 
            }
            if(search.SearchEndDate != null)
            {
                searchString += "Submitted before: " + String.Format("{0:MM/dd/yyyy}",search.SearchEndDate) + "<br />";
            }

            // Dollar amount info is only relevant if a dollar amount is specified
            if (search.SearchMinAmount != null || search.SearchMinAmount != null)
            {
                if (search.IsContractAmount)
                {
                    searchString += "Contract Amount ";
                }
                if (search.IsEncumbranceAmount)
                {
                    if (search.IsContractAmount) { searchString += " or "; }
                    searchString += "Encumbrance Amount ";
                }
                if (search.IsLineItemAmount)
                {
                    if (search.IsContractAmount || search.IsEncumbranceAmount) { searchString += " or "; }
                    searchString += "Line Item Amount ";
                }
                if (search.SearchMinAmount != null)
                {
                    searchString += "Greater than " + Utils.FormatCurrency((decimal)search.SearchMinAmount) + "<br />";
                }
                if (search.SearchMaxAmount != null)
                {
                    searchString += "Less than " + Utils.FormatCurrency((decimal)search.SearchMaxAmount) + "<br />";
                }
            }
            // if search criteria are specified, prefix with a heading
            if(searchString.Length > 0)
            {
                searchString = "<strong>Search Criteria</strong><br />" + searchString;
            }
            return searchString;
        }
    }
}
