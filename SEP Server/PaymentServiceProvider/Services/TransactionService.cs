﻿using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;
using PaymentServiceProvider.Repository;

namespace PaymentServiceProvider.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<Transaction> AddTransaction(Transaction transaction)
        {
            List<Transaction> allTransactions = await GetAllTransactions();
            Transaction? existingTransaction = allTransactions.Where(x => x.MerchantTimestamp == transaction.MerchantTimestamp)
                                                    .Where(x => x.WebShopClientId == transaction.WebShopClientId)
                                                    .Where(x => x.MerchantOrderID == transaction.MerchantOrderID)
                                                    .Where(x => x.Amount == transaction.Amount)
                                                    .SingleOrDefault();
                         
            if (existingTransaction != null)
                throw new Exception($"Transaction is duplicated. Same transaction with id {existingTransaction.Id} exists. Aborting...");
            
            return await _transactionRepository.Add(transaction);            
        }

        public async Task<bool> RemoveTransaction(int id)
        {
            Transaction transaction = await _transactionRepository.Get(id);
            if (transaction == null)
                throw new Exception($"Transaction with id {id} does not exist!");

            var deleted = await _transactionRepository.Delete(id);

            return deleted == null ? false : true;
        }

        public async Task<List<Transaction>> GetAllTransactions()
        {
            IEnumerable<Transaction> transactions = await _transactionRepository.GetAll();
            return transactions.ToList();
        }

        public async Task<Transaction> GetById(int id)
        {
            return await _transactionRepository.Get(id);
        }
    }
}
