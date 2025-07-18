using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using AzureDeploymentSaaS.Shared.Infrastructure.Data;
using AzureDeploymentSaaS.Shared.Infrastructure.Repositories;
using AzureDeploymentSaaS.Shared.Infrastructure.Services;

namespace AzureDeploymentSaaS.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring shared infrastructure services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add shared infrastructure services to the DI container
    /// </summary>
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework with Cosmos DB
        services.AddDbContext<SaasDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("CosmosDb");
            var databaseName = configuration["CosmosDb:DatabaseName"] ?? "SaasDatabase";
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseCosmos(connectionString, databaseName);
            }
        });

        // Add repository pattern
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add Azure Search service
        services.AddSingleton<IAzureSearchService, AzureSearchService>();

        return services;
    }

    /// <summary>
    /// Add JWT authentication with Azure AD configuration
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = configuration["AzureAd:Authority"];
                options.Audience = configuration["AzureAd:Audience"];
                options.RequireHttpsMetadata = true;
                
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

        return services;
    }

    /// <summary>
    /// Add CORS policy for SaaS frontend
    /// </summary>
    public static IServiceCollection AddSaasCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("SaasPolicy", builder =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "https://localhost:3000" };
                
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }
}