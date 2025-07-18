using AzureDeploymentSaaS.Shared.Contracts.Models;

namespace AzureDeploymentSaaS.Shared.Contracts.Services;

/// <summary>
/// Interface for template management service
/// </summary>
public interface ITemplateService
{
    Task<IEnumerable<TemplateDto>> GetTemplatesAsync(Guid tenantId, string? category, string? search, int page, int pageSize);
    Task<TemplateDto?> GetTemplateAsync(Guid id, Guid tenantId);
    Task<TemplateDto> CreateTemplateAsync(TemplateDto template);
    Task<TemplateDto> UpdateTemplateAsync(TemplateDto template);
    Task DeleteTemplateAsync(Guid id, Guid tenantId);
    Task<IEnumerable<TemplateDto>> SearchTemplatesAsync(string query, Guid tenantId, int page, int pageSize);
}

/// <summary>
/// Interface for user management service
/// </summary>
public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid userId, Guid tenantId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<UserDto> CreateUserAsync(UserDto user);
    Task<UserDto> UpdateUserAsync(UserDto user);
    Task<bool> DeactivateUserAsync(Guid userId, Guid tenantId);
    Task<IEnumerable<UserDto>> GetUsersByTenantAsync(Guid tenantId, int page, int pageSize);
    Task<bool> ValidateUserAsync(Guid userId, Guid tenantId);
}

/// <summary>
/// Interface for tenant management service
/// </summary>
public interface ITenantService
{
    Task<TenantDto?> GetTenantByIdAsync(Guid tenantId);
    Task<TenantDto?> GetTenantByDomainAsync(string domain);
    Task<TenantDto> CreateTenantAsync(TenantDto tenant);
    Task<TenantDto> UpdateTenantAsync(TenantDto tenant);
    Task<bool> ActivateTenantAsync(Guid tenantId);
    Task<bool> DeactivateTenantAsync(Guid tenantId);
    Task<IEnumerable<TenantDto>> GetAllTenantsAsync(int page, int pageSize);
}

/// <summary>
/// Interface for Azure deployment service
/// </summary>
public interface IDeploymentService
{
    Task<DeploymentDto> CreateDeploymentAsync(CreateDeploymentRequest request, Guid tenantId, Guid userId);
    Task<DeploymentDto?> GetDeploymentAsync(Guid deploymentId, Guid tenantId);
    Task<IEnumerable<DeploymentDto>> GetDeploymentsAsync(Guid tenantId, int page, int pageSize);
    Task<DeploymentDto> UpdateDeploymentStatusAsync(Guid deploymentId, DeploymentStatus status, string? errorMessage = null);
    Task<bool> CancelDeploymentAsync(Guid deploymentId, Guid tenantId);
    Task<DeploymentDto> StartDeploymentAsync(Guid deploymentId, Guid tenantId);
    Task<object?> GetDeploymentOutputsAsync(Guid deploymentId, Guid tenantId);
}

/// <summary>
/// Request model for creating deployments
/// </summary>
public class CreateDeploymentRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public string ParametersJson { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
}