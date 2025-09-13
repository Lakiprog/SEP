using Microsoft.EntityFrameworkCore;
using PaymentServiceProvider.Data;
using PaymentServiceProvider.Interfaces;
using PaymentServiceProvider.Repository;
using PaymentServiceProvider.Services;
using PaymentServiceProvider.Services.Plugins;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentServiceProviderDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("PSPDB")));


// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "https://localhost:3000", "https://localhost:3001") // Allow your frontend to make requests
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Allow credentials for authenticated requests
    });
});

builder.Services.AddControllers();

// Add OpenAPI/Swagger documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Payment Service Provider (PSP) API",
        Version = "v1",
        Description = "API for Payment Service Provider system that enables merchants to accept multiple payment methods through a unified interface.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "PSP Support",
            Email = "support@psp.com"
        }
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

//Add Repository implementations
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IPaymentTypeRepository, PaymentTypeRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IWebShopClientRepository, WebShopClientRepository>();
builder.Services.AddScoped<IWebShopClientPaymentTypesRepository, WebShopClientPaymentTypesRepository>();

//Add Service implementations
builder.Services.AddScoped<IPaymentTypeService, PaymentTypeService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IWebShopClientService, WebShopClientService>();

//Add PSP Services
builder.Services.AddScoped<IPSPService, PSPService>();
builder.Services.AddScoped<IPaymentPluginManager, PaymentPluginManager>();

//Register Payment Plugins
builder.Services.AddScoped<IPaymentPlugin, CardPaymentPlugin>();
builder.Services.AddScoped<IPaymentPlugin, PayPalPaymentPlugin>();
builder.Services.AddScoped<IPaymentPlugin, BitcoinPaymentPlugin>();

// Add HttpClient for bank communication
builder.Services.AddHttpClient<CardPaymentPlugin>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PSP API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Payment Service Provider API Documentation";
    });
}

// CORS must be before HTTPS redirection to handle preflight requests
app.UseCors("AllowLocalhost");

app.UseHttpsRedirection();

// Serve static files for admin panel
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

// Initialize PSP plugins and seed data
Console.WriteLine("[DEBUG] Starting PSP initialization...");
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentServiceProviderDbContext>();
    var pluginManager = scope.ServiceProvider.GetRequiredService<IPaymentPluginManager>();
    
    Console.WriteLine("[DEBUG] Services created, ensuring database...");
    // Ensure database is created
    context.Database.EnsureCreated();
    
    Console.WriteLine("[DEBUG] Getting plugin services...");
    // Register payment plugins
    var plugins = scope.ServiceProvider.GetServices<IPaymentPlugin>();
    Console.WriteLine($"[DEBUG] Found {plugins.Count()} plugins to register");
    foreach (var plugin in plugins)
    {
        Console.WriteLine($"[DEBUG] Registering plugin: {plugin.Name} (Type: {plugin.Type})");
        var result = await pluginManager.RegisterPaymentPluginAsync(plugin);
        Console.WriteLine($"[DEBUG] Plugin registration result: {result}");
    }
    
    // Seed payment types if they don't exist
    if (!context.PaymentTypes.Any())
    {
        var paymentTypes = new List<PaymentServiceProvider.Models.PaymentType>
        {
            new PaymentServiceProvider.Models.PaymentType 
            { 
                Name = "Credit/Debit Card", 
                Type = "card", 
                Description = "Pay with credit or debit card",
                IsEnabled = true,
                Configuration = "{}",
                CreatedAt = DateTime.UtcNow
            },
            new PaymentServiceProvider.Models.PaymentType 
            { 
                Name = "PayPal", 
                Type = "paypal", 
                Description = "Pay with PayPal account",
                IsEnabled = true,
                Configuration = "{}",
                CreatedAt = DateTime.UtcNow
            },
            new PaymentServiceProvider.Models.PaymentType 
            { 
                Name = "Bitcoin", 
                Type = "bitcoin", 
                Description = "Pay with Bitcoin cryptocurrency",
                IsEnabled = true,
                Configuration = "{}",
                CreatedAt = DateTime.UtcNow
            }
        };
        context.PaymentTypes.AddRange(paymentTypes);
        context.SaveChanges();
    }
    
    // Seed demo web shop client (Telecom)
    if (!context.WebShopClients.Any())
    {
        var telecomClient = new PaymentServiceProvider.Models.WebShopClient
        {
            Name = "Telecom Operator",
            Description = "Telecommunications service provider",
            AccountNumber = "ACC001",
            MerchantId = "TELECOM_001",
            MerchantPassword = "telecom123",
            ApiKey = Guid.NewGuid().ToString(),
            WebhookSecret = Guid.NewGuid().ToString(),
            BaseUrl = "https://localhost:7006",
            Status = PaymentServiceProvider.Models.ClientStatus.Active,
            Configuration = "{}",
            CreatedAt = DateTime.UtcNow
        };
        context.WebShopClients.Add(telecomClient);
        context.SaveChanges();
        
        // Subscribe Telecom to all payment methods
        var allPaymentTypes = context.PaymentTypes.ToList();
        var clientPaymentTypes = allPaymentTypes.Select(pt => new PaymentServiceProvider.Models.WebShopClientPaymentTypes
        {
            ClientId = telecomClient.Id,
            PaymentTypeId = pt.Id
        }).ToList();
        
        context.WebShopClientPaymentTypes.AddRange(clientPaymentTypes);
        context.SaveChanges();
    }
    
    Console.WriteLine("[DEBUG] PSP initialization completed successfully!");
}

app.Run();
