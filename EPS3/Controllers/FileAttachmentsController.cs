using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
        public async Task<IActionResult> UploadFile(IFormFile FileToUpload, int fileGroupID)
        {
            if (FileToUpload == null || FileToUpload.Length == 0) {
                //return RedirectToAction("Manage", "LineItemGroups", new { id = fileGroupID }); 
                return Json("\"error\" : \"No file uploaded\"");
            }
            string fileName = Path.GetFileName(FileToUpload.FileName);
            try { 
                // 1. Save the FileAttachment record
                FileAttachment fileAttachment = new FileAttachment()
                {
                    FileDate = DateTime.Now,
                    FileName = fileName,
                    DisplayName = fileName,
                    GroupID = fileGroupID
                };
                _context.FileAttachments.Add(fileAttachment);
                _context.SaveChanges();

                // 2. Update the FileName to include the Attachment ID
                // This is to make the filename in the UserFiles directory unique
                // This will prevent overwrites by files with the same name
                // The DisplayName will remain identical to what the user uploaded, so it can duplicate without conflict
                fileAttachment.FileName = fileAttachment.AttachmentID + "_" + fileAttachment.FileName;
                _context.FileAttachments.Update(fileAttachment);
                _context.SaveChanges();

                // 3. Save the file to the UserFiles directory, using the FileName, not the DisplayName
                string filePath = USER_FILE_PATH + fileAttachment.FileName;
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await FileToUpload.CopyToAsync(stream);
                }
                // Assert: DisplayName = Filename.pdf, FileName = 123_Filename.pdf, file is stored in /UserFiles/123_Filename.pdf
                string displayPath = USER_FILE_PATH + fileAttachment.DisplayName;
                ViewData["FilePath"] = filePath;
                ViewData["DisplayPath"] = displayPath;

                // Add to ViewBag a list of all files associated with this encumbrance request
                List<FileAttachment> files = _context.FileAttachments.Where(f => f.GroupID == fileGroupID).ToList();
                ViewBag.Files = files;
                //return RedirectToAction("Manage", "LineItemGroups", new { id = fileGroupID });
                string returnString = "{\"fileID\" : \"" + fileAttachment.AttachmentID + "\", \"fileName\" : \"" + fileAttachment.DisplayName + "\", \"fileURL\" : \"\\\\UserFiles\\\\" + fileAttachment.FileName + "\"}";
                return (Json(returnString));

            }
	        catch(Exception e)
            {
                //Console.WriteLine(e.Message);
                //TODO: If the file wasn't deleted, restore the FileAttachment record
                //return Json("{'error': 'true', 'message' : 'The attachment was not saved.' }");
                //return RedirectToAction("Manage", "LineItemGroups", new { id = fileGroupID });
                return Json("\"error\" : \"Could not upload file.\"");
            }
        }

       
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                //Delete the FileAttachment Record
                FileAttachment fileAttachment = _context.FileAttachments.SingleOrDefault(f => f.AttachmentID == id);
                string fileName = fileAttachment.DisplayName;
                _context.FileAttachments.Remove(fileAttachment);
                _context.SaveChanges();

                //Remove the file from UserFiles directory
                DeleteFile(USER_FILE_PATH + fileAttachment.FileName);

                return (new JsonResult("{\"fileName\" : \"" + fileName + "\"}"));
            }catch(Exception e)
            {
                return (new JsonResult("result: error"));
            }
        }

        private bool DeleteFile(string fileName)
        {
            try
            {
                System.IO.File.Delete(fileName);
                return true;
            }
            catch (System.IO.IOException e)//C:\Users\chris_sands\source\repos\EPS3\EPS3\wwwroot\UserFiles
            {
                Console.WriteLine(e.Message);
                return false;
            }
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
