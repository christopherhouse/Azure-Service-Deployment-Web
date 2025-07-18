using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureDeploymentSaaS.Shared.Contracts.Models;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using TemplateLibrary.Api.Services;
using FluentValidation;

namespace TemplateLibrary.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
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
    [ProducesResponseType(typeof(IEnumerable<TemplateDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<TemplateDto>>> GetTemplates(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenantId = GetTenantId();
            var templates = await _templateService.GetTemplatesAsync(tenantId, category, search, page, pageSize);
            return Ok(templates);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get templates");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving templates");
            return StatusCode(500, new { error = "Internal server error", message = "Failed to retrieve templates" });
        }
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TemplateDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<TemplateDto>> GetTemplate(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var template = await _templateService.GetTemplateAsync(id, tenantId);
            
            if (template == null)
                return NotFound(new { error = "Template not found", templateId = id });

            return Ok(template);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get template {TemplateId}", id);
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template {TemplateId}", id);
            return StatusCode(500, new { error = "Internal server error", message = "Failed to retrieve template" });
        }
    }

    /// <summary>
    /// Create a new ARM template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TemplateDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<TemplateDto>> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        try
        {
            var validator = new CreateTemplateRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            
            if (!validationResult.IsValid)
            {
                return BadRequest(new { 
                    error = "Validation failed", 
                    errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) 
                });
            }

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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to create template");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, new { error = "Internal server error", message = "Failed to create template" });
        }
    }

    /// <summary>
    /// Update an existing template
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TemplateDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<TemplateDto>> UpdateTemplate(Guid id, [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var validator = new UpdateTemplateRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            
            if (!validationResult.IsValid)
            {
                return BadRequest(new { 
                    error = "Validation failed", 
                    errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) 
                });
            }

            var tenantId = GetTenantId();
            var existingTemplate = await _templateService.GetTemplateAsync(id, tenantId);
            
            if (existingTemplate == null)
                return NotFound(new { error = "Template not found", templateId = id });

            existingTemplate.Name = request.Name;
            existingTemplate.Description = request.Description;
            existingTemplate.Category = request.Category;
            existingTemplate.TemplateContent = request.TemplateContent;
            existingTemplate.ParametersContent = request.ParametersContent;
            existingTemplate.Tags = request.Tags;
            existingTemplate.ModifiedAt = DateTime.UtcNow;
            existingTemplate.IsPublic = request.IsPublic;

            var updatedTemplate = await _templateService.UpdateTemplateAsync(existingTemplate);
            return Ok(updatedTemplate);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to update template {TemplateId}", id);
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {TemplateId}", id);
            return StatusCode(500, new { error = "Internal server error", message = "Failed to update template" });
        }
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var template = await _templateService.GetTemplateAsync(id, tenantId);
            
            if (template == null)
                return NotFound(new { error = "Template not found", templateId = id });

            await _templateService.DeleteTemplateAsync(id, tenantId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to delete template {TemplateId}", id);
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {TemplateId}", id);
            return StatusCode(500, new { error = "Internal server error", message = "Failed to delete template" });
        }
    }

    /// <summary>
    /// Search templates by content using Azure AI Search
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<TemplateDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<TemplateDto>>> SearchTemplates(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { error = "Search query is required" });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenantId = GetTenantId();
            var templates = await _templateService.SearchTemplatesAsync(query, tenantId, page, pageSize);
            return Ok(templates);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to search templates");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching templates with query: {Query}", query);
            return StatusCode(500, new { error = "Internal server error", message = "Failed to search templates" });
        }
    }

    private Guid GetTenantId()
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value ?? User.FindFirst("extension_tenant_id")?.Value;
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

public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required")
            .MaximumLength(255).WithMessage("Template name cannot exceed 255 characters");
            
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
            
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");
            
        RuleFor(x => x.TemplateContent)
            .NotEmpty().WithMessage("Template content is required")
            .Must(BeValidJson).WithMessage("Template content must be valid JSON");
    }
    
    private bool BeValidJson(string json)
    {
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

public class UpdateTemplateRequestValidator : AbstractValidator<UpdateTemplateRequest>
{
    public UpdateTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required")
            .MaximumLength(255).WithMessage("Template name cannot exceed 255 characters");
            
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
            
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");
            
        RuleFor(x => x.TemplateContent)
            .NotEmpty().WithMessage("Template content is required")
            .Must(BeValidJson).WithMessage("Template content must be valid JSON");
    }
    
    private bool BeValidJson(string json)
    {
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