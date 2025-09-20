using Telecom.Models;

namespace Telecom.Interfaces
{
    public interface ISubscriptionService
    {
        Task<Subscription> CreateSubscription(Subscription subscription);
        Task<Subscription> UpdateSubscription(Subscription subscription);
        Task<List<Subscription>> GetUserSubscriptions(int userId);
        Task<List<Subscription>> GetAllSubscriptions();
        Task<Subscription?> GetSubscriptionByTransactionId(string transactionId);
        Task<Subscription?> GetSubscriptionById(int id);
    }
}
