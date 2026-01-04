using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LuxDrive.Services.Interfaces
{
    public interface IFileService
    {
        Task<Guid?> CreateFileAsync(string userId, IFormFile file);
        Task<string?> GetFileExtensionAsync(Guid? fileId);
        Task<bool> UpdateFileUrlAsync(Guid? fileId, string url);

        Task ShareFileAsync(Guid fileId, string senderId, string receiverId);

        Task<IEnumerable<LuxDrive.Data.Models.File>> GetUserFilesAsync(string userId);
        Task<IEnumerable<LuxDrive.Data.Models.File>> GetSharedWithMeFilesAsync(string userId);

        Task<bool> ChangeFileNameAsync(string userId, Guid fileId, string newName);

        Task<LuxDrive.Data.Models.File?> GetUserFileAsync(Guid fileId, string userId);

        Task<bool> RemoveFileAsync(LuxDrive.Data.Models.File file);
    }
}