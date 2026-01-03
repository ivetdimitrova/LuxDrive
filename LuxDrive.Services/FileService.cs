using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FileEntity = LuxDrive.Data.Models.File;

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

            return changes == 1;
        }

        public async Task<Guid?> CreateFileAsync(string userId, IFormFile file)
        {
            if (!Guid.TryParse(userId, out Guid userIdGuid))
            {
                return null;
            }

            var newFile = new FileEntity
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
            => await _dbContext.Files
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

            return changesCount == 1;
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

            return await _dbContext.SaveChangesAsync() == 1;
        }

        // СПОДЕЛЯНЕ НА ФАЙЛ
        public async Task ShareFileAsync(Guid fileId, Guid senderId, Guid receiverId)
        {
            // 1. Проверка дали са приятели
            bool areFriends = await _dbContext.UserFriends
                .AnyAsync(x => x.UserId == senderId && x.FriendId == receiverId);

            if (!areFriends)
            {
                // Ако НЕ са приятели, хвърляме грешка и спираме дотук
                throw new InvalidOperationException("Users are not friends.");
            }

            // 2. Проверка дали файлът вече не е споделен (за да не се дублира)
            bool alreadyShared = await _dbContext.SharedFiles
                .AnyAsync(x => x.FileId == fileId && x.ReceiverId == receiverId);

            if (alreadyShared) return; // Ако вече е споделен, просто излизаме

            // 3. Създаване на записа
            var sharedFile = new SharedFile
            {
                FileId = fileId,
                SenderId = senderId,
                ReceiverId = receiverId,
                SharedOn = DateTime.UtcNow
            };

            _dbContext.SharedFiles.Add(sharedFile);
            await _dbContext.SaveChangesAsync();
        }
    }
}