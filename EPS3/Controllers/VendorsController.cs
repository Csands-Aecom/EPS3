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
        private object loggerFactory;

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

        public JsonResult AddNewVendor(string vendor)
        {
            Vendor newVendor = JsonConvert.DeserializeObject<Vendor>(vendor);
            newVendor.VendorCode = newVendor.VendorCode.ToUpper();
            if((newVendor.VendorCode != null && newVendor.VendorCode.Length > 0) && (newVendor.VendorName != null && newVendor.VendorName.Length > 0)) {
                if (VendorExists(newVendor.VendorCode))
                {
                    newVendor = _context.Vendors.SingleOrDefault(v => v.VendorCode == newVendor.VendorCode);
                }else
                {
                    _context.Vendors.Add(newVendor);
                    _context.SaveChanges();
                }
            }
            else
            {
                return (Json("{\"success\": \"false\"}"));
            }
            string success = "{\"success\": \"true\", \"VendorName\" : \"" + newVendor.VendorName + "\", \"VendorID\" : \"" + newVendor.VendorID + "\", \"VendorCode\": \"" + newVendor.VendorCode + "\"}";
            return Json(success);
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
