using AzureDeploymentSaaS.Shared.Infrastructure.Extensions;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using Account.Api.Services;
using Account.Api.Endpoints;
using FluentValidation;
using Identity.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Account API", 
        Version = "v1",
        Description = "Microservice for tenant and user administration"
    });
});

// Add shared infrastructure services
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Add JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add CORS for SaaS frontend
builder.Services.AddSaasCors(builder.Configuration);

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Add application services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IUserService, UserService>();

// Add validators
builder.Services.AddScoped<IValidator<SuspendTenantRequest>, SuspendTenantRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateLimitsRequest>, UpdateLimitsRequestValidator>();
builder.Services.AddScoped<IValidator<UpgradePlanRequest>, UpgradePlanRequestValidator>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Account API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("SaasPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

// Map API endpoints
app.MapAccountEndpoints();

app.Run();