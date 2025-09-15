using PaymentCardCenterService.Interfaces;
using PaymentCardCenterService.Services;
using PaymentCardCenterService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add Entity Framework
builder.Services.AddDbContext<PCCDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PCCDB")));

// Add PCC service
builder.Services.AddScoped<IPCCService, PCCService>();

// Add HttpClient for bank communication
builder.Services.AddHttpClient();

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PCCDbContext>();
    try
    {
        context.Database.Migrate(); // Apply pending migrations
        Console.WriteLine("[PCC] Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[PCC ERROR] Failed to apply database migrations: {ex.Message}");
        // Fallback to EnsureCreated for development
        context.Database.EnsureCreated();
        Console.WriteLine("[PCC] Database created using EnsureCreated fallback");
    }
}

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
