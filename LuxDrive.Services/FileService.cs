using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LuxDrive.Services
{
    public class FileService : IFileService
    {
        private readonly LuxDriveDbContext _dbContext;

        public FileService(LuxDriveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid?> CreateFileAsync(string userId, IFormFile file)
        {
            bool isValidUserId = Guid.TryParse(userId, out Guid userIdGuid);

            if (!isValidUserId)
            {
                return null;
            }

            Guid fileId = Guid.NewGuid();

            var newFile = new LuxDrive.Data.Models.File
            {
                Id = Guid.NewGuid(),
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                Extension = Path.GetExtension(file.FileName),
                Size = file.Length,
                StorageUrl = "",
                UploadAt = DateTime.UtcNow,
                UserId = userIdGuid
            };

            await _dbContext.Files.AddAsync(newFile);
            await _dbContext.SaveChangesAsync();

            return newFile.Id;
        }

        public async Task<string?> GetFileExtensionAsync(Guid? fileId)
        {

            return await _dbContext.Files
                .AsNoTracking()
                .Where(f => f.Id == fileId)
                .Select(f => f.Extension)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateFileUrlAsync(Guid? fileId, string url)
        {
            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null)
            {
                return false;
            }

            file.StorageUrl = url;

            _dbContext.Update(file);

            int modified = await _dbContext.SaveChangesAsync();
            if (modified == 1)
            {
                return true;
            }
            else
            { 
                return false; 
            }
        }
    }
}
