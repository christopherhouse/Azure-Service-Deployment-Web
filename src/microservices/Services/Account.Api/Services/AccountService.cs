using AzureDeploymentSaaS.Shared.Contracts.Models;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace Account.Api.Services;

/// <summary>
/// Service for account management operations (admin functions)
/// </summary>
public interface IAccountService
{
    Task<IEnumerable<TenantDto>> GetAllTenantsAsync(int page, int pageSize);
    Task<TenantDto?> GetTenantDetailsAsync(Guid tenantId);
    Task<IEnumerable<UserDto>> GetTenantUsersAsync(Guid tenantId, int page, int pageSize);
    Task<bool> SuspendTenantAsync(Guid tenantId, string reason);
    Task<bool> ReactivateTenantAsync(Guid tenantId);
    Task<bool> UpdateTenantLimitsAsync(Guid tenantId, int maxUsers, int maxTemplates);
    Task<bool> UpgradeTenantPlanAsync(Guid tenantId, SubscriptionPlan newPlan);
    Task<AccountSummaryDto> GetAccountSummaryAsync(Guid tenantId);
}

public class AccountService : IAccountService
{
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(ITenantService tenantService, IUserService userService, ILogger<AccountService> logger)
    {
        _tenantService = tenantService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<IEnumerable<TenantDto>> GetAllTenantsAsync(int page, int pageSize)
    {
        try
        {
            return await _tenantService.GetAllTenantsAsync(page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all tenants");
            throw;
        }
    }

    public async Task<TenantDto?> GetTenantDetailsAsync(Guid tenantId)
    {
        try
        {
            return await _tenantService.GetTenantByIdAsync(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant details {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<IEnumerable<UserDto>> GetTenantUsersAsync(Guid tenantId, int page, int pageSize)
    {
        try
        {
            return await _userService.GetUsersByTenantAsync(tenantId, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> SuspendTenantAsync(Guid tenantId, string reason)
    {
        try
        {
            _logger.LogInformation("Suspending tenant {TenantId} with reason: {Reason}", tenantId, reason);
            return await _tenantService.DeactivateTenantAsync(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> ReactivateTenantAsync(Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Reactivating tenant {TenantId}", tenantId);
            return await _tenantService.ActivateTenantAsync(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> UpdateTenantLimitsAsync(Guid tenantId, int maxUsers, int maxTemplates)
    {
        try
        {
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return false;

            tenant.MaxUsers = maxUsers;
            tenant.MaxTemplates = maxTemplates;

            await _tenantService.UpdateTenantAsync(tenant);
            _logger.LogInformation("Updated limits for tenant {TenantId}: {MaxUsers} users, {MaxTemplates} templates", 
                tenantId, maxUsers, maxTemplates);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant limits {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> UpgradeTenantPlanAsync(Guid tenantId, SubscriptionPlan newPlan)
    {
        try
        {
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                return false;

            var oldPlan = tenant.SubscriptionPlan;
            tenant.SubscriptionPlan = newPlan;

            // Update limits based on plan
            switch (newPlan)
            {
                case SubscriptionPlan.Free:
                    tenant.MaxUsers = 5;
                    tenant.MaxTemplates = 50;
                    break;
                case SubscriptionPlan.Basic:
                    tenant.MaxUsers = 25;
                    tenant.MaxTemplates = 250;
                    break;
                case SubscriptionPlan.Professional:
                    tenant.MaxUsers = 100;
                    tenant.MaxTemplates = 1000;
                    break;
                case SubscriptionPlan.Enterprise:
                    tenant.MaxUsers = 1000;
                    tenant.MaxTemplates = 10000;
                    break;
            }

            await _tenantService.UpdateTenantAsync(tenant);
            _logger.LogInformation("Upgraded tenant {TenantId} from {OldPlan} to {NewPlan}", 
                tenantId, oldPlan, newPlan);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading tenant plan {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<AccountSummaryDto> GetAccountSummaryAsync(Guid tenantId)
    {
        try
        {
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant == null)
                throw new ArgumentException("Tenant not found", nameof(tenantId));

            var users = await _userService.GetUsersByTenantAsync(tenantId, 1, int.MaxValue);
            var activeUsers = users.Count(u => u.IsActive);

            return new AccountSummaryDto
            {
                TenantId = tenantId,
                TenantName = tenant.Name,
                SubscriptionPlan = tenant.SubscriptionPlan,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt,
                TotalUsers = users.Count(),
                ActiveUsers = activeUsers,
                MaxUsers = tenant.MaxUsers,
                MaxTemplates = tenant.MaxTemplates,
                TemplatesUsed = 0 // TODO: Implement template count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account summary for tenant {TenantId}", tenantId);
            throw;
        }
    }
}

/// <summary>
/// Account summary data transfer object
/// </summary>
public class AccountSummaryDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public SubscriptionPlan SubscriptionPlan { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int MaxUsers { get; set; }
    public int MaxTemplates { get; set; }
    public int TemplatesUsed { get; set; }
}