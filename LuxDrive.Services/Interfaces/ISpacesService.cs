namespace LuxDrive.Services.Interfaces
{
    public interface ISpacesService
    {
        Task<string> UploadAsync(Stream stream, string key, string contentType);
        Task<List<string>> ListFiles();
        Task DeleteAsync(string key);
    }
}
