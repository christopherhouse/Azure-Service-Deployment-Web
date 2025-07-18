using Microsoft.AspNetCore.Authorization;
using AzureDeploymentSaaS.Shared.Contracts.Models;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using FluentValidation;
using System.Security.Claims;

namespace Identity.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", GetUser)
            .WithName("GetUser")
            .WithOpenApi();

        group.MapGet("/", GetUsersByTenant)
            .WithName("GetUsersByTenant")
            .WithOpenApi();

        group.MapGet("/email/{email}", GetUserByEmail)
            .WithName("GetUserByEmail")
            .WithOpenApi();

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithOpenApi();

        group.MapPut("/{id:guid}", UpdateUser)
            .WithName("UpdateUser")
            .WithOpenApi();

        group.MapPost("/{id:guid}/deactivate", DeactivateUser)
            .WithName("DeactivateUser")
            .WithOpenApi();

        group.MapPost("/{id:guid}/validate", ValidateUser)
            .WithName("ValidateUser")
            .WithOpenApi();
    }

    private static async Task<IResult> GetUser(
        Guid id,
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<IUserService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var userDto = await userService.GetUserByIdAsync(id, tenantId);
            
            if (userDto == null)
                return Results.NotFound(new { error = "User not found", userId = id });

            return Results.Ok(userDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get user {UserId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user {UserId}", id);
            return Results.Problem("Failed to retrieve user", statusCode: 500);
        }
    }

    private static async Task<IResult> GetUsersByTenant(
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<IUserService> logger,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenantId = GetTenantId(user);
            var users = await userService.GetUsersByTenantAsync(tenantId, page, pageSize);
            return Results.Ok(users);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get users by tenant");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving users by tenant");
            return Results.Problem("Failed to retrieve users", statusCode: 500);
        }
    }

    private static async Task<IResult> GetUserByEmail(
        string email,
        IUserService userService,
        ILogger<IUserService> logger)
    {
        try
        {
            var userDto = await userService.GetUserByEmailAsync(email);
            
            if (userDto == null)
                return Results.NotFound(new { error = "User not found", email });

            return Results.Ok(userDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user by email {Email}", email);
            return Results.Problem("Failed to retrieve user", statusCode: 500);
        }
    }

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<IUserService> logger,
        IValidator<CreateUserRequest> validator)
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
            
            var userDto = new UserDto
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Roles = request.Roles ?? new List<string>()
            };

            var createdUser = await userService.CreateUserAsync(userDto);
            return Results.Created($"/api/v1/users/{createdUser.Id}", createdUser);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to create user");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user");
            return Results.Problem("Failed to create user", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateUser(
        Guid id,
        UpdateUserRequest request,
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<IUserService> logger,
        IValidator<UpdateUserRequest> validator)
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
            var existingUser = await userService.GetUserByIdAsync(id, tenantId);
            
            if (existingUser == null)
                return Results.NotFound(new { error = "User not found", userId = id });

            existingUser.FirstName = request.FirstName;
            existingUser.LastName = request.LastName;
            existingUser.Roles = request.Roles ?? new List<string>();

            var updatedUser = await userService.UpdateUserAsync(existingUser);
            return Results.Ok(updatedUser);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to update user {UserId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user {UserId}", id);
            return Results.Problem("Failed to update user", statusCode: 500);
        }
    }

    private static async Task<IResult> DeactivateUser(
        Guid id,
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<IUserService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var deactivated = await userService.DeactivateUserAsync(id, tenantId);
            
            if (!deactivated)
                return Results.NotFound(new { error = "User not found", userId = id });

            return Results.NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to deactivate user {UserId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivating user {UserId}", id);
            return Results.Problem("Failed to deactivate user", statusCode: 500);
        }
    }

    private static async Task<IResult> ValidateUser(
        Guid id,
        IUserService userService,
        ClaimsPrincipal user,
        ILogger<IUserService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var isValid = await userService.ValidateUserAsync(id, tenantId);
            
            return Results.Ok(new { userId = id, isValid });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to validate user {UserId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating user {UserId}", id);
            return Results.Problem("Failed to validate user", statusCode: 500);
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

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/tenants")
            .WithTags("Tenants")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", GetTenant)
            .WithName("GetTenant")
            .WithOpenApi();

        group.MapGet("/", GetAllTenants)
            .WithName("GetAllTenants")
            .WithOpenApi();

        group.MapGet("/domain/{domain}", GetTenantByDomain)
            .WithName("GetTenantByDomain")
            .WithOpenApi();

        group.MapPost("/", CreateTenant)
            .WithName("CreateTenant")
            .WithOpenApi();

        group.MapPut("/{id:guid}", UpdateTenant)
            .WithName("UpdateTenant")
            .WithOpenApi();

        group.MapPost("/{id:guid}/activate", ActivateTenant)
            .WithName("ActivateTenant")
            .WithOpenApi();

        group.MapPost("/{id:guid}/deactivate", DeactivateTenant)
            .WithName("DeactivateTenant")
            .WithOpenApi();
    }

    private static async Task<IResult> GetTenant(
        Guid id,
        ITenantService tenantService,
        ILogger<ITenantService> logger)
    {
        try
        {
            var tenant = await tenantService.GetTenantByIdAsync(id);
            
            if (tenant == null)
                return Results.NotFound(new { error = "Tenant not found", tenantId = id });

            return Results.Ok(tenant);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tenant {TenantId}", id);
            return Results.Problem("Failed to retrieve tenant", statusCode: 500);
        }
    }

    private static async Task<IResult> GetAllTenants(
        ITenantService tenantService,
        ILogger<ITenantService> logger,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenants = await tenantService.GetAllTenantsAsync(page, pageSize);
            return Results.Ok(tenants);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tenants");
            return Results.Problem("Failed to retrieve tenants", statusCode: 500);
        }
    }

    private static async Task<IResult> GetTenantByDomain(
        string domain,
        ITenantService tenantService,
        ILogger<ITenantService> logger)
    {
        try
        {
            var tenant = await tenantService.GetTenantByDomainAsync(domain);
            
            if (tenant == null)
                return Results.NotFound(new { error = "Tenant not found", domain });

            return Results.Ok(tenant);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving tenant by domain {Domain}", domain);
            return Results.Problem("Failed to retrieve tenant", statusCode: 500);
        }
    }

    private static async Task<IResult> CreateTenant(
        CreateTenantRequest request,
        ITenantService tenantService,
        ILogger<ITenantService> logger,
        IValidator<CreateTenantRequest> validator)
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

            var tenantDto = new TenantDto
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Domain = request.Domain,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                SubscriptionPlan = request.SubscriptionPlan,
                MaxUsers = request.MaxUsers,
                MaxTemplates = request.MaxTemplates
            };

            var createdTenant = await tenantService.CreateTenantAsync(tenantDto);
            return Results.Created($"/api/v1/tenants/{createdTenant.Id}", createdTenant);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating tenant");
            return Results.Problem("Failed to create tenant", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateTenant(
        Guid id,
        UpdateTenantRequest request,
        ITenantService tenantService,
        ILogger<ITenantService> logger,
        IValidator<UpdateTenantRequest> validator)
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

            var existingTenant = await tenantService.GetTenantByIdAsync(id);
            
            if (existingTenant == null)
                return Results.NotFound(new { error = "Tenant not found", tenantId = id });

            existingTenant.Name = request.Name;
            existingTenant.SubscriptionPlan = request.SubscriptionPlan;
            existingTenant.MaxUsers = request.MaxUsers;
            existingTenant.MaxTemplates = request.MaxTemplates;

            var updatedTenant = await tenantService.UpdateTenantAsync(existingTenant);
            return Results.Ok(updatedTenant);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating tenant {TenantId}", id);
            return Results.Problem("Failed to update tenant", statusCode: 500);
        }
    }

    private static async Task<IResult> ActivateTenant(
        Guid id,
        ITenantService tenantService,
        ILogger<ITenantService> logger)
    {
        try
        {
            var activated = await tenantService.ActivateTenantAsync(id);
            
            if (!activated)
                return Results.NotFound(new { error = "Tenant not found", tenantId = id });

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activating tenant {TenantId}", id);
            return Results.Problem("Failed to activate tenant", statusCode: 500);
        }
    }

    private static async Task<IResult> DeactivateTenant(
        Guid id,
        ITenantService tenantService,
        ILogger<ITenantService> logger)
    {
        try
        {
            var deactivated = await tenantService.DeactivateTenantAsync(id);
            
            if (!deactivated)
                return Results.NotFound(new { error = "Tenant not found", tenantId = id });

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivating tenant {TenantId}", id);
            return Results.Problem("Failed to deactivate tenant", statusCode: 500);
        }
    }
}

// Request models
public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string>? Roles { get; set; }
}

public class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string>? Roles { get; set; }
}

public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Free;
    public int MaxUsers { get; set; } = 10;
    public int MaxTemplates { get; set; } = 100;
}

public class UpdateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public SubscriptionPlan SubscriptionPlan { get; set; }
    public int MaxUsers { get; set; }
    public int MaxTemplates { get; set; }
}

// Validators
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");
            
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");
            
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");
            
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");
    }
}

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required")
            .MaximumLength(255).WithMessage("Tenant name cannot exceed 255 characters");
            
        RuleFor(x => x.Domain)
            .NotEmpty().WithMessage("Domain is required")
            .MaximumLength(100).WithMessage("Domain cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9-]+$").WithMessage("Domain can only contain letters, numbers, and hyphens");
            
        RuleFor(x => x.MaxUsers)
            .GreaterThan(0).WithMessage("Max users must be greater than 0");
            
        RuleFor(x => x.MaxTemplates)
            .GreaterThan(0).WithMessage("Max templates must be greater than 0");
    }
}

public class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required")
            .MaximumLength(255).WithMessage("Tenant name cannot exceed 255 characters");
            
        RuleFor(x => x.MaxUsers)
            .GreaterThan(0).WithMessage("Max users must be greater than 0");
            
        RuleFor(x => x.MaxTemplates)
            .GreaterThan(0).WithMessage("Max templates must be greater than 0");
    }
}