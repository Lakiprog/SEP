using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Telecom.Data;
using Telecom.Services;
using Telecom.Interfaces;
using Telecom.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TelecomDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("TelecomDB")));

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllers();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register services
builder.Services.AddScoped<IPackageDealService, PackageDealService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TelecomDbContext>();
    
    // Ensure database is created
    context.Database.EnsureCreated();
    
    // Seed categories if they don't exist
    if (!context.Categories.Any())
    {
        var categories = new List<Category>
        {
            new Category { Name = "Internet", Description = "Internet packages", CreatedAt = DateTime.UtcNow },
            new Category { Name = "Mobile", Description = "Mobile packages", CreatedAt = DateTime.UtcNow },
            new Category { Name = "TV", Description = "TV packages", CreatedAt = DateTime.UtcNow },
            new Category { Name = "Bundle", Description = "Combined packages", CreatedAt = DateTime.UtcNow }
        };
        context.Categories.AddRange(categories);
        context.SaveChanges();
    }
    
    
    // Seed packages if they don't exist
    if (!context.PackageDeals.Any())
    {
        var internetCategory = context.Categories.FirstOrDefault(c => c.Name == "Internet");
        var mobileCategory = context.Categories.FirstOrDefault(c => c.Name == "Mobile");
        var tvCategory = context.Categories.FirstOrDefault(c => c.Name == "TV");
        var bundleCategory = context.Categories.FirstOrDefault(c => c.Name == "Bundle");
        
        var packages = new List<PackageDeal>
        {
            new PackageDeal 
            { 
                Name = "Basic Internet", 
                Description = "100 Mbps internet connection", 
                Price = 29.99m, 
                IsForIndividual = true, 
                CategoryId = internetCategory?.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new PackageDeal 
            { 
                Name = "Premium Internet", 
                Description = "500 Mbps internet connection", 
                Price = 49.99m, 
                IsForIndividual = true, 
                CategoryId = internetCategory?.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new PackageDeal 
            { 
                Name = "Mobile Unlimited", 
                Description = "Unlimited calls, SMS and 10GB data", 
                Price = 19.99m, 
                IsForIndividual = true, 
                CategoryId = mobileCategory?.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new PackageDeal 
            { 
                Name = "TV Premium", 
                Description = "200+ channels including sports and movies", 
                Price = 39.99m, 
                IsForIndividual = true, 
                CategoryId = tvCategory?.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new PackageDeal 
            { 
                Name = "Triple Play", 
                Description = "Internet + Mobile + TV bundle", 
                Price = 79.99m, 
                IsForIndividual = false, 
                CategoryId = bundleCategory?.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            }
        };
        context.PackageDeals.AddRange(packages);
        context.SaveChanges();
    }
}

app.Run();
