using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddHttpClient();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add Ocelot with Consul service discovery
builder.Services.AddOcelot(builder.Configuration)
    .AddConsul();

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

var app = builder.Build();

// Configure the HTTP request pipeline.

// Add custom middleware for request logging
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    // Log incoming request
    var clientIP = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var userAgent = context.Request.Headers.UserAgent.ToString();
    var method = context.Request.Method;
    var path = context.Request.Path;
    var query = context.Request.QueryString;

    logger.LogInformation("[GATEWAY] Incoming Request: {Method} {Path}{Query} from {ClientIP}",
        method, path, query, clientIP);

    if (userAgent.Contains("Mozilla") || userAgent.Contains("Chrome"))
    {
        logger.LogInformation("[GATEWAY] Request from Telecom Frontend detected - User Agent: {UserAgent}", userAgent);
    }

    // Record start time
    var startTime = DateTime.UtcNow;

    // Continue to next middleware
    await next();

    // Log response
    var duration = DateTime.UtcNow - startTime;
    var statusCode = context.Response.StatusCode;

    if (path.StartsWithSegments("/api/psp"))
    {
        logger.LogInformation("[GATEWAY] PSP Request Completed: {Method} {Path} - Status: {StatusCode} in {Duration}ms",
            method, path, statusCode, duration.TotalMilliseconds);
    }
    else
    {
        logger.LogInformation("[GATEWAY] Request Completed: {Method} {Path} - Status: {StatusCode} in {Duration}ms",
            method, path, statusCode, duration.TotalMilliseconds);
    }
});

// Use CORS before Ocelot
app.UseCors("AllowAll");

// Add Ocelot request logging
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    if (context.Request.Path.StartsWithSegments("/api/psp"))
    {
        logger.LogInformation("[GATEWAY -> PSP] Forwarding to PSP: {Method} {Path}",
            context.Request.Method, context.Request.Path);

        var headers = string.Join(", ", context.Request.Headers
            .Where(h => h.Key.ToLower() != "authorization") // Don't log auth headers
            .Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));

        logger.LogInformation("[GATEWAY -> PSP] Request Headers: {Headers}", headers);
    }

    await next();
});

// Use Ocelot middleware
await app.UseOcelot();

// Log Gateway startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("============================================");
logger.LogInformation("API GATEWAY STARTED SUCCESSFULLY!");
logger.LogInformation("============================================");
logger.LogInformation("Gateway URL: https://localhost:5001 (HTTPS)");
logger.LogInformation("Gateway URL: http://localhost:5000 (HTTP)");
logger.LogInformation("PSP Service Discovery: Consul at localhost:8500");
logger.LogInformation("Available PSP Routes:");
logger.LogInformation("   - POST /api/psp/payment/create");
logger.LogInformation("   - POST /api/psp/payment/{{id}}/process");
logger.LogInformation("   - GET  /api/psp/payment/{{id}}/status");
logger.LogInformation("   - GET  /api/psp/payment-methods");
logger.LogInformation("   - POST /api/psp/callback");
logger.LogInformation("   - GET  /api/psp/transactions");
logger.LogInformation("============================================");
logger.LogInformation("Waiting for Telecom Frontend requests...");

app.Run();
