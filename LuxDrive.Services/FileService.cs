using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            if (!Guid.TryParse(userId, out Guid userGuid)) return false;

            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userGuid);

            if (file == null) return false;

            string clean = newName.Trim();
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

            file.Name = clean;
            return await _dbContext.SaveChangesAsync() == 1;
        }

        public async Task<Guid?> CreateFileAsync(string userId, IFormFile file)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) return null;

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
            return await _dbContext.Files
                .AsNoTracking()
                .Where(f => f.Id == fileId)
                .Select(f => f.Extension)
                .FirstOrDefaultAsync();
        }

        public async Task<FileEntity?> GetUserFileAsync(Guid fileId, string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) return null;

            return await _dbContext.Files
                .Where(f => f.Id == fileId && f.UserId == userGuid)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<FileEntity>> GetUserFilesAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) return new List<FileEntity>();

            return await _dbContext.Files
                .AsNoTracking()
                .Where(f => f.UserId == userGuid)
                .ToListAsync();
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

        public async Task ShareFileAsync(Guid fileId, string senderId, string receiverId)
        {
            if (!Guid.TryParse(senderId, out Guid senderGuid) ||
                !Guid.TryParse(receiverId, out Guid receiverGuid))
            {
                throw new ArgumentException("Invalid IDs.");
            }

            bool areFriends = await _dbContext.UserFriends
                .AnyAsync(x => x.UserId == senderGuid && x.FriendId == receiverGuid);

            if (!areFriends) throw new InvalidOperationException("Users are not friends.");

            bool alreadyShared = await _dbContext.SharedFiles
                .AnyAsync(x => x.FileId == fileId && x.ReceiverId == receiverGuid);

            if (alreadyShared) return;

            var sharedFile = new SharedFile
            {
                FileId = fileId,
                SenderId = senderGuid,     
                ReceiverId = receiverGuid, 
                SharedOn = DateTime.UtcNow
            };

            _dbContext.SharedFiles.Add(sharedFile);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<FileEntity>> GetSharedWithMeFilesAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) return new List<FileEntity>();

            return await _dbContext.SharedFiles
                .Where(sf => sf.ReceiverId == userGuid)
                .Include(sf => sf.File)
                .Include(sf => sf.Sender)
                .Select(sf => new FileEntity
                {
                    Id = sf.File.Id,
                    Name = sf.File.Name,
                    Extension = sf.File.Extension,
                    StorageUrl = sf.File.StorageUrl,
                    UploadAt = sf.SharedOn,
                    SenderName = sf.Sender.UserName
                })
                .ToListAsync();
        }
    }
}