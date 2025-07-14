using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using AzureDeploymentWeb.Services;
using AzureDeploymentWeb.Hubs;

var builder = WebApplication.CreateBuilder(args);

var clientId = builder.Configuration["AzureAd:ClientId"];
var clientSecret = builder.Configuration["AzureAd:ClientSecret"];

// Add services to the container.
var controllersBuilder = builder.Services.AddControllersWithViews();

// Only configure Microsoft Identity Web if ClientId is provided
if (!string.IsNullOrEmpty(clientId))
{
    builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");
    controllersBuilder.AddMicrosoftIdentityUI();
}

// Register Azure services
builder.Services.AddScoped<IAzureDeploymentService, AzureDeploymentService>();
builder.Services.AddScoped<IAzureResourceDiscoveryService, AzureResourceDiscoveryService>();

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
