using AzureDeploymentSaaS.Shared.Infrastructure.Extensions;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using Deployment.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Deployment API", 
        Version = "v1",
        Description = "Microservice for Azure ARM template deployments with real-time status tracking"
    });
});

// Add shared infrastructure services
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Add JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add CORS for SaaS frontend
builder.Services.AddSaasCors(builder.Configuration);

// Add application services
builder.Services.AddScoped<IDeploymentService, AzureDeploymentService>();

// Add health checks
builder.Services.AddHealthChecks();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Deployment API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("SaasPolicy");

// Add authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

// Add health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();