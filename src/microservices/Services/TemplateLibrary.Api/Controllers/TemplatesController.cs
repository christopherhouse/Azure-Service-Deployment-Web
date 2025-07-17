using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureDeploymentSaaS.Shared.Contracts.Models;

namespace TemplateLibrary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly ILogger<TemplatesController> _logger;
    private readonly ITemplateService _templateService;

    public TemplatesController(ILogger<TemplatesController> logger, ITemplateService templateService)
    {
        _logger = logger;
        _templateService = templateService;
    }

    /// <summary>
    /// Get all templates for the current user's tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TemplateDto>>> GetTemplates(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantId = GetTenantId();
            var templates = await _templateService.GetTemplatesAsync(tenantId, category, search, page, pageSize);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TemplateDto>> GetTemplate(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var template = await _templateService.GetTemplateAsync(id, tenantId);
            
            if (template == null)
                return NotFound();

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new ARM template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TemplateDto>> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();
            
            var template = new TemplateDto
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                TemplateContent = request.TemplateContent,
                ParametersContent = request.ParametersContent,
                Tags = request.Tags,
                TenantId = tenantId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                Version = 1,
                IsPublic = request.IsPublic
            };

            var createdTemplate = await _templateService.CreateTemplateAsync(template);
            return CreatedAtAction(nameof(GetTemplate), new { id = createdTemplate.Id }, createdTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing template
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TemplateDto>> UpdateTemplate(Guid id, [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var tenantId = GetTenantId();
            var existingTemplate = await _templateService.GetTemplateAsync(id, tenantId);
            
            if (existingTemplate == null)
                return NotFound();

            existingTemplate.Name = request.Name;
            existingTemplate.Description = request.Description;
            existingTemplate.Category = request.Category;
            existingTemplate.TemplateContent = request.TemplateContent;
            existingTemplate.ParametersContent = request.ParametersContent;
            existingTemplate.Tags = request.Tags;
            existingTemplate.ModifiedAt = DateTime.UtcNow;
            existingTemplate.Version++;
            existingTemplate.IsPublic = request.IsPublic;

            var updatedTemplate = await _templateService.UpdateTemplateAsync(existingTemplate);
            return Ok(updatedTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var template = await _templateService.GetTemplateAsync(id, tenantId);
            
            if (template == null)
                return NotFound();

            await _templateService.DeleteTemplateAsync(id, tenantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Search templates by content using Azure AI Search
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<TemplateDto>>> SearchTemplates(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantId = GetTenantId();
            var templates = await _templateService.SearchTemplatesAsync(query, tenantId, page, pageSize);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching templates with query: {Query}", query);
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid GetTenantId()
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Invalid tenant information");
        }
        return tenantId;
    }

    private Guid GetUserId()
    {
        var userClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("oid")?.Value;
        if (string.IsNullOrEmpty(userClaim) || !Guid.TryParse(userClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user information");
        }
        return userId;
    }
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TemplateContent { get; set; } = string.Empty;
    public string? ParametersContent { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsPublic { get; set; }
}

public class UpdateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TemplateContent { get; set; } = string.Empty;
    public string? ParametersContent { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsPublic { get; set; }
}

public interface ITemplateService
{
    Task<IEnumerable<TemplateDto>> GetTemplatesAsync(Guid tenantId, string? category, string? search, int page, int pageSize);
    Task<TemplateDto?> GetTemplateAsync(Guid id, Guid tenantId);
    Task<TemplateDto> CreateTemplateAsync(TemplateDto template);
    Task<TemplateDto> UpdateTemplateAsync(TemplateDto template);
    Task DeleteTemplateAsync(Guid id, Guid tenantId);
    Task<IEnumerable<TemplateDto>> SearchTemplatesAsync(string query, Guid tenantId, int page, int pageSize);
}