using LuxDrive.Data;
using LuxDrive.Services;
using LuxDrive.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuxDrive.Controllers
{
    [Authorize]
    public class FileController : BaseController
    {
        private readonly LuxDriveDbContext _dbContext;
        private readonly SpacesService _spacesService;
        private readonly IFileService fileService;

        public FileController(LuxDriveDbContext dbContext ,SpacesService spacesService,IFileService fileService)
        {
            _spacesService = spacesService;
            this.fileService = fileService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null)
            {
                return Unauthorized();
            }

           IEnumerable<Data.Models.File> files = await this.fileService
                .GetUserFiles(Guid.Parse(userIdStr));

            return View(files);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null)
            {
                return Unauthorized();
            }

            if (files == null || files.Count == 0)
            {
                TempData["UploadError"] = "Избери поне един файл.";
                return RedirectToAction(nameof(Index));
            }

            var userId = Guid.Parse(userIdStr);

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    continue;
                }

                 Guid? fileId = await this.fileService.CreateFileAsync(userIdStr,file);
                if(fileId==null)
                {
                    return NotFound();
                }

                 string? extension = await this.fileService.GetFileExtensionAsync(fileId);

                if(extension==null)
                {
                    return NotFound();
                }

                    var key = $"{userId}/{fileId}{extension}";

                using var stream = file.OpenReadStream();
                var url = await _spacesService.UploadAsync(stream, key, file.ContentType);

               
                bool isUpdated = await this.fileService.UpdateFileUrlAsync(fileId, url);

            }

             return RedirectToAction(nameof(Index));
           
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(Guid fileId, string newName)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["UploadError"] = "Името не може да е празно.";
                return RedirectToAction(nameof(Index));
            }


            bool isRenamed = await this.fileService.ChangeFileName(Guid.Parse(userIdStr),fileId,newName);

            if (!isRenamed)
            {
                return NotFound();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // --------  Изтриване на файл --------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            var userId = Guid.Parse(userIdStr);

            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (file == null) return NotFound();

            try
            {
                if (!string.IsNullOrEmpty(file.StorageUrl))
                {
                    var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                    var key = file.StorageUrl.Replace(endpoint, string.Empty);
                    await _spacesService.DeleteAsync(key);
                }
            }
            catch
            {
                // Логване на грешка при нужда
            }

            _dbContext.Files.Remove(file);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}