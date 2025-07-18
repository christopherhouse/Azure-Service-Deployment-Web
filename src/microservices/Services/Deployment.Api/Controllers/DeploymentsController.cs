using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureDeploymentSaaS.Shared.Contracts.Models;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using FluentValidation;

namespace Deployment.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DeploymentsController : ControllerBase
{
    private readonly ILogger<DeploymentsController> _logger;
    private readonly IDeploymentService _deploymentService;

    public DeploymentsController(ILogger<DeploymentsController> logger, IDeploymentService deploymentService)
    {
        _logger = logger;
        _deploymentService = deploymentService;
    }

    /// <summary>
    /// Create a new deployment
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DeploymentDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DeploymentDto>> CreateDeployment([FromBody] CreateDeploymentRequest request)
    {
        try
        {
            var validator = new CreateDeploymentRequestValidator();
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
            
            var deployment = await _deploymentService.CreateDeploymentAsync(request, tenantId, userId);
            return CreatedAtAction(nameof(GetDeployment), new { id = deployment.Id }, deployment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for deployment creation");
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to create deployment");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deployment");
            return StatusCode(500, new { error = "Internal server error", message = "Failed to create deployment" });
        }
    }

    /// <summary>
    /// Get a specific deployment by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeploymentDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DeploymentDto>> GetDeployment(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var deployment = await _deploymentService.GetDeploymentAsync(id, tenantId);
            
            if (deployment == null)
                return NotFound(new { error = "Deployment not found", deploymentId = id });

            return Ok(deployment);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get deployment {DeploymentId}", id);
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deployment {DeploymentId}", id);
            return StatusCode(500, new { error = "Internal server error", message = "Failed to retrieve deployment" });
        }
    }

    /// <summary>
    /// Get all deployments for the current tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DeploymentDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<DeploymentDto>>> GetDeployments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var tenantId = GetTenantId();
            var deployments = await _deploymentService.GetDeploymentsAsync(tenantId, page, pageSize);
            return Ok(deployments);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get deployments");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deployments");
            return StatusCode(500, new { error = "Internal server error", message = "Failed to retrieve deployments" });
        }
    }

    /// <summary>
    /// Start a pending deployment
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(DeploymentDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DeploymentDto>> StartDeployment(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var deployment = await _deploymentService.StartDeploymentAsync(id, tenantId);
            return Ok(deployment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for starting deployment {DeploymentId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for starting deployment {DeploymentId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to start deployment {DeploymentId}", id);
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting deployment {DeploymentId}", id);
            return StatusCode(500, new { error = "Internal server error", message = "Failed to start deployment" });
        }
    }

    /// <summary>
    /// Cancel a running deployment
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CancelDeployment(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var cancelled = await _deploymentService.CancelDeploymentAsync(id, tenantId);
            
            if (!cancelled)
                return BadRequest(new { error = "Deployment cannot be cancelled" });

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to cancel deployment {DeploymentId}", id);
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling deployment {DeploymentId}", id);
            return StatusCode(500, new { error = "Internal server error", message = "Failed to cancel deployment" });
        }
    }

    /// <summary>
    /// Get deployment outputs
    /// </summary>
    [HttpGet("{id:guid}/outputs")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<object>> GetDeploymentOutputs(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var outputs = await _deploymentService.GetDeploymentOutputsAsync(id, tenantId);
            
            if (outputs == null)
                return NotFound(new { error = "Deployment outputs not found", deploymentId = id });

            return Ok(outputs);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get deployment outputs {DeploymentId}", id);
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deployment outputs {DeploymentId}", id);
            return StatusCode(500, new { error = "Internal server error", message = "Failed to retrieve deployment outputs" });
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