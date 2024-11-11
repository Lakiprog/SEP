using Telecom.Interfaces;
using Telecom.Models;

namespace Telecom.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        public Task<Subscription> CreateSubscription(Subscription subscription)
        {
            throw new NotImplementedException();
        }

        public Task<List<Subscription>> GetUserSubscriptions(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> UpdateSubscription(Subscription subscription)
        {
            throw new NotImplementedException();
        }
    }
}
