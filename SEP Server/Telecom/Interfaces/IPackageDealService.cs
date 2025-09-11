using Telecom.Models;
using Telecom.DTO;

namespace Telecom.Interfaces
{
    public interface IPackageDealService
    {
        Task<IEnumerable<PackageDeal>> GetAllPackagesAsync();
        Task<PackageDeal?> GetPackageByIdAsync(int id);
        Task<PackageDeal> CreatePackageAsync(PackageDeal package);
        Task<PackageDeal> UpdatePackageAsync(PackageDeal package);
        Task DeletePackageAsync(int id);
        Task<Subscription> SubscribeToPackageAsync(SubscriptionRequest request);
        Task<IEnumerable<Subscription>> GetUserSubscriptionsAsync(int userId);
    }
}
