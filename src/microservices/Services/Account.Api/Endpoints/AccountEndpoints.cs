using Microsoft.AspNetCore.Authorization;
using AzureDeploymentSaaS.Shared.Contracts.Models;
using Account.Api.Services;
using FluentValidation;
using System.Security.Claims;

namespace Account.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/account")
            .WithTags("Account Management")
            .RequireAuthorization();

        // Tenant management
        group.MapGet("/tenants", GetAllTenants)
            .WithName("GetAllTenants")
            .WithOpenApi()
            .RequireAuthorization("AdminOnly");

        group.MapGet("/tenants/{id:guid}", GetTenantDetails)
            .WithName("GetTenantDetails")
            .WithOpenApi();

        group.MapGet("/tenants/{id:guid}/users", GetTenantUsers)
            .WithName("GetTenantUsers")
            .WithOpenApi();

        group.MapPost("/tenants/{id:guid}/suspend", SuspendTenant)
            .WithName("SuspendTenant")
            .WithOpenApi()
            .RequireAuthorization("AdminOnly");

        group.MapPost("/tenants/{id:guid}/reactivate", ReactivateTenant)
            .WithName("ReactivateTenant")
            .WithOpenApi()
            .RequireAuthorization("AdminOnly");

        group.MapPut("/tenants/{id:guid}/limits", UpdateTenantLimits)
            .WithName("UpdateTenantLimits")
            .WithOpenApi()
            .RequireAuthorization("AdminOnly");

        group.MapPut("/tenants/{id:guid}/plan", UpgradeTenantPlan)
            .WithName("UpgradeTenantPlan")
            .WithOpenApi();

        // Account summary
        group.MapGet("/summary", GetAccountSummary)
            .WithName("GetAccountSummary")
            .WithOpenApi();
    }

    private static async Task<IResult> GetAllTenants(
        IAccountService accountService,
        ILogger<IAccountService> logger,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenants = await accountService.GetAllTenantsAsync(page, pageSize);
            return Results.Ok(tenants);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all tenants");
            return Results.Problem("Failed to retrieve tenants", statusCode: 500);
        }
    }

    private static async Task<IResult> GetTenantDetails(
        Guid id,
        IAccountService accountService,
        ClaimsPrincipal user,
        ILogger<IAccountService> logger)
    {
        try
        {
            // Allow users to see their own tenant or admins to see any tenant
            var userTenantId = GetTenantId(user);
            var isAdmin = user.IsInRole("Admin");
            
            if (!isAdmin && userTenantId != id)
                return Results.Forbid();

            var tenant = await accountService.GetTenantDetailsAsync(id);
            
            if (tenant == null)
                return Results.NotFound(new { error = "Tenant not found", tenantId = id });

            return Results.Ok(tenant);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get tenant details {TenantId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tenant details {TenantId}", id);
            return Results.Problem("Failed to retrieve tenant details", statusCode: 500);
        }
    }

    private static async Task<IResult> GetTenantUsers(
        Guid id,
        IAccountService accountService,
        ClaimsPrincipal user,
        ILogger<IAccountService> logger,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            // Allow users to see their own tenant users or admins to see any tenant
            var userTenantId = GetTenantId(user);
            var isAdmin = user.IsInRole("Admin");
            
            if (!isAdmin && userTenantId != id)
                return Results.Forbid();

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var users = await accountService.GetTenantUsersAsync(id, page, pageSize);
            return Results.Ok(users);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get tenant users {TenantId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tenant users {TenantId}", id);
            return Results.Problem("Failed to retrieve tenant users", statusCode: 500);
        }
    }

    private static async Task<IResult> SuspendTenant(
        Guid id,
        SuspendTenantRequest request,
        IAccountService accountService,
        ILogger<IAccountService> logger,
        IValidator<SuspendTenantRequest> validator)
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

            var suspended = await accountService.SuspendTenantAsync(id, request.Reason);
            
            if (!suspended)
                return Results.NotFound(new { error = "Tenant not found", tenantId = id });

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error suspending tenant {TenantId}", id);
            return Results.Problem("Failed to suspend tenant", statusCode: 500);
        }
    }

    private static async Task<IResult> ReactivateTenant(
        Guid id,
        IAccountService accountService,
        ILogger<IAccountService> logger)
    {
        try
        {
            var reactivated = await accountService.ReactivateTenantAsync(id);
            
            if (!reactivated)
                return Results.NotFound(new { error = "Tenant not found", tenantId = id });

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reactivating tenant {TenantId}", id);
            return Results.Problem("Failed to reactivate tenant", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateTenantLimits(
        Guid id,
        UpdateLimitsRequest request,
        IAccountService accountService,
        ILogger<IAccountService> logger,
        IValidator<UpdateLimitsRequest> validator)
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

            var updated = await accountService.UpdateTenantLimitsAsync(id, request.MaxUsers, request.MaxTemplates);
            
            if (!updated)
                return Results.NotFound(new { error = "Tenant not found", tenantId = id });

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating tenant limits {TenantId}", id);
            return Results.Problem("Failed to update tenant limits", statusCode: 500);
        }
    }

    private static async Task<IResult> UpgradeTenantPlan(
        Guid id,
        UpgradePlanRequest request,
        IAccountService accountService,
        ClaimsPrincipal user,
        ILogger<IAccountService> logger,
        IValidator<UpgradePlanRequest> validator)
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

            // Allow users to upgrade their own tenant or admins to upgrade any tenant
            var userTenantId = GetTenantId(user);
            var isAdmin = user.IsInRole("Admin");
            
            if (!isAdmin && userTenantId != id)
                return Results.Forbid();

            var upgraded = await accountService.UpgradeTenantPlanAsync(id, request.NewPlan);
            
            if (!upgraded)
                return Results.NotFound(new { error = "Tenant not found", tenantId = id });

            return Results.NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to upgrade tenant plan {TenantId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error upgrading tenant plan {TenantId}", id);
            return Results.Problem("Failed to upgrade tenant plan", statusCode: 500);
        }
    }

    private static async Task<IResult> GetAccountSummary(
        IAccountService accountService,
        ClaimsPrincipal user,
        ILogger<IAccountService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var summary = await accountService.GetAccountSummaryAsync(tenantId);
            return Results.Ok(summary);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get account summary");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving account summary");
            return Results.Problem("Failed to retrieve account summary", statusCode: 500);
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
}

// Request models
public class SuspendTenantRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdateLimitsRequest
{
    public int MaxUsers { get; set; }
    public int MaxTemplates { get; set; }
}

public class UpgradePlanRequest
{
    public SubscriptionPlan NewPlan { get; set; }
}

// Validators
public class SuspendTenantRequestValidator : AbstractValidator<SuspendTenantRequest>
{
    public SuspendTenantRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Suspension reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}

public class UpdateLimitsRequestValidator : AbstractValidator<UpdateLimitsRequest>
{
    public UpdateLimitsRequestValidator()
    {
        RuleFor(x => x.MaxUsers)
            .GreaterThan(0).WithMessage("Max users must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("Max users cannot exceed 10,000");
            
        RuleFor(x => x.MaxTemplates)
            .GreaterThan(0).WithMessage("Max templates must be greater than 0")
            .LessThanOrEqualTo(50000).WithMessage("Max templates cannot exceed 50,000");
    }
}

public class UpgradePlanRequestValidator : AbstractValidator<UpgradePlanRequest>
{
    public UpgradePlanRequestValidator()
    {
        RuleFor(x => x.NewPlan)
            .IsInEnum().WithMessage("Invalid subscription plan");
    }
}