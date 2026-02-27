namespace LuxDrive.Services.Interfaces
{
    public interface IApplicationUserService
    {
        Task DeleteAccountAsync(string userId);
    }
}
