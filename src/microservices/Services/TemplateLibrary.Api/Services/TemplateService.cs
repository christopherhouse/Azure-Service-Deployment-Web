using AzureDeploymentSaaS.Shared.Contracts.Models;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using AzureDeploymentSaaS.Shared.Infrastructure.Repositories;
using AzureDeploymentSaaS.Shared.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Data = AzureDeploymentSaaS.Shared.Infrastructure.Data;

namespace TemplateLibrary.Api.Services;

/// <summary>
/// Service for managing ARM templates with Azure AI Search integration
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAzureSearchService _searchService;
    private readonly ILogger<TemplateService> _logger;
    private readonly IMapper _mapper;
    private const string SearchIndexName = "templates";

    public TemplateService(
        IUnitOfWork unitOfWork,
        IAzureSearchService searchService,
        ILogger<TemplateService> logger,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _searchService = searchService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<IEnumerable<TemplateDto>> GetTemplatesAsync(Guid tenantId, string? category, string? search, int page, int pageSize)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.Template>();
            IEnumerable<Data.Template> templates;

            if (!string.IsNullOrEmpty(search))
            {
                // Use Azure AI Search for content-based search
                var searchResults = await _searchService.SearchAsync<TemplateSearchDocument>(search, SearchIndexName, (page - 1) * pageSize, pageSize);
                var templateIds = searchResults.Select(r => r.Id).ToList();
                
                if (templateIds.Any())
                {
                    templates = await repository.FindAsync(t => templateIds.Contains(t.Id) && t.TenantId == tenantId);
                }
                else
                {
                    templates = new List<Data.Template>();
                }
            }
            else
            {
                templates = await repository.FindAsync(
                    t => t.TenantId == tenantId && (string.IsNullOrEmpty(category) || t.Category == category),
                    tenantId);
            }

            return _mapper.Map<IEnumerable<TemplateDto>>(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<TemplateDto?> GetTemplateAsync(Guid id, Guid tenantId)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.Template>();
            var template = await repository.GetByIdAsync(id, tenantId);
            return template != null ? _mapper.Map<TemplateDto>(template) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }

    public async Task<TemplateDto> CreateTemplateAsync(TemplateDto templateDto)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.Template>();
            var template = _mapper.Map<Data.Template>(templateDto);
            template.CreatedAt = DateTime.UtcNow;
            template.ModifiedAt = DateTime.UtcNow;
            template.Version = 1;

            var createdTemplate = await repository.AddAsync(template);
            await _unitOfWork.SaveChangesAsync();

            // Index in Azure Search for content-based search
            var searchDoc = new TemplateSearchDocument
            {
                Id = createdTemplate.Id,
                Name = createdTemplate.Name,
                Description = createdTemplate.Description,
                Category = createdTemplate.Category,
                Content = createdTemplate.TemplateContent,
                Tags = createdTemplate.Tags,
                TenantId = createdTemplate.TenantId,
                IsPublic = createdTemplate.IsPublic
            };

            await _searchService.IndexDocumentAsync(searchDoc, SearchIndexName);
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Created template {TemplateId} for tenant {TenantId}", createdTemplate.Id, createdTemplate.TenantId);
            return _mapper.Map<TemplateDto>(createdTemplate);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating template for tenant {TenantId}", templateDto.TenantId);
            throw;
        }
    }

    public async Task<TemplateDto> UpdateTemplateAsync(TemplateDto templateDto)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.Template>();
            var template = _mapper.Map<Data.Template>(templateDto);
            template.ModifiedAt = DateTime.UtcNow;
            template.Version++;

            var updatedTemplate = await repository.UpdateAsync(template);
            await _unitOfWork.SaveChangesAsync();

            // Update search index
            var searchDoc = new TemplateSearchDocument
            {
                Id = updatedTemplate.Id,
                Name = updatedTemplate.Name,
                Description = updatedTemplate.Description,
                Category = updatedTemplate.Category,
                Content = updatedTemplate.TemplateContent,
                Tags = updatedTemplate.Tags,
                TenantId = updatedTemplate.TenantId,
                IsPublic = updatedTemplate.IsPublic
            };

            await _searchService.IndexDocumentAsync(searchDoc, SearchIndexName);
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Updated template {TemplateId} for tenant {TenantId}", updatedTemplate.Id, updatedTemplate.TenantId);
            return _mapper.Map<TemplateDto>(updatedTemplate);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error updating template {TemplateId}", templateDto.Id);
            throw;
        }
    }

    public async Task DeleteTemplateAsync(Guid id, Guid tenantId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.Template>();
            await repository.DeleteAsync(id, tenantId);
            await _unitOfWork.SaveChangesAsync();

            // Remove from search index
            await _searchService.DeleteDocumentAsync(id.ToString(), SearchIndexName);
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Deleted template {TemplateId} for tenant {TenantId}", id, tenantId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error deleting template {TemplateId} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }

    public async Task<IEnumerable<TemplateDto>> SearchTemplatesAsync(string query, Guid tenantId, int page, int pageSize)
    {
        try
        {
            var searchResults = await _searchService.SearchAsync<TemplateSearchDocument>(
                $"{query} AND (tenantId:{tenantId} OR isPublic:true)", 
                SearchIndexName, 
                (page - 1) * pageSize, 
                pageSize);

            var templateIds = searchResults.Select(r => r.Id).ToList();
            
            if (!templateIds.Any())
                return new List<TemplateDto>();

            var repository = _unitOfWork.Repository<Data.Template>();
            var templates = await repository.FindAsync(t => templateIds.Contains(t.Id));
            
            return _mapper.Map<IEnumerable<TemplateDto>>(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching templates with query '{Query}' for tenant {TenantId}", query, tenantId);
            throw;
        }
    }
}

/// <summary>
/// Document model for Azure AI Search indexing
/// </summary>
public class TemplateSearchDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public Guid TenantId { get; set; }
    public bool IsPublic { get; set; }
}