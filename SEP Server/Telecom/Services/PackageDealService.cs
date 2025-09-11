using Telecom.Interfaces;
using Telecom.Models;
using Telecom.DTO;
using Telecom.Data;
using Microsoft.EntityFrameworkCore;

namespace Telecom.Services
{
    public class PackageDealService : IPackageDealService
    {
        private readonly TelecomDbContext _context;
        private readonly ILogger<PackageDealService> _logger;

        public PackageDealService(TelecomDbContext context, ILogger<PackageDealService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<PackageDeal>> GetAllPackagesAsync()
        {
            try
            {
                return await _context.PackageDeals
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all packages");
                throw;
            }
        }

        public async Task<PackageDeal?> GetPackageByIdAsync(int id)
        {
            try
            {
                return await _context.PackageDeals
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting package by ID");
                throw;
            }
        }

        public async Task<PackageDeal> CreatePackageAsync(PackageDeal package)
        {
            try
            {
                package.CreatedAt = DateTime.UtcNow;
                package.IsActive = true;

                _context.PackageDeals.Add(package);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created package: {package.Name}");
                return package;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating package");
                throw;
            }
        }

        public async Task<PackageDeal> UpdatePackageAsync(PackageDeal package)
        {
            try
            {
                var existingPackage = await _context.PackageDeals.FindAsync(package.Id);
                if (existingPackage == null)
                    throw new ArgumentException("Package not found");

                existingPackage.Name = package.Name;
                existingPackage.Description = package.Description;
                existingPackage.Price = package.Price;
                existingPackage.CategoryId = package.CategoryId;
                existingPackage.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated package: {package.Name}");
                return existingPackage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating package");
                throw;
            }
        }

        public async Task DeletePackageAsync(int id)
        {
            try
            {
                var package = await _context.PackageDeals.FindAsync(id);
                if (package == null)
                    throw new ArgumentException("Package not found");

                package.IsActive = false;
                package.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted package: {package.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting package");
                throw;
            }
        }

        public async Task<Subscription> SubscribeToPackageAsync(SubscriptionRequest request)
        {
            try
            {
                var package = await GetPackageByIdAsync(request.PackageId);
                if (package == null)
                    throw new ArgumentException("Package not found");

                var subscription = new Subscription
                {
                    UserId = request.UserId,
                    PackageId = request.PackageId,
                    StartDate = request.SubscriptionDate,
                    EndDate = request.SubscriptionDate.AddYears(request.Years),
                    Status = "ACTIVE",
                    PaymentMethod = request.PaymentMethod,
                    Amount = package.Price * request.Years,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {request.UserId} subscribed to package {package.Name}");
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to package");
                throw;
            }
        }

        public async Task<IEnumerable<Subscription>> GetUserSubscriptionsAsync(int userId)
        {
            try
            {
                return await _context.Subscriptions
                    .Include(s => s.Package)
                    .Where(s => s.UserId == userId && s.Status == "ACTIVE")
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user subscriptions");
                throw;
            }
        }
    }
}
