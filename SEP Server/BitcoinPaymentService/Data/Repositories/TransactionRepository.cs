using Microsoft.EntityFrameworkCore;
using BitcoinPaymentService.Data.Entities;
using BitcoinPaymentService.Models;

namespace BitcoinPaymentService.Data.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly BitcoinPaymentDbContext _context;
        private readonly ILogger<TransactionRepository> _logger;

        public TransactionRepository(BitcoinPaymentDbContext context, ILogger<TransactionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Transaction> CreateAsync(Transaction transaction)
        {
            try
            {
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created transaction with ID: {TransactionId}", transaction.TransactionId);
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction with ID: {TransactionId}", transaction.TransactionId);
                throw;
            }
        }

        public async Task<Transaction?> GetByTransactionIdAsync(string transactionId)
        {
            try
            {
                return await _context.Transactions
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction by ID: {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<Transaction?> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction by GUID: {Id}", id);
                throw;
            }
        }

        public async Task<List<Transaction>> GetByStatusAsync(TransactionStatus status)
        {
            try
            {
                return await _context.Transactions
                    .Where(t => t.Status == status)
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions by status: {Status}", status);
                throw;
            }
        }

        public async Task<List<Transaction>> GetByBuyerEmailAsync(string buyerEmail)
        {
            try
            {
                return await _context.Transactions
                    .Where(t => t.BuyerEmail == buyerEmail)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions by buyer email: {BuyerEmail}", buyerEmail);
                throw;
            }
        }

        public async Task<List<Transaction>> GetByTelecomServiceIdAsync(Guid telecomServiceId)
        {
            try
            {
                return await _context.Transactions
                    .Where(t => t.TelecomServiceId == telecomServiceId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions by telecom service ID: {TelecomServiceId}", telecomServiceId);
                throw;
            }
        }

        public async Task<Transaction> UpdateAsync(Transaction transaction)
        {
            try
            {
                transaction.UpdatedAt = DateTime.UtcNow;
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated transaction with ID: {TransactionId}", transaction.TransactionId);
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction with ID: {TransactionId}", transaction.TransactionId);
                throw;
            }
        }

        public async Task DeleteAsync(long id)
        {
            try
            {
                var transaction = await GetByIdAsync(id);
                if (transaction != null)
                {
                    _context.Transactions.Remove(transaction);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted transaction with ID: {TransactionId}", transaction.TransactionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting transaction with GUID: {Id}", id);
                throw;
            }
        }

        public async Task<List<Transaction>> GetPendingTransactionsAsync()
        {
            try
            {
                return await _context.Transactions
                    .Where(t => t.Status == TransactionStatus.PENDING)
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending transactions");
                throw;
            }
        }

        public async Task<List<Transaction>> GetExpiredTransactionsAsync(DateTime expiredBefore)
        {
            try
            {
                return await _context.Transactions
                    .Where(t => t.Status == TransactionStatus.PENDING && t.CreatedAt < expiredBefore)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired transactions");
                throw;
            }
        }

        public async Task<List<Transaction>> GetAllAsync(int skip = 0, int take = 100, bool newest = true)
        {
            try
            {
                var query = _context.Transactions.AsQueryable();

                if (newest)
                {
                    query = query.OrderByDescending(t => t.CreatedAt);
                }
                else
                {
                    query = query.OrderBy(t => t.CreatedAt);
                }

                return await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all transactions");
                throw;
            }
        }
    }
}