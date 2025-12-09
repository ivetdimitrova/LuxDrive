using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuxDrive.Controllers
{
    [Authorize]
    public class FileController : BaseController
    {
        private readonly LuxDriveDbContext _dbContext;
        private readonly SpacesService _spacesService;

        public FileController(LuxDriveDbContext dbContext, SpacesService spacesService)
        {
            _dbContext = dbContext;
            _spacesService = spacesService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = GetUserId();
            if (userIdStr == null)
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdStr);

            var files = await _dbContext.Files
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.UploadAt)
                .ToListAsync();

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

                var extension = Path.GetExtension(file.FileName);
                var key = $"{userId}/{Guid.NewGuid()}{extension}";

                using var stream = file.OpenReadStream();
                var url = await _spacesService.UploadAsync(stream, key, file.ContentType);

                var entity = new LuxDrive.Data.Models.File
                {
                    Id = Guid.NewGuid(),
                    Name = Path.GetFileNameWithoutExtension(file.FileName),
                    Extension = extension,
                    Size = file.Length,
                    StorageUrl = url,
                    UploadAt = DateTime.UtcNow,
                    UserId = userId
                };

                _dbContext.Files.Add(entity);
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // --------  Преименуване на файл --------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(Guid id, string newName)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["UploadError"] = "Името не може да е празно.";
                return RedirectToAction(nameof(Index));
            }

            var userId = Guid.Parse(userIdStr);

            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (file == null)
            {
                return NotFound();
            }

            var clean = newName.Trim();

            // ако потребителят е написал и разширение – махаме го
            if (!string.IsNullOrEmpty(file.Extension) &&
                clean.EndsWith(file.Extension, StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[..^file.Extension.Length];
            }
            else
            {
                // ако има някаква друга точка – режем след последната
                var dotIndex = clean.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    clean = clean.Substring(0, dotIndex);
                }
            }

            file.Name = clean;

            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // --------  Изтриване на файл --------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = GetUserId();
            if (userIdStr == null)
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdStr);

            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (file == null)
            {
                return NotFound();
            }

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
            }

            _dbContext.Files.Remove(file);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
