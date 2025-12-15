using Microsoft.AspNetCore.Http;

namespace LuxDrive.Services.Interfaces
{
    public interface IFileService
    {
        Task<Guid?> CreateFileAsync(string userId,IFormFile file);

        Task<string?> GetFileExtensionAsync(Guid? fileId);

        Task<bool> UpdateFileUrlAsync(Guid? fileId, string url);
        Task<IEnumerable<Data.Models.File>> GetUserFiles(Guid userId);

        Task<bool> ChangeFileName(Guid userId,Guid fileId,string newName);
    }
}
