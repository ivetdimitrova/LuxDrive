using LuxDrive.Data;
using LuxDrive.Services;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace LuxDrive.Controllers
{
    [Authorize]
    public class FileController : BaseController
    {
        private readonly SpacesService _spacesService;
        private readonly IFileService fileService;

        public FileController(SpacesService spacesService, IFileService fileService)
        {
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
            string? userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            try
            {
                IEnumerable<IndexViewModel> allUserFiles = await this.fileService.GetUserFilesAsync(userIdStr);
                //var activeFiles = allUserFiles.Where(f => !f.IsDeleted).ToList();

                string planKey = GetUserKey("CurrentPlan");
                string currentPlan = Request.Cookies[planKey] ?? "Free";

                CalculateStorageUsage(allUserFiles, currentPlan);

                return View(allUserFiles);
            }
            catch (Exception)
            {
                return View(new List<IndexViewModel>());
            }

        }

        [HttpGet]
        public async Task<IActionResult> SharedWithMe()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            IEnumerable<IndexViewModel> sharedFiles = await this.fileService.GetSharedWithMeFilesAsync(userIdStr);
            IEnumerable<IndexViewModel> userFiles = await this.fileService.GetUserFilesAsync(userIdStr); // Да се изнесе

            string planKey = GetUserKey("CurrentPlan");
            string currentPlan = Request.Cookies[planKey] ?? "Free";

            CalculateStorageUsage(userFiles, currentPlan);

            return View("Index", sharedFiles);
        }


        [HttpGet]
        public async Task<IActionResult> Trash()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            var allUserFiles = await this.fileService.GetUserFilesAsync(userIdStr);
            string planKey = GetUserKey("CurrentPlan");
            string currentPlan = Request.Cookies[planKey] ?? "Free";
            CalculateStorageUsage(allUserFiles, currentPlan);

            IEnumerable<TrashViewModel>? trashedFiles = await this.fileService.GetTrashedFilesAsync(userIdStr);

            return View(trashedFiles);
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

            long currentUsedBytes = await this.fileService.GetTotalUsedStorageAsync(userIdStr);
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
                if (string.IsNullOrEmpty(extension))
                {
                    extension = Path.GetExtension(file.FileName);
                }

                var key = $"{userIdStr}/{fileId}{extension}";

                try
                {
                    using var stream = file.OpenReadStream();
                    var url = await _spacesService.UploadAsync(stream, key, file.ContentType);

                    await this.fileService.UpdateFileUrlAsync(fileId, url);
                }
                catch (Exception)
                {
                    continue;
                }

            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Rename(Guid id, string newName)
        {
            try
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
            catch (Exception)
            {
                return StatusCode(500, "An internal error occurred.");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                bool isDeleted = await fileService.DeleteUserFileAsync(id, userIdStr);

                if (!isDeleted)
                {
                    TempData["AlertMessage"] = "The file was not found or you do not have access to it.";

                }


            }
            catch (Exception)
            {
                TempData["AlertMessage"] = "An unexpected error occurred while attempting to delete!";
            }

            TempData.Keep("AlertMessage");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Restore(Guid id)
        {
            try
            {
                var userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                bool isRestored = await fileService.RestoreUserFileAsync(id, userIdStr);

                if (!isRestored)
                {
                    TempData["AlertMessage"] = "File not found or access denied";
                }

            }
            catch (Exception)
            {
                TempData["AlertMessage"] = "File not found or access denied";
            }


            return RedirectToAction(nameof(Trash));
        }

        [HttpPost]
        public async Task<IActionResult> PermanentDelete(Guid id)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            string? fileStorageUrl = await this.fileService.PermanentDeleteFileAsync(id, userIdStr);

            if (!string.IsNullOrEmpty(fileStorageUrl))
            {
                try
                {

                    var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                    var key = fileStorageUrl.Replace(endpoint, string.Empty);
                    await _spacesService.DeleteAsync(key);

                }
                catch { }


            }

            return RedirectToAction(nameof(Trash));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<Guid> ids)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();


            bool areDeleted = await this.fileService.DeleteMultipleFilesAsync(ids, userIdStr);

            if (!areDeleted)
            {
                return NotFound("No files were found or they are already deleted.");
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ShareMultiple(Guid receiverId, [FromBody] List<Guid> fileIds)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            try
            {
                bool areShared = await this.fileService.ShareMultipleFilesAsync(fileIds, userIdStr, receiverId);

                if (!areShared)
                {
                    return BadRequest("Could not share files. They might be missing or you don't have permission.");
                }

                return Ok("Files shared successfully.");
            }
            catch { return BadRequest("Error sharing files."); }
        }
        [HttpPost]
        public async Task<IActionResult> Share(Guid fileId, Guid receiverId)
        {
            var userIdStr = base.GetUserId();
            if (userIdStr == null) return BadRequest();

            try
            {
                await fileService.ShareFileAsync(fileId, userIdStr, receiverId);

                TempData["AlertMessage"] = "File was shared.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return BadRequest("Error sharing file!");
            }
        }


        //TODO
        private void CalculateStorageUsage(IEnumerable<IndexViewModel> files, string planName)
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();


            List<string>? filesUrls = await fileService.EmptyTrashAsync(userIdStr);

            if (filesUrls != null)
            {
                return BadRequest("No valid files were selected for sharing.");
            }


            foreach (var url in filesUrls)
            {
                try
                {

                    var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                    var key = url.Replace(endpoint, string.Empty);
                    await _spacesService.DeleteAsync(key);

                }
                catch
                {
                    return BadRequest("No valid files were selected for sharing.");
                }
            }


            TempData["SuccessMessage"] = "Trash emptied successfully!";
            return RedirectToAction(nameof(Trash));
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
        [HttpPost]
        public async Task<IActionResult> RestoreMultiple(List<Guid> ids)
        {
            var userId = GetUserId();
            if (ids == null || !ids.Any()) return BadRequest("No files selected.");

            bool isRestored = await this.fileService.RestoreMultipleFilesAsync(ids, userId);

            if (isRestored)
            {
                return BadRequest("Problem restoring files!");
            }
            return RedirectToAction(nameof(Trash));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiplePermanent(List<Guid> ids)
        {
            var userId = GetUserId();
            if (ids == null || !ids.Any()) return BadRequest("No files selected.");


            List<string>? filesUrls = await fileService.EmptyTrashAsync(userId);

            if (filesUrls == null)
            {
                return BadRequest("No valid files were selected for sharing.");
            }


            foreach (var url in filesUrls)
            {
                try
                {

                    var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/";
                    var key = url.Replace(endpoint, string.Empty);
                    await _spacesService.DeleteAsync(key);

                }
                catch
                {
                    return BadRequest("No valid files were selected for sharing.");
                }
            }


            TempData["SuccessMessage"] = "Files was delete successfully!";
            return RedirectToAction(nameof(Trash));
        }
        [HttpGet]
        public async Task<IActionResult> Download(Guid id)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            DownloadFileViewModel? file = await fileService.GetFileToDownloadAsync(id,userIdStr);

            if (file == null) return NotFound();

            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(file.StorageUrl);
                if (!response.IsSuccessStatusCode) return BadRequest("File not found in storage.");

                var stream = await response.Content.ReadAsStreamAsync();

                return File(stream, "application/octet-stream", file.Name + file.Extension);
            }
            catch
            {
                return BadRequest("Connection to storage failed.");
            }
        }
        [HttpPost]
        public async Task<IActionResult> DownloadMultiple([FromBody] List<Guid> ids)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            List<DownloadFileViewModel>? files = await this.fileService.GetMultipleFilesToDownloadAsync(ids, userIdStr);

            if (files == null) return NotFound();

            using var ms = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                using var httpClient = new HttpClient();

                foreach (var file in files)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(file.StorageUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var fileStream = await response.Content.ReadAsStreamAsync();

                            var entry = archive.CreateEntry(file.Name + file.Extension);
                            using var entryStream = entry.Open();
                            await fileStream.CopyToAsync(entryStream);
                        }
                    }
                    catch { }
                }
            }

            ms.Position = 0;
            return File(ms.ToArray(), "application/zip", "LuxDrive_Download.zip");
        }


    }
}