namespace LuxDrive.Services.Interfaces
{
    public interface IPaymentCardService
    {
        Task<bool> HasUserLinkedCardAsync(string userId);
    }
}
