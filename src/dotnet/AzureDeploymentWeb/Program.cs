using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using AzureDeploymentWeb.Services;
using AzureDeploymentWeb.Hubs;

var builder = WebApplication.CreateBuilder(args);

var cid = builder.Configuration["AzureAd:ClientId"];
var cis = builder.Configuration["AzureAd:ClientSecret"];

// Add services to the container.
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd");

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Register Azure deployment service
builder.Services.AddScoped<IAzureDeploymentService, AzureDeploymentService>();

// Add SignalR
Console.WriteLine("Azure SignalR Connection String: " + builder.Configuration["AzureSignalR:ConnectionString"]);
builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["AzureSignalR:ConnectionString"]);

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map SignalR hub
app.MapHub<DeploymentHub>("/deploymentHub");

app.Run();
