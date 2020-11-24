using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EPS3.DataContexts;
using EPS3.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.Extensions.Logging;

namespace EPS3.Controllers
{
    public class UserFilesController : Controller
    {
        private readonly EPSContext _context;
        private readonly ILogger<UserFilesController> _logger;

        public UserFilesController(IHostingEnvironment appEnvironment, EPSContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _logger = loggerFactory.CreateLogger<UserFilesController>();
        }
        public IActionResult GetFile(String fileNameOrId)
        {
            //is it a number? 
            int attachmentId;
            if (Int32.TryParse(fileNameOrId, out attachmentId))
            {
                return GetFileById(attachmentId);
            }
            //does it start with a number and underscore? If so, just tease out that number; that gets around URLs already out there in emails that have invalid characters, particularly #
            //good old RegEx
            var rx = new Regex(@"^(\d*)_"); 
            if (rx.IsMatch(fileNameOrId))
            {
                var matches = rx.Matches(fileNameOrId);
                if (matches.Any())
                {
                    var potentialId = matches[0].Groups[1].Value;
                    if (Int32.TryParse(potentialId, out attachmentId))
                    {
                        return GetFileById(attachmentId);
                    }
                }
            }

            //punt
            return GetFileByName(fileNameOrId, fileNameOrId);
        }

        public ActionResult GetFileById(int AttachmentID)
        {
            //get the filename
            var fileAttachment = _context.FileAttachments.Find(AttachmentID);
            if (fileAttachment == null)
            {
                return NotFound("No record of file attachment with ID " + AttachmentID);
            }
            return GetFileByName(fileAttachment.FileName, fileAttachment.DisplayName);
        }

        public ActionResult GetFileByName(string FileName, string DisplayName) { 
            var fullName = AppSettingsJson.UserFilesPhysicalPath() + FileName;


            if (System.IO.File.Exists(fullName))
            {

                FileStream fs = System.IO.File.OpenRead(fullName);
                byte[] data = new byte[fs.Length];
                int br = fs.Read(data, 0, data.Length);
                if (br != fs.Length)
                {
                    throw new IOException("Unable to completely read file.");
                }

                //get MIME type; tried using methods in Microsoft.AspNetCore.StaticFiles; but can't install that package without a bunch of other conflicting dependencies
                string mimeType = "application/octet-stream"; //default; not recommended to use this generic, but no other extensions have been used to date beyond what's listed in the switch below
                string extension = System.IO.Path.GetExtension(FileName);
                switch (extension)
                {
                    case ".doc":
                        mimeType = "application/msword";
                        break;
                    case ".docx":
                        mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        break;
                    case ".pdf":
                        mimeType = "application/pdf";
                        break;
                    case ".xls":
                        mimeType = "application/vnd.ms-excel";
                        break;
                    case ".xlsm":
                        mimeType = "application/vnd.ms-excel.sheet.macroEnabled.12";
                        break;
                    case ".xlsx":
                        mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;
                }
                return File(data, mimeType, DisplayName);
            }
            else
            {
                return NotFound("File not found: " + FileName);
            }
        }

    }
}
