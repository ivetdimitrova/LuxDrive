using LuxDrive.Data;
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

        public async Task<bool> ChangeFileNameAsync(Guid userId, Guid fileId, string newName)
        {
            Data.Models.File? file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

            if (file == null)
                return false;

            string clean = newName.Trim();

            if (!string.IsNullOrEmpty(file.Extension) &&
                clean.EndsWith(file.Extension, StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[..^file.Extension.Length];
            }
            else
            {
                var dotIndex = clean.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    clean = clean.Substring(0, dotIndex);
                }
            }

            file.Name = clean;
            int changes = await _dbContext.SaveChangesAsync();

            if (changes == 1)
            {
                return true;
            }
            else
            {
                return false;
            }

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

        public async Task<Data.Models.File?> GetUserFileAsync(Guid fileId, Guid userId)
            =>await _dbContext.Files
                .Where(f => f.Id == fileId && f.UserId == userId)
                .FirstOrDefaultAsync();


        public async Task<IEnumerable<Data.Models.File>> GetUserFilesAsync(Guid userId)
        => await _dbContext.Files
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .ToListAsync();

        public async Task<bool> RemoveFileAsync(Data.Models.File file)
        {
            _dbContext.Files.Remove(file);
            int changesCount = await _dbContext.SaveChangesAsync();

            if (changesCount == 1)
            {
                return true;
            }
            else
            {
                return false;
            }

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
