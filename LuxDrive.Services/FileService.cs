using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.File;
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

        public async Task<bool> ChangeFileNameAsync(string userId, Guid fileId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName) || !Guid.TryParse(userId, out Guid userGuid))
                return false;


            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userGuid);

            if (file == null) return false;

            string clean = newName.Trim();

            if (string.IsNullOrEmpty(clean) || string.IsNullOrWhiteSpace(clean)) return false;

            if (!string.IsNullOrEmpty(file.Extension) &&
                clean.EndsWith(file.Extension, StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[..^file.Extension.Length];
            }
            else
            {
                var dotIndex = clean.LastIndexOf('.');
                if (dotIndex > 0) clean = clean.Substring(0, dotIndex);
            }

            if (file.Name == clean) return true;

            file.Name = clean;
            return await _dbContext.SaveChangesAsync() == 1;
        }

        public async Task<Guid?> CreateFileAsync(string userId, IFormFile file)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) return null;

            if (file == null || string.IsNullOrWhiteSpace(file.FileName))
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
                UserId = userGuid
            };

            await _dbContext.Files.AddAsync(newFile);
            await _dbContext.SaveChangesAsync();

            return newFile.Id;
        }

        public async Task<string?> GetFileExtensionAsync(Guid? fileId)
        {
            if (fileId == null || fileId == Guid.Empty)
            {
                return null;
            }

            return await _dbContext.Files
                .AsNoTracking()
                .Where(f => f.Id == fileId)
                .Select(f => f.Extension)
                .FirstOrDefaultAsync();
        }


        public async Task<IEnumerable<IndexViewModel>> GetUserFilesAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) return new List<IndexViewModel>();

            IEnumerable<IndexViewModel> files = await _dbContext.Files
                 .AsNoTracking()
                 .Where(f => f.UserId == userGuid)
                 .Select(f => new IndexViewModel
                 {
                     Id = f.Id,
                     Type = f.Extension,
                     StorageUrl = f.StorageUrl,
                     Name = f.Name,
                     Extension = f.Extension,
                     UploadedAt = f.UploadAt,
                     IsDeleted = f.IsDeleted,
                     Size = (int)f.Size

                 })
                 .ToListAsync();

            foreach (IndexViewModel file in files)
            {
                switch (file.Extension.ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".gif":
                    case ".webp":
                        file.Icon = "fas fa-image";
                        break;

                    case ".mp4":
                    case ".avi":
                    case ".mov":
                        file.Icon = "fas fa-play";
                        break;

                    case ".mp3":
                    case ".wav":
                        file.Icon = "fas fa-music";
                        break;

                    case ".pdf":
                        file.Icon = "fas fa-file-pdf";
                        break;

                    case ".doc":
                    case ".docx":
                        file.Icon = "fas fa-file-word";
                        break;

                    case ".xls":
                    case ".xlsx":
                        file.Icon = "fas fa-file-excel";
                        break;

                    case ".zip":
                    case ".rar":
                    case ".7z":
                        file.Icon = "fas fa-file-archive";
                        break;

                    default:
                        file.Icon = "fas fa-file";
                        break;
                }


            }
            return files;
        }

        public async Task<bool> RemoveFileAsync(FileEntity file)
        {
            _dbContext.Files.Remove(file);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateFileUrlAsync(Guid? fileId, string url)
        {
            var file = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null) return false;

            file.StorageUrl = url;
            _dbContext.Update(file);
            return await _dbContext.SaveChangesAsync() == 1;
        }

        public async Task ShareFileAsync(Guid fileId, string senderId, Guid receiverId)
        {
            if (!Guid.TryParse(senderId, out Guid senderGuid))
            {
                throw new ArgumentException("Invalid IDs.");
            }

            bool areFriends = await _dbContext.UserFriends
                .AnyAsync(x => x.UserId == senderGuid && x.FriendId == receiverId);

            if (!areFriends) throw new InvalidOperationException("Users are not friends.");

            bool alreadyShared = await _dbContext.SharedFiles
                .AnyAsync(x => x.FileId == fileId && x.ReceiverId == receiverId);

            if (alreadyShared) return;

            var sharedFile = new SharedFile
            {
                FileId = fileId,
                SenderId = senderGuid,
                ReceiverId = receiverId,
                SharedOn = DateTime.UtcNow
            };

            _dbContext.SharedFiles.Add(sharedFile);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<IndexViewModel>> GetSharedWithMeFilesAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) return new List<IndexViewModel>();

            return await _dbContext.SharedFiles
                .Where(sf => sf.ReceiverId == userGuid)
                .Include(sf => sf.File)
                .Include(sf => sf.Sender)
                .AsNoTracking()
                .Select(sf => new IndexViewModel
                {
                    Id = sf.File.Id,
                    Name = sf.File.Name,
                    Extension = sf.File.Extension,
                    StorageUrl = sf.File.StorageUrl,
                    UploadedAt = sf.SharedOn,
                    SenderName = sf.Sender.UserName
                })
                .ToListAsync();
        }

        public async Task<long> GetTotalUsedStorageAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) return 0;

            return await _dbContext.Files
                .Where(f => f.UserId == userGuid)
                .SumAsync(f => (long?)f.Size) ?? 0;
        }
    }
}