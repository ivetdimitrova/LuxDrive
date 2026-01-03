using LuxDrive.Data;
using LuxDrive.Data.Models; // Трябва за SharedFile
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
            _spacesService = spacesService;
            this.fileService = fileService;
            _dbContext = dbContext;
        }

        // -------- Всички файлове (Index) --------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            var userId = Guid.Parse(userIdStr);

            var files = await _dbContext.Files
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.UploadAt)
                .ToListAsync();

            return View(files);
        }

        // -------- НОВО: Споделени с мен --------
        [HttpGet]
        public async Task<IActionResult> SharedWithMe()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            // Взимаме записите от SharedFiles, където ние сме Получател (Receiver)
            // И чрез .Select(sf => sf.File) взимаме директно файла
            var sharedFiles = await _dbContext.SharedFiles
                .Where(sf => sf.ReceiverId == userId)
                .Include(sf => sf.File) // Зареждаме данните за файла
                .Select(sf => sf.File)  // Избираме само обекта File
                .OrderByDescending(f => f.UploadAt)
                .ToListAsync();

           
            return View("Index", sharedFiles);
        }

        // -------- НОВО: Действие за споделяне (за JS) --------
        [HttpPost]
        public async Task<IActionResult> Share(Guid fileId, Guid receiverId)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();
            var senderId = Guid.Parse(userIdStr);

            try
            {

                // Проверка дали вече е споделен
                bool alreadyShared = await _dbContext.SharedFiles
                    .AnyAsync(sf => sf.FileId == fileId && sf.ReceiverId == receiverId);

                if (!alreadyShared)
                {
                    var share = new SharedFile
                    {
                        FileId = fileId,
                        SenderId = senderId,
                        ReceiverId = receiverId,
                        SharedOn = DateTime.UtcNow
                    };

                    _dbContext.SharedFiles.Add(share);
                    await _dbContext.SaveChangesAsync();
                }

                return Ok();
            }
            catch
            {
                return BadRequest("Грешка при споделяне.");
            }
        }

        // -------- Качване (Upload) --------
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

        // -------- Преименуване (Rename) --------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(Guid id, string newName)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["UploadError"] = "Името не може да е празно.";
                return RedirectToAction(nameof(Index));
            }

            var userId = Guid.Parse(userIdStr);

            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (file == null) return NotFound();

            var clean = newName.Trim();

            // Логика за запазване на разширението
            if (!string.IsNullOrEmpty(file.Extension) && !clean.EndsWith(file.Extension, StringComparison.OrdinalIgnoreCase))
            {
                
            }

            file.Name = clean;
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // -------- Изтриване (Delete) --------
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
            catch { }

            _dbContext.Files.Remove(file);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // -------- НОВО: Масово изтриване --------
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
                            var endpoint = "https://luxdrive.ams3.digitaloceanspaces.com/"; // Твоят endpoint
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

        // -------- НОВО: Масово споделяне --------
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
                    // Проверка за всеки файл дали вече е споделен
                    bool alreadyShared = await _dbContext.SharedFiles
                        .AnyAsync(sf => sf.FileId == fileId && sf.ReceiverId == receiverId);

                    if (!alreadyShared)
                    {
                        var share = new SharedFile
                        {
                            FileId = fileId,
                            SenderId = senderId,
                            ReceiverId = receiverId,
                            SharedOn = DateTime.UtcNow
                        };
                        _dbContext.SharedFiles.Add(share);
                    }
                }
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch
            {
                return BadRequest("Грешка при споделяне.");
            }
        }
    }
}