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
using EPS3.Helpers;
using EPS3.ViewModels;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EPS3.Controllers
{
    public class LegacyContractsController : Controller
    {
        private readonly LegacyContext _legacycontext;
        private readonly ILogger<LegacyContractsController> _logger;

        public LegacyContractsController(LegacyContext context, ILoggerFactory loggerFactory)
        {
            _legacycontext = context;
            _logger = loggerFactory.CreateLogger<LegacyContractsController>();
        }
        // GET: Contracts
        public async Task<IActionResult> Index()
        {
            //var contracts = _legacycontext.LegacyContracts
            //    .Include(c => c.Financials)
            //    .Include(c => c.Info);
            var contracts = _legacycontext.LegacyContracts;
            return View(await contracts.ToListAsync());
        }
    }
}