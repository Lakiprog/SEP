using PaymentCardCenterService.Interfaces;
using PaymentCardCenterService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add PCC service
builder.Services.AddScoped<IPCCService, PCCService>();

// Add HttpClient for bank communication
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
