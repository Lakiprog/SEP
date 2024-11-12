using Microsoft.EntityFrameworkCore;
using PaymentServiceProvider.Data;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Repository
{
    public class PaymentTypeRepository : GenericRepository<PaymentType>, IPaymentTypeRepository
    {
        public PaymentTypeRepository(PaymentServiceProviderDbContext context) : base(context) { }

        public async Task<PaymentType> GetPaymentTypeByName(string paymentTypeName)
        {
            return await _context.PaymentTypes.FirstOrDefaultAsync(x => x.Name == paymentTypeName);
        }
    }
}
