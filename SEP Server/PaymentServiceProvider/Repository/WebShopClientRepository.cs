﻿using Microsoft.EntityFrameworkCore;
using PaymentServiceProvider.Data;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Models;

namespace PaymentServiceProvider.Repository
{
    public class WebShopClientRepository : GenericRepository<WebShopClient>, IWebShopClientRepository
    {
        public WebShopClientRepository(PaymentServiceProviderDbContext context) : base(context) { }

        public async Task<WebShopClient> GetWebShopClientByName(string webShopClientName)
        {
            return await _context.WebShopClients.FirstOrDefaultAsync(x => x.Name == webShopClientName);
        }
    }
}
