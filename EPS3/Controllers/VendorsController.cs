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

namespace EPS3.Controllers
{
    public class VendorsController : Controller
    {
        private readonly EPSContext _context;

        public VendorsController(EPSContext context)
        {
            _context = context;
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

    }
}
