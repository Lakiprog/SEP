using Telecom.Interfaces;
using Telecom.Models;
using Telecom.Data;
using Microsoft.EntityFrameworkCore;

namespace Telecom.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly TelecomDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(TelecomDbContext context, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Subscription> CreateSubscription(Subscription subscription)
        {
            try
            {
                subscription.CreatedAt = DateTime.UtcNow;
                subscription.Status = "PENDING"; // Initially pending until payment is confirmed
                subscription.IsPaid = false;

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created subscription: {subscription.Id} for user {subscription.UserId}");
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                throw;
            }
        }

        public async Task<List<Subscription>> GetUserSubscriptions(int userId)
        {
            try
            {
                return await _context.Subscriptions
                    .Include(s => s.Package)
                    .Include(s => s.User)
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting subscriptions for user {userId}");
                throw;
            }
        }

        public async Task<List<Subscription>> GetAllSubscriptions()
        {
            try
            {
                return await _context.Subscriptions
                    .Include(s => s.Package)
                    .Include(s => s.User)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all subscriptions");
                throw;
            }
        }

        public async Task<Subscription> UpdateSubscription(Subscription subscription)
        {
            try
            {
                var existingSubscription = await _context.Subscriptions.FindAsync(subscription.Id);
                if (existingSubscription == null)
                {
                    throw new ArgumentException($"Subscription {subscription.Id} not found");
                }

                // Update fields
                existingSubscription.Status = subscription.Status;
                existingSubscription.IsPaid = subscription.IsPaid;
                existingSubscription.TimeOfPayment = subscription.TimeOfPayment;
                existingSubscription.PaymentMethod = subscription.PaymentMethod;
                
                if (subscription.IsPaid && existingSubscription.Status == "PENDING")
                {
                    existingSubscription.Status = "ACTIVE";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated subscription: {subscription.Id}");
                return existingSubscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating subscription {subscription.Id}");
                throw;
            }
        }

        public async Task<Subscription?> GetSubscriptionByTransactionId(string transactionId)
        {
            try
            {
                if (Guid.TryParse(transactionId, out var transactionGuid))
                {
                    return await _context.Subscriptions
                        .Include(s => s.Package)
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.TransactionId == transactionGuid);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting subscription by transaction ID {transactionId}");
                throw;
            }
        }

        public async Task<Subscription?> GetSubscriptionById(int id)
        {
            try
            {
                return await _context.Subscriptions
                    .Include(s => s.Package)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting subscription by ID {id}");
                throw;
            }
        }
    }
}
