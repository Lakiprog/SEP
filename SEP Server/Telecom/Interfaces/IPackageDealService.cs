using Telecom.Models;

namespace Telecom.Interfaces
{
    public interface IPackageDealService
    {
        Task<PackageDeal> CreatePackageDeal(PackageDeal packageDeal);
        Task<PackageDeal> UpdatePackageDeal(PackageDeal packageDeal);
        Task<PackageDeal> GetPackageDealById(int packageId);
        Task<List<PackageDeal>> GetAllPackageDeals();
        Task<bool> DeletePackageDealById(int packageId);
    }
}
