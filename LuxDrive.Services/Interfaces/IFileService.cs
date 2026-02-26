using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LuxDrive.ViewModels.File;

namespace LuxDrive.Services.Interfaces
{
    public interface IFileService
    {
        Task<Guid?> CreateFileAsync(string userId, IFormFile file);
        Task<string?> GetFileExtensionAsync(Guid? fileId);
        Task<bool> UpdateFileUrlAsync(Guid? fileId, string url);

        Task ShareFileAsync(Guid fileId, string senderId, Guid receiverId);

        Task<IEnumerable<IndexViewModel>> GetUserFilesAsync(string userId);
        Task<IEnumerable<IndexViewModel>> GetSharedWithMeFilesAsync(string userId);

        Task<bool> ChangeFileNameAsync(string userId, Guid fileId, string newName);

        Task<LuxDrive.Data.Models.File?> GetUserFileAsync(Guid fileId, string userId);

        Task<bool> RemoveFileAsync(LuxDrive.Data.Models.File file);

        Task<long> GetTotalUsedStorageAsync(string userId);
    }
}