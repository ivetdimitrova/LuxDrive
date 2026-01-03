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

        public FileController(LuxDriveDbContext dbContext, SpacesService spacesService, IFileService fileService)
        {
            _dbContext = dbContext;
            _spacesService = spacesService;
            this.fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            IEnumerable<Data.Models.File> files = await this.fileService
                 .GetUserFilesAsync(Guid.Parse(userIdStr));

            return View(files);
        }

        [HttpGet]
        public async Task<IActionResult> SharedWithMe()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var sharedFiles = await _dbContext.SharedFiles
                .Where(sf => sf.ReceiverId == userId)
                .Include(sf => sf.File)
                .Select(sf => sf.File)
                .OrderByDescending(f => f.UploadAt)
                .ToListAsync();

            return View("Index", sharedFiles);
        }

        [HttpPost]
        public async Task<IActionResult> Share(Guid fileId, Guid receiverId)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();
            var senderId = Guid.Parse(userIdStr);

            try
            {
                await fileService.ShareFileAsync(fileId, senderId, receiverId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest("Грешка при споделяне: " + ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            if (files == null || files.Count == 0)
            {
                TempData["UploadError"] = "Избери поне един файл.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;

              
                Guid? fileId = await this.fileService.CreateFileAsync(userIdStr, file);
                if (fileId == null) continue;

               
                string? extension = await this.fileService.GetFileExtensionAsync(fileId);
                if (extension == null) continue;

                var userId = Guid.Parse(userIdStr);

                
                var key = $"{userId}/{fileId}{extension}";

                using var stream = file.OpenReadStream();
                var url = await _spacesService.UploadAsync(stream, key, file.ContentType);

               
                await this.fileService.UpdateFileUrlAsync(fileId, url);
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

            bool isRenamed = await this.fileService.ChangeFileNameAsync(Guid.Parse(userIdStr), fileId, newName);

            if (!isRenamed)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            var userId = Guid.Parse(userIdStr);

            var file = await fileService.GetUserFileAsync(id, userId);
            if (file == null) return NotFound();

            try
            {
                if (!string.IsNullOrEmpty(file.StorageUrl))
                {
                    var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                    var key = file.StorageUrl.Replace(endpoint, string.Empty);
                    await _spacesService.DeleteAsync(key);
                }

                bool isDeleted = await fileService.RemoveFileAsync(file);

                if (!isDeleted)
                {
                    return NotFound();
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            foreach (var id in ids)
            {
                var file = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
                if (file != null)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(file.StorageUrl))
                        {
                            var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                            var key = file.StorageUrl.Replace(endpoint, string.Empty);
                            await _spacesService.DeleteAsync(key);
                        }
                    }
                    catch { }
                    _dbContext.Files.Remove(file);
                }
            }
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ShareMultiple(Guid receiverId, [FromBody] List<Guid> fileIds)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();
            var senderId = Guid.Parse(userIdStr);

            try
            {
                foreach (var fileId in fileIds)
                {
                    try
                    {
                        await fileService.ShareFileAsync(fileId, senderId, receiverId);
                    }
                    catch
                    {
                        continue;
                    }
                }
                return Ok();
            }
            catch
            {
                return BadRequest("Грешка при споделяне.");
            }
        }
    }
}