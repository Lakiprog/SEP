using BankService.Data;
using BankService.Interfaces;
using BankService.Repository;
using BankService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BankServiceDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("BankOneDB")));

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3001",  // PSP frontend
                "http://localhost:3002"   // Bank frontend
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();

// Add logging
builder.Services.AddLogging();
builder.Services.AddSingleton<ILogger<PCCCommunicationService>>(provider =>
    provider.GetRequiredService<ILoggerFactory>().CreateLogger<PCCCommunicationService>());

// Add configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

//Add Repository implementations
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();
builder.Services.AddScoped<IBankTransactionRepository, BankTransactionRepository>();
builder.Services.AddScoped<IMerchantRepository, MerchantRepository>();
builder.Services.AddScoped<IPaymentCardRepository, PaymentCardRepository>();
builder.Services.AddScoped<IRegularUserRepository, RegularUserRepository>();
builder.Services.AddScoped<IPaymentCardService, PaymentCardService>();
builder.Services.AddScoped<QRCodeService, QRCodeService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPCCCommunicationService, PCCCommunicationService>();
builder.Services.AddScoped<PCCCommunicationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowLocalhost");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BankServiceDbContext>();
    context.Database.EnsureCreated();
    
    // Seed data if database is empty
    if (!context.BankAccounts.Any())
    {
        // Add sample merchants
        var merchant1 = new BankService.Models.Merchant
        {
            MerchantId = "MERCHANT001",
            Merchant_Id = "MERCHANT001",
            MerchantPassword = "password123",
            MerchantName = "Test Merchant 1",
            BankId = 1
        };
        
        var merchant2 = new BankService.Models.Merchant
        {
            MerchantId = "MERCHANT002",
            Merchant_Id = "MERCHANT002", 
            MerchantPassword = "password123",
            MerchantName = "Test Merchant 2",
            BankId = 1
        };
        
        var telecomMerchant = new BankService.Models.Merchant
        {
            MerchantId = "TELECOM_001",
            Merchant_Id = "TELECOM_001",
            MerchantPassword = "telecom123",
            MerchantName = "Telecom Service",
            BankId = 1
        };
        
        context.Merchants.AddRange(merchant1, merchant2, telecomMerchant);
        context.SaveChanges(); // Save merchants first to get generated IDs
        
        // Add sample regular users
        var user1 = new BankService.Models.RegularUser
        {
            FirstName = "Darie",
            LastName = "Colak",
            Email = "darie.colak@example.com",
            PhoneNumber = "+1234567890"
        };
        
        var user2 = new BankService.Models.RegularUser
        {
            FirstName = "Milos",
            LastName = "Josipovic", 
            Email = "milos.josp@example.com",
            PhoneNumber = "+1234567891"
        };
        
        context.RegularUsers.AddRange(user1, user2);
        context.SaveChanges(); // Save users to get generated IDs
        
        // Add sample bank accounts
        var account1 = new BankService.Models.BankAccount
        {
            AccountNumber = "1234567890",
            Balance = 10000.00m,
            RegularUserId = user1.Id,
            MerchantId = null, // No merchant for regular user accounts
            BankId = 1
        };
        
        var account2 = new BankService.Models.BankAccount
        {
            AccountNumber = "0987654321",
            Balance = 5000.00m,
            RegularUserId = user2.Id,
            MerchantId = null, // No merchant for regular user accounts
            BankId = 1
        };
        
        var merchantAccount = new BankService.Models.BankAccount
        {
            AccountNumber = "MERCHANT001",
            Balance = 0.00m,
            RegularUserId = null, // No regular user for merchant accounts
            MerchantId = merchant1.Id,
            Merchant_Id = merchant1.MerchantId, // String version
            BankId = 1
        };
        
        var telecomAccount = new BankService.Models.BankAccount
        {
            AccountNumber = "TELECOM001",
            Balance = 0.00m,
            RegularUserId = null, // No regular user for merchant accounts
            MerchantId = telecomMerchant.Id,
            Merchant_Id = telecomMerchant.MerchantId, // String version
            BankId = 1
        };
        
        context.BankAccounts.AddRange(account1, account2, merchantAccount, telecomAccount);
        context.SaveChanges(); // Save to get generated IDs
        
        // Add sample payment cards
        var card1 = new BankService.Models.PaymentCard
        {
            CardNumber = "4111111111111111",
            CardHolderName = "Darie Colak",
            ExpiryDate = "12/25",
            SecurityCode = "123",
            BankAccountId = account1.Id
        };
        
        var card2 = new BankService.Models.PaymentCard
        {
            CardNumber = "5555555555554444",
            CardHolderName = "Milos Josp",
            ExpiryDate = "06/26",
            SecurityCode = "456",
            BankAccountId = account2.Id
        };
        
        context.PaymentCards.AddRange(card1, card2);
        context.SaveChanges();
    }
}

app.Run();
