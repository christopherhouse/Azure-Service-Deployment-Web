using Microsoft.AspNetCore.Authorization;
using AzureDeploymentSaaS.Shared.Contracts.Models;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using FluentValidation;
using System.Security.Claims;

namespace TemplateLibrary.Api.Endpoints;

public static class TemplateEndpoints
{
    public static void MapTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/templates")
            .WithTags("Templates")
            .RequireAuthorization();

        group.MapGet("/", GetTemplates)
            .WithName("GetTemplates")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetTemplate)
            .WithName("GetTemplate")
            .WithOpenApi();

        group.MapPost("/", CreateTemplate)
            .WithName("CreateTemplate")
            .WithOpenApi();

        group.MapPut("/{id:guid}", UpdateTemplate)
            .WithName("UpdateTemplate")
            .WithOpenApi();

        group.MapDelete("/{id:guid}", DeleteTemplate)
            .WithName("DeleteTemplate")
            .WithOpenApi();

        group.MapGet("/search", SearchTemplates)
            .WithName("SearchTemplates")
            .WithOpenApi();
    }

    private static async Task<IResult> GetTemplates(
        ITemplateService templateService,
        ClaimsPrincipal user,
        ILogger<ITemplateService> logger,
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenantId = GetTenantId(user);
            var templates = await templateService.GetTemplatesAsync(tenantId, category, search, page, pageSize);
            return Results.Ok(templates);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get templates");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving templates");
            return Results.Problem("Failed to retrieve templates", statusCode: 500);
        }
    }

    private static async Task<IResult> GetTemplate(
        Guid id,
        ITemplateService templateService,
        ClaimsPrincipal user,
        ILogger<ITemplateService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var template = await templateService.GetTemplateAsync(id, tenantId);
            
            if (template == null)
                return Results.NotFound(new { error = "Template not found", templateId = id });

            return Results.Ok(template);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to get template {TemplateId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving template {TemplateId}", id);
            return Results.Problem("Failed to retrieve template", statusCode: 500);
        }
    }

    private static async Task<IResult> CreateTemplate(
        CreateTemplateRequest request,
        ITemplateService templateService,
        ClaimsPrincipal user,
        ILogger<ITemplateService> logger,
        IValidator<CreateTemplateRequest> validator)
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

            var createdTemplate = await templateService.CreateTemplateAsync(template);
            return Results.Created($"/api/v1/templates/{createdTemplate.Id}", createdTemplate);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to create template");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating template");
            return Results.Problem("Failed to create template", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateTemplate(
        Guid id,
        UpdateTemplateRequest request,
        ITemplateService templateService,
        ClaimsPrincipal user,
        ILogger<ITemplateService> logger,
        IValidator<UpdateTemplateRequest> validator)
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
            var existingTemplate = await templateService.GetTemplateAsync(id, tenantId);
            
            if (existingTemplate == null)
                return Results.NotFound(new { error = "Template not found", templateId = id });

            existingTemplate.Name = request.Name;
            existingTemplate.Description = request.Description;
            existingTemplate.Category = request.Category;
            existingTemplate.TemplateContent = request.TemplateContent;
            existingTemplate.ParametersContent = request.ParametersContent;
            existingTemplate.Tags = request.Tags;
            existingTemplate.ModifiedAt = DateTime.UtcNow;
            existingTemplate.IsPublic = request.IsPublic;

            var updatedTemplate = await templateService.UpdateTemplateAsync(existingTemplate);
            return Results.Ok(updatedTemplate);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to update template {TemplateId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating template {TemplateId}", id);
            return Results.Problem("Failed to update template", statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteTemplate(
        Guid id,
        ITemplateService templateService,
        ClaimsPrincipal user,
        ILogger<ITemplateService> logger)
    {
        try
        {
            var tenantId = GetTenantId(user);
            var template = await templateService.GetTemplateAsync(id, tenantId);
            
            if (template == null)
                return Results.NotFound(new { error = "Template not found", templateId = id });

            await templateService.DeleteTemplateAsync(id, tenantId);
            return Results.NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to delete template {TemplateId}", id);
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting template {TemplateId}", id);
            return Results.Problem("Failed to delete template", statusCode: 500);
        }
    }

    private static async Task<IResult> SearchTemplates(
        string query,
        ITemplateService templateService,
        ClaimsPrincipal user,
        ILogger<ITemplateService> logger,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return Results.BadRequest(new { error = "Search query is required" });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenantId = GetTenantId(user);
            var templates = await templateService.SearchTemplatesAsync(query, tenantId, page, pageSize);
            return Results.Ok(templates);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt to search templates");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching templates with query: {Query}", query);
            return Results.Problem("Failed to search templates", statusCode: 500);
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