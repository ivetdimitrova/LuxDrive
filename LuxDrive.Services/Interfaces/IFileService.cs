using Microsoft.AspNetCore.Http;

namespace LuxDrive.Services.Interfaces
{
    public interface IFileService
    {
        Task<Guid?> CreateFileAsync(string userId, IFormFile file);
        Task<string?> GetFileExtensionAsync(Guid? fileId);
        Task<bool> UpdateFileUrlAsync(Guid? fileId, string url);
        Task ShareFileAsync(Guid fileId, Guid senderId, Guid receiverId);
        Task<IEnumerable<Data.Models.File>> GetUserFilesAsync(Guid userId);

        Task<bool> ChangeFileNameAsync(Guid userId,Guid fileId,string newName);

        Task<Data.Models.File?> GetUserFileAsync(Guid fileId, Guid userId);

        Task<bool> RemoveFileAsync(Data.Models.File file);
    }
}
