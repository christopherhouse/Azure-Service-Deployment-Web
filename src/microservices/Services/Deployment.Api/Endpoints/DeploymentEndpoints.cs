using Microsoft.AspNetCore.Authorization;
using AzureDeploymentSaaS.Shared.Contracts.Models;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using FluentValidation;
using System.Security.Claims;

namespace Deployment.Api.Endpoints;

public static class DeploymentEndpoints
{
    public static void MapDeploymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/deployments")
            .WithTags("Deployments")
            .RequireAuthorization();

        group.MapPost("/", CreateDeployment)
            .WithName("CreateDeployment")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetDeployment)
            .WithName("GetDeployment")
            .WithOpenApi();

        group.MapGet("/", GetDeployments)
            .WithName("GetDeployments")
            .WithOpenApi();

        group.MapPost("/{id:guid}/start", StartDeployment)
            .WithName("StartDeployment")
            .WithOpenApi();

        group.MapPost("/{id:guid}/cancel", CancelDeployment)
            .WithName("CancelDeployment")
            .WithOpenApi();

        group.MapGet("/{id:guid}/outputs", GetDeploymentOutputs)
            .WithName("GetDeploymentOutputs")
            .WithOpenApi();
    }

    private static async Task<IResult> CreateDeployment(
        CreateDeploymentRequest request,
        IDeploymentService deploymentService,
        ClaimsPrincipal user,
        ILogger<IDeploymentService> logger,
        IValidator<CreateDeploymentRequest> validator)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(request);
            
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new { 
                    error = "Validation failed", 
                    errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) 
                });
            }

            var tenantId = GetTenantId(user);
            var userId = GetUserId(user);
            
            var deployment = await deploymentService.CreateDeploymentAsync(request, tenantId, userId);
            return Results.Created($"/api/v1/deployments/{deployment.Id}", deployment);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument for deployment creation");
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to create deployment");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating deployment");
            return Results.Problem("Failed to create deployment", statusCode: 500);
        }
    }

    private static async Task<IResult> GetDeployment(
        Guid id,
        IDeploymentService deploymentService,
        ClaimsPrincipal user,
        ILogger<IDeploymentService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var deployment = await deploymentService.GetDeploymentAsync(id, tenantId);
            
            if (deployment == null)
                return Results.NotFound(new { error = "Deployment not found", deploymentId = id });

            return Results.Ok(deployment);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get deployment {DeploymentId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deployment {DeploymentId}", id);
            return Results.Problem("Failed to retrieve deployment", statusCode: 500);
        }
    }

    private static async Task<IResult> GetDeployments(
        IDeploymentService deploymentService,
        ClaimsPrincipal user,
        ILogger<IDeploymentService> logger,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenantId = GetTenantId(user);
            var deployments = await deploymentService.GetDeploymentsAsync(tenantId, page, pageSize);
            return Results.Ok(deployments);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get deployments");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deployments");
            return Results.Problem("Failed to retrieve deployments", statusCode: 500);
        }
    }

    private static async Task<IResult> StartDeployment(
        Guid id,
        IDeploymentService deploymentService,
        ClaimsPrincipal user,
        ILogger<IDeploymentService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var deployment = await deploymentService.StartDeploymentAsync(id, tenantId);
            return Results.Ok(deployment);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument for starting deployment {DeploymentId}", id);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation for starting deployment {DeploymentId}", id);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to start deployment {DeploymentId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting deployment {DeploymentId}", id);
            return Results.Problem("Failed to start deployment", statusCode: 500);
        }
    }

    private static async Task<IResult> CancelDeployment(
        Guid id,
        IDeploymentService deploymentService,
        ClaimsPrincipal user,
        ILogger<IDeploymentService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var cancelled = await deploymentService.CancelDeploymentAsync(id, tenantId);
            
            if (!cancelled)
                return Results.BadRequest(new { error = "Deployment cannot be cancelled" });

            return Results.NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to cancel deployment {DeploymentId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling deployment {DeploymentId}", id);
            return Results.Problem("Failed to cancel deployment", statusCode: 500);
        }
    }

    private static async Task<IResult> GetDeploymentOutputs(
        Guid id,
        IDeploymentService deploymentService,
        ClaimsPrincipal user,
        ILogger<IDeploymentService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var outputs = await deploymentService.GetDeploymentOutputsAsync(id, tenantId);
            
            if (outputs == null)
                return Results.NotFound(new { error = "Deployment outputs not found", deploymentId = id });

            return Results.Ok(outputs);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get deployment outputs {DeploymentId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving deployment outputs {DeploymentId}", id);
            return Results.Problem("Failed to retrieve deployment outputs", statusCode: 500);
        }
    }

    private static Guid GetTenantId(ClaimsPrincipal user)
    {
        var tenantClaim = user.FindFirst("tenant_id")?.Value ?? user.FindFirst("extension_tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Invalid tenant information");
        }
        return tenantId;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var userClaim = user.FindFirst("sub")?.Value ?? user.FindFirst("oid")?.Value;
        if (string.IsNullOrEmpty(userClaim) || !Guid.TryParse(userClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user information");
        }
        return userId;
    }
}

public class CreateDeploymentRequestValidator : AbstractValidator<CreateDeploymentRequest>
{
    public CreateDeploymentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Deployment name is required")
            .MaximumLength(255).WithMessage("Deployment name cannot exceed 255 characters")
            .Matches("^[a-zA-Z0-9-_]+$").WithMessage("Deployment name can only contain letters, numbers, hyphens and underscores");
            
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("Template ID is required");
            
        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("Subscription ID is required")
            .Must(BeValidGuid).WithMessage("Invalid subscription ID format");
            
        RuleFor(x => x.ResourceGroupName)
            .NotEmpty().WithMessage("Resource group name is required")
            .MaximumLength(90).WithMessage("Resource group name cannot exceed 90 characters");
            
        RuleFor(x => x.ParametersJson)
            .Must(BeValidJson).WithMessage("Parameters must be valid JSON");
    }
    
    private bool BeValidGuid(string guidString)
    {
        return Guid.TryParse(guidString, out _);
    }
    
    private bool BeValidJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return true; // Empty is valid
        
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}