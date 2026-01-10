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

        public FileController(LuxDriveDbContext dbContext, SpacesService spacesService, IFileService fileService)
        {
            _dbContext = dbContext;
            _spacesService = spacesService;
            this.fileService = fileService;
        }

        private string GetUserKey(string baseKey)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return baseKey;
            string safeUserName = User.Identity.Name.Replace("@", "_").Replace(".", "_");
            return $"{baseKey}_{safeUserName}";
        }

        private long GetMaxBytesForPlan(string plan)
        {
            return plan switch
            {
                "Basic" => 50L * 1024 * 1024 * 1024,      
                "Pro" => 2048L * 1024 * 1024 * 1024,     
                "Enterprise" => 100000L * 1024 * 1024 * 1024, 
                _ => 10L * 1024 * 1024 * 1024            
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            IEnumerable<FileEntity> files = await this.fileService.GetUserFilesAsync(userIdStr);

            string planKey = GetUserKey("CurrentPlan");
            string currentPlan = Request.Cookies[planKey] ?? "Free";

            CalculateStorageUsage(files, currentPlan);

            return View(files);
        }

        [HttpGet]
        public async Task<IActionResult> SharedWithMe()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            IEnumerable<FileEntity> sharedFiles = await this.fileService.GetSharedWithMeFilesAsync(userIdStr);

            var userFiles = await this.fileService.GetUserFilesAsync(userIdStr);

            string planKey = GetUserKey("CurrentPlan");
            string currentPlan = Request.Cookies[planKey] ?? "Free";

            CalculateStorageUsage(userFiles, currentPlan);

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
                TempData["UploadError"] = "Select at least one file.";
                return RedirectToAction(nameof(Index));
            }

            string planKey = GetUserKey("CurrentPlan");
            string currentPlan = Request.Cookies[planKey] ?? "Free";
            long maxStorageBytes = GetMaxBytesForPlan(currentPlan);

            var userFiles = await this.fileService.GetUserFilesAsync(userIdStr);
            long currentUsedBytes = userFiles.Sum(f => f.Size);
            long newFilesBytes = files.Sum(f => f.Length);

            if (currentUsedBytes + newFilesBytes > maxStorageBytes)
            {
                TempData["UploadError"] = $"Not enough space! You are trying to upload {FormatBytes(newFilesBytes)}, but you have {FormatBytes(maxStorageBytes - currentUsedBytes)} left on your {currentPlan} plan.";
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
                return BadRequest("Name cannot be empty.");
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
                return BadRequest("Error sharing files.");
            }
        }

        private void CalculateStorageUsage(IEnumerable<FileEntity> files, string planName)
        {
            long totalUsedBytes = files.Sum(f => f.Size);
            long maxBytes = GetMaxBytesForPlan(planName); 

            double percent = 0;
            if (planName == "Enterprise")
            {
                percent = totalUsedBytes > 0 ? 1 : 0; 
            }
            else
            {
                percent = ((double)totalUsedBytes / maxBytes) * 100;
                if (percent > 100) percent = 100;
            }

            string totalLabel = FormatBytes(maxBytes);
            if (planName == "Enterprise") totalLabel = "Unlimited";

            string usedLabel = FormatBytes(totalUsedBytes);

            ViewBag.StoragePercent = (int)percent;
            ViewBag.StorageText = $"{usedLabel} / {totalLabel}";
        }

        private string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024 * 1024)
                return $"{(bytes / 1024.0 / 1024.0 / 1024.0 / 1024.0):F2} TB";

            if (bytes >= 1024 * 1024 * 1024) 
                return $"{(bytes / 1024.0 / 1024.0 / 1024.0):F2} GB";

            double mb = (bytes / 1024.0) / 1024.0;
            return $"{mb:F1} MB";
        }
    }
}