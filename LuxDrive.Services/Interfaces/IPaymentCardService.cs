namespace LuxDrive.Services.Interfaces
{
    public interface IPaymentCardService
    {
        Task<bool> HasUserLinkedCardAsync(Guid userId);

        Task CreateCard(Guid userId, string last4, string cardType);
    }
}
