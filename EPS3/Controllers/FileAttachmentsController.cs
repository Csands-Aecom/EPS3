using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EPS3.Models;
using EPS3.DataContexts;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace EPS3.Controllers
{
    public class FileAttachmentsController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<FileAttachmentsController> _logger;

        public FileAttachmentsController(EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<FileAttachmentsController>();
        }

        [HttpGet]
        public IActionResult Add(int id)
        {

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Add([Bind("lineItemID, fileName")] FileAttachment fileAttachment, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (file.Length > 0)
                    {
                        /*using (var stream = new FileStream(fileAttachment.FileName, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        */
                    }
                    _context.Add(fileAttachment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Edit","LineItem");
                }
                catch (Exception e)
                {
                    _logger.LogError("ContractsController.Create Error:" + e.GetBaseException());
                    Log.Error("ContractsController.Create Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
            }
            return View();
        }
    }
}
