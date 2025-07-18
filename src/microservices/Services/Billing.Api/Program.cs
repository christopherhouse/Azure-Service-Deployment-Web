using AzureDeploymentSaaS.Shared.Infrastructure.Extensions;
using Billing.Api.Services;
using Billing.Api.Endpoints;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Billing API", 
        Version = "v1",
        Description = "Microservice for subscription management and billing operations"
    });
});

// Add shared infrastructure services
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Add JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add CORS for SaaS frontend
builder.Services.AddSaasCors(builder.Configuration);

// Add application services
builder.Services.AddScoped<IBillingService, BillingService>();

// Add validators
builder.Services.AddScoped<IValidator<CreateSubscriptionRequest>, CreateSubscriptionRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateSubscriptionPlanRequest>, UpdateSubscriptionPlanRequestValidator>();
builder.Services.AddScoped<IValidator<CancelSubscriptionRequest>, CancelSubscriptionRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePaymentMethodRequest>, UpdatePaymentMethodRequestValidator>();

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Billing API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("SaasPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

// Map API endpoints
app.MapBillingEndpoints();

app.Run();