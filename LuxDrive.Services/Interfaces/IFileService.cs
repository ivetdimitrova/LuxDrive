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

        Task<long> GetTotalUsedStorageAsync(string userId);

        Task<bool> DeleteUserFileAsync(Guid id, string userIdStr);

        Task<string?> PermanentDeleteFileAsync(Guid id, string userIdStr);

        Task<bool> RestoreUserFileAsync(Guid id, string userIdStr);

        Task<IEnumerable<TrashViewModel>?> GetTrashedFilesAsync(string userIdStr);

        Task<bool> DeleteMultipleFilesAsync(List<Guid> ids, string userIdStr);

        Task<bool> ShareMultipleFilesAsync(List<Guid> ids, string userIdStr, Guid receiverId);

        Task<List<string>?> EmptyTrashAsync(string userIdStr);

        Task<bool> RestoreMultipleFilesAsync(List<Guid> ids, string userIdStr);

        Task<List<string>?> PermanentDeleteMultipleFileсAsync(List<Guid> ids, string userIdStr);

        Task<DownloadFileViewModel?> GetFileToDownloadAsync(Guid id,string userIdStr);

        Task<List<DownloadFileViewModel>?> GetMultipleFilesToDownloadAsync(List<Guid> ids, string userIdStr);
    }
}