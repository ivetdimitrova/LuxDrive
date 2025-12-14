using Microsoft.AspNetCore.Http;

namespace LuxDrive.Services.Interfaces
{
    public interface IFileService
    {
        Task<bool> CreateFileAsync(string url,IFormFile file);
    }
}
