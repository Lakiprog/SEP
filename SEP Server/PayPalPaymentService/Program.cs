using PayPalPaymentService.Interfaces;
using PayPalPaymentService.Models;
using PayPalPaymentService.Services;
using Consul;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<PayPalConfig>(builder.Configuration.GetSection("PayPal"));
builder.Services.AddHttpClient<IPayPalService, PayPalService>();
builder.Services.AddScoped<IPayPalService, PayPalService>();

// Add Consul
builder.Services.AddSingleton<IConsulClient>(provider =>
{
    var consulConfig = new ConsulClientConfiguration
    {
        Address = new Uri("http://localhost:8500")
    };
    return new ConsulClient(consulConfig);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", service = "paypal-payment-service", timestamp = DateTime.UtcNow });

// Register with Consul
var consulClient = app.Services.GetRequiredService<IConsulClient>();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        var registration = new AgentServiceRegistration
        {
            ID = "paypal-payment-service-1",
            Name = "paypal-payment-service",
            Address = "localhost",
            Port = 7008,
            Tags = new[] { "payment", "paypal", "digital-wallet" },
            Check = new AgentServiceCheck
            {
                HTTP = "https://localhost:7008/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            }
        };

        await consulClient.Agent.ServiceRegister(registration);
        Console.WriteLine("PayPal Payment Service registered with Consul");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to register with Consul: {ex.Message}");
        Console.WriteLine("PayPal Payment Service running without service discovery");
    }
});

lifetime.ApplicationStopping.Register(async () =>
{
    try
    {
        await consulClient.Agent.ServiceDeregister("paypal-payment-service-1");
        Console.WriteLine("PayPal Payment Service deregistered from Consul");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to deregister from Consul: {ex.Message}");
    }
});

app.Run();
