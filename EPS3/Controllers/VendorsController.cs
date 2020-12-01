using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EPS3.DataContexts;
using EPS3.Models;
using EPS3.ViewModels;
using Newtonsoft.Json;
using System.Security.Principal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EPS3.Controllers
{
    public class VendorsController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<VendorsController> _logger;

        public VendorsController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<VendorsController>();
        }
        public IActionResult Create()
        {
            ViewData["Vendors"] = _context.Vendors.OrderBy(v => v.VendorName);
            return View();
        }

        // POST: Contracts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VendorCode, VendorName")] Vendor vendor, int ContractID)
        {
            if (ModelState.IsValid)
            {
                var oldVendor = _context.Vendors.Where(v => v.VendorCode == vendor.VendorCode);
                // if the vendorcode does not already exist, add the new vendor
                if (oldVendor != null)
                {
                    vendor.VendorCode = vendor.VendorCode.ToUpper();
                    _context.Add(vendor);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Edit", "Contracts", new { id = ContractID });
        }

        public JsonResult AddNewVendor(Vendor vendor)
        {
            if(!String.IsNullOrWhiteSpace(vendor.VendorCode) && !String.IsNullOrWhiteSpace(vendor.VendorName)) {
                if (VendorExists(vendor.VendorCode))
                {
                    vendor = _context.Vendors.SingleOrDefault(v => v.VendorCode == vendor.VendorCode);
                } else {
                    vendor.VendorName = vendor.VendorName.ToUpper();
                    vendor.VendorCode = vendor.VendorCode.ToUpper();
                    _context.Vendors.Add(vendor);
                    _context.SaveChanges();
                }
            } else {
                throw new Exception("Missing vendor code or name");
            }
            return Json(vendor); 
        }

        public JsonResult UpdateVendor(string vendor)
        {
            Vendor theVendor = JsonConvert.DeserializeObject<Vendor>(vendor);
            theVendor.VendorCode = theVendor.VendorCode.ToUpper();
            if ((theVendor.VendorCode != null && theVendor.VendorCode.Length > 0) && (theVendor.VendorName != null && theVendor.VendorName.Length > 0))
            {
                if (VendorExists(theVendor.VendorID))
                {
                    Vendor thisVendor = _context.Vendors.SingleOrDefault(v => v.VendorID == theVendor.VendorID);
                    thisVendor.VendorName = theVendor.VendorName;
                    thisVendor.VendorCode = theVendor.VendorCode;
                    _context.Vendors.Update(thisVendor);
                    _context.SaveChanges();
                    theVendor = thisVendor;
                }
                else
                {
                    return (Json("{\"success\": \"false\"}"));
                }
            }
            else
            {
                return (Json("{\"success\": \"false\"}"));
            }
            string success = "{\"success\": \"true\", \"VendorName\" : \"" + theVendor.VendorName + "\", \"VendorID\" : \"" + theVendor.VendorID + "\", \"VendorCode\": \"" + theVendor.VendorCode + "\"}";
            return Json(success);
        }

        public bool VendorExists(int vendorID)
        {
            int vendorCount = _context.Vendors.Count(v => v.VendorID == vendorID);
            return (vendorCount > 0);
        }
        public bool VendorExists(string vendorCode)
        {
            int vendorCount = _context.Vendors.Count(v => v.VendorCode == vendorCode);
            return (vendorCount > 0);
        }
        // GET: Users/Edit/5
        public IActionResult Edit(int? id)
        {
            if(id != null && id > 0)
            { 
                Vendor vendor = (Vendor) _context.Vendors
                .SingleOrDefault(v => v.VendorID == id);
                return View(vendor);
             }
            else
            {
                return RedirectToAction("Index", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public  IActionResult Edit(int id, [Bind("VendorID, VendorCode, VendorName")] Vendor vendor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(vendor).State = EntityState.Modified;
                    _context.SaveChanges();
                    ViewBag.Message = "Vendor " + vendor.VendorName + " successfully updated.";
                    return RedirectToAction("Index", "Users");
                }
                catch (Exception e)
                {
                    _logger.LogError("LineItemsController.Edit Error:" + e.GetBaseException());
                    Log.Error("LineItemsController.Edit  Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                    ViewBag.Message = "No Vendor updated.";
                    return RedirectToAction("Index", "Users");
                }
            }
            else
            {
                ViewBag.Message = "No Vendor updated.";
                return RedirectToAction("Index", "Users");
            }

        }
    }
}
