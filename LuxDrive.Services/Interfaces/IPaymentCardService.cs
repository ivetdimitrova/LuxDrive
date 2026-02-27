using LuxDrive.ViewModels.Settings;

namespace LuxDrive.Services.Interfaces
{
    public interface IPaymentCardService
    {
        Task<bool> HasUserLinkedCardAsync(Guid userId);

        Task CreateCardAsync(Guid userId, string last4, string cardType);
        Task DeleteCardAsync(Guid cardId, string userId);

        Task<List<CardViewModel>?> GetUserCards(Guid userId);
    }
}
