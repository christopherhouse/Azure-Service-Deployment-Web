using AzureDeploymentSaaS.Shared.Infrastructure.Extensions;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using Identity.Api.Services;
using Identity.Api.Endpoints;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Identity API", 
        Version = "v1",
        Description = "Microservice for user and tenant management with Microsoft Entra External ID integration"
    });
});

// Add shared infrastructure services
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Add JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add CORS for SaaS frontend
builder.Services.AddSaasCors(builder.Configuration);

// Add application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITenantService, TenantService>();

// Add validators
builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
builder.Services.AddScoped<IValidator<CreateTenantRequest>, CreateTenantRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateTenantRequest>, UpdateTenantRequestValidator>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("SaasPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

// Map API endpoints
app.MapUserEndpoints();
app.MapTenantEndpoints();

app.Run();
