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