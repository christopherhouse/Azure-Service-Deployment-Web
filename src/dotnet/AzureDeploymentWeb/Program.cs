using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using AzureDeploymentWeb.Services;
using AzureDeploymentWeb.Hubs;
using AzureDeploymentWeb.Models;
using Microsoft.Extensions.Logging.ApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

// Bind AzureAdOptions from configuration and register with DI
builder.Services.Configure<AzureAdOptions>(builder.Configuration.GetSection(AzureAdOptions.SectionName));

// Bind ServiceBusOptions from configuration
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection(ServiceBusOptions.SectionName));

var clientId = builder.Configuration["AzureAd:ClientId"];
var clientSecret = builder.Configuration["AzureAd:ClientSecret"];

// Configure cache options
var cacheOptions = new CacheOptions();
builder.Configuration.GetSection(CacheOptions.SectionName).Bind(cacheOptions);
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));

// Configure caching services
if (!string.IsNullOrEmpty(cacheOptions.Redis.ConnectionString))
{
    Console.WriteLine("Using Redis distributed cache");
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = cacheOptions.Redis.ConnectionString;
    });
}
else
{
    Console.WriteLine("Using in-memory distributed cache");
    builder.Services.AddDistributedMemoryCache();
}

// Add services to the container.
var controllersBuilder = builder.Services.AddControllersWithViews();

// Configure Application Insights
var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrEmpty(applicationInsightsConnectionString))
{
    builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information);
    Console.WriteLine("Configuring Application Insights telemetry");
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = applicationInsightsConnectionString;
    });
}
else
{
    Console.WriteLine("Application Insights connection string not provided - telemetry disabled");
}

// Only configure Microsoft Identity Web if ClientId is provided
if (!string.IsNullOrEmpty(clientId))
{
    builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");
    controllersBuilder.AddMicrosoftIdentityUI();
}

// Register Azure services
builder.Services.AddScoped<IAzureDeploymentService, AzureDeploymentService>();
builder.Services.AddScoped<IAzureResourceDiscoveryService, AzureResourceDiscoveryService>();

// Register deployment queue services
var serviceBusNamespaceEndpoint = builder.Configuration["ServiceBus:NamespaceEndpoint"];
if (!string.IsNullOrEmpty(serviceBusNamespaceEndpoint))
{
    Console.WriteLine("Using Service Bus deployment queue with managed identity authentication");
    builder.Services.AddSingleton<IServiceBusDeploymentQueueService, ServiceBusDeploymentQueueService>();
    builder.Services.AddSingleton<IDeploymentQueueService>(provider => 
        provider.GetRequiredService<IServiceBusDeploymentQueueService>());
    builder.Services.AddHostedService<ServiceBusDeploymentWorker>();
}
else
{
    Console.WriteLine("Using in-memory deployment queue (no Service Bus namespace endpoint provided)");
    builder.Services.AddSingleton<IDeploymentQueueService, DeploymentQueueService>();
    builder.Services.AddHostedService<DeploymentWorker>();
}

// Add SignalR
var azureSignalRConnectionString = builder.Configuration["AzureSignalR:ConnectionString"];
if (!string.IsNullOrEmpty(azureSignalRConnectionString))
{
    Console.WriteLine("Using Azure SignalR Service");
    builder.Services.AddSignalR().AddAzureSignalR(azureSignalRConnectionString);
}
else
{
    Console.WriteLine("Using local SignalR (no Azure SignalR connection string provided)");
    builder.Services.AddSignalR();
}

// Add background service for deployment monitoring
builder.Services.AddHostedService<DeploymentMonitoringService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Only use authentication/authorization if Microsoft Identity Web is configured
if (!string.IsNullOrEmpty(clientId))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map SignalR hub
app.MapHub<DeploymentHub>("/deploymentHub");

app.Run();
