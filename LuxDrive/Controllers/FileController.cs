using LuxDrive.Data;
using FileEntity = LuxDrive.Data.Models.File;
using LuxDrive.Services;
using LuxDrive.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LuxDrive.Controllers
{
    [Authorize]
    public class FileController : BaseController
    {
        private readonly LuxDriveDbContext _dbContext;
        private readonly SpacesService _spacesService;
        private readonly IFileService fileService;

        private const long MaxStorageBytes = 10L * 1024 * 1024 * 1024;

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

            IEnumerable<FileEntity> files = await this.fileService.GetUserFilesAsync(userIdStr);

            CalculateStorageUsage(files);

            return View(files);
        }

        [HttpGet]
        public async Task<IActionResult> SharedWithMe()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            IEnumerable<FileEntity> sharedFiles = await this.fileService.GetSharedWithMeFilesAsync(userIdStr);

            var userFiles = await this.fileService.GetUserFilesAsync(userIdStr);
            CalculateStorageUsage(userFiles);

            return View("Index", sharedFiles);
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

            var userFiles = await this.fileService.GetUserFilesAsync(userIdStr);
            long currentUsedBytes = userFiles.Sum(f => f.Size);
            long newFilesBytes = files.Sum(f => f.Length);

            if (currentUsedBytes + newFilesBytes > MaxStorageBytes)
            {
                TempData["UploadError"] = $"Нямате достатъчно място! Опитвате се да качите {FormatBytes(newFilesBytes)}, а разполагате с {FormatBytes(MaxStorageBytes - currentUsedBytes)}.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;

                Guid? fileId = await this.fileService.CreateFileAsync(userIdStr, file);
                if (fileId == null) continue;

                string? extension = await this.fileService.GetFileExtensionAsync(fileId);
                if (extension == null) continue;

                var key = $"{userIdStr}/{fileId}{extension}";

                using var stream = file.OpenReadStream();
                var url = await _spacesService.UploadAsync(stream, key, file.ContentType);

                await this.fileService.UpdateFileUrlAsync(fileId, url);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Rename(Guid id, string newName)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(newName))
            {
                return BadRequest("Името не може да е празно.");
            }

            bool isRenamed = await this.fileService.ChangeFileNameAsync(userIdStr, id, newName);

            if (!isRenamed) return NotFound();

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            FileEntity? file = await fileService.GetUserFileAsync(id, userIdStr);
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

                if (!isDeleted) return NotFound();

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

            if (!Guid.TryParse(userIdStr, out Guid userGuid)) return Unauthorized();

            foreach (var id in ids)
            {
                var file = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userGuid);
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
        public async Task<IActionResult> ShareMultiple(string receiverId, [FromBody] List<Guid> fileIds)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            try
            {
                foreach (var fileId in fileIds)
                {
                    try
                    {
                        await fileService.ShareFileAsync(fileId, userIdStr, receiverId);
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

        private void CalculateStorageUsage(IEnumerable<FileEntity> files)
        {
            long totalUsedBytes = files.Sum(f => f.Size);
            double percent = ((double)totalUsedBytes / MaxStorageBytes) * 100;

            if (percent > 100) percent = 100;

            ViewBag.StoragePercent = (int)percent;
            ViewBag.StorageText = $"{FormatBytes(totalUsedBytes)} / 10 GB";
        }

        private string FormatBytes(long bytes)
        {
            double mb = (bytes / 1024.0) / 1024.0;
            if (mb < 1024) return $"{mb:F1} MB";
            double gb = mb / 1024.0;
            return $"{gb:F2} GB";
        }
    }
}