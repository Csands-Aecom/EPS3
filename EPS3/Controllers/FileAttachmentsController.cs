using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using EPS3.Models;
using EPS3.DataContexts;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;

namespace EPS3.Controllers
{
    public class FileAttachmentsController : Controller
    {
        private readonly IHostingEnvironment _appEnvironment;
        private readonly EPSContext _context;
        private readonly ILogger<FileAttachmentsController> _logger;
        private const string USER_FILE_DIR = "\\UserFiles\\"; //TODO: move to appsettings.json. 
        private string USER_FILE_PATH;
        public FileAttachmentsController(IHostingEnvironment appEnvironment, EPSContext context, ILoggerFactory loggerFactory)
        {
            _appEnvironment = appEnvironment;
            USER_FILE_PATH = _appEnvironment.WebRootPath + USER_FILE_DIR;
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
                        using (var stream = new FileStream(fileAttachment.FileName, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        
                    }
                    _context.Add(fileAttachment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Edit","LineItem");
                }
                catch (Exception e)
                {
                    _logger.LogError("FileAttachmentsController.Add Error:" + e.GetBaseException());
                    Log.Error("FileAttachmentsController.Add Error:" + e.GetBaseException() + "\n" + e.StackTrace);
                }
            }
            return View();
        }

        [HttpGet]
        public IActionResult UploadFile()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile FileToUpload)
        {
            if(FileToUpload == null || FileToUpload.Length == 0) { return Content("File not selected.");  }
            string filePath = USER_FILE_PATH + FileToUpload.FileName;
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await FileToUpload.CopyToAsync(stream);
            }

            ViewData["FilePath"] = filePath;
            return View();
        }

        
        //[HttpPost]
        //public async Task<IActionResult> AttachFile(FileAttachment fileAttachment)
        //{
        //    IFormFile FileToUpload = (IFormFile)fileAttachment.Attachment;
        //    fileAttachment.FileDate = DateTime.Now;
        //    fileAttachment.FileName = FileToUpload.FileName;
        //    if (!FilenameIsUnique(FileName))
        //    {

        //    }
        //    if(FileToUpload == null || FileToUpload.Length == 0) { return Content("File not selected.");  }
        //    string pathRoot = _appEnvironment.WebRootPath;
        //    string filePath = pathRoot + USER_FILE_PATH + FileToUpload.FileName;
        //    using (var stream = new FileStream(filePath, FileMode.Create))
        //    {
        //        await FileToUpload.CopyToAsync(stream);
        //    }

        //    ViewData["FilePath"] = filePath;
        //    return View();
        //}

        //private bool FilenameIsUnique(filename)
        //{
        //    //return false if file already exists in file directory
        //    foreach(File file in files)
        //    {
        //        if (file.FileName.equals(filename))
        //        {
        //            return false;
        //        }
        //    }

        //    //return true if it does not exist in file directory
        //    return true;
        //}
    }
}
