using BitcoinPaymentService.Services;
using Consul;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add HttpClient for Bitcoin service
builder.Services.AddHttpClient<BitcoinService>();

// Add Consul
builder.Services.AddSingleton<IConsulClient>(provider =>
{
    var consulConfig = new ConsulClientConfiguration
    {
        Address = new Uri("http://localhost:8500")
    };
    return new ConsulClient(consulConfig);
});

// Add Bitcoin service
builder.Services.AddScoped<BitcoinService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", service = "bitcoin-payment-service", timestamp = DateTime.UtcNow });

// Register with Consul
var consulClient = app.Services.GetRequiredService<IConsulClient>();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        var registration = new AgentServiceRegistration
        {
            ID = "bitcoin-payment-service-1",
            Name = "bitcoin-payment-service",
            Address = "localhost",
            Port = 7002,
            Tags = new[] { "payment", "bitcoin", "cryptocurrency" },
            Check = new AgentServiceCheck
            {
                HTTP = "https://localhost:7002/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            }
        };

        await consulClient.Agent.ServiceRegister(registration);
        Console.WriteLine("Bitcoin Payment Service registered with Consul");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to register with Consul: {ex.Message}");
        Console.WriteLine("Bitcoin Payment Service running without service discovery");
    }
});

lifetime.ApplicationStopping.Register(async () =>
{
    try
    {
        await consulClient.Agent.ServiceDeregister("bitcoin-payment-service-1");
        Console.WriteLine("Bitcoin Payment Service deregistered from Consul");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to deregister from Consul: {ex.Message}");
    }
});

app.Run();
