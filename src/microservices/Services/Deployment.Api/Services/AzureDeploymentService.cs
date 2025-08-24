using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System.Text.Json;
using AzureDeploymentSaaS.Shared.Contracts.Models;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using AzureDeploymentSaaS.Shared.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Data = AzureDeploymentSaaS.Shared.Infrastructure.Data;

namespace Deployment.Api.Services;

/// <summary>
/// Production-ready deployment service for ARM template deployments
/// Ports functionality from the original monolithic application
/// </summary>
public class AzureDeploymentService : IDeploymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITemplateService _templateService;
    private readonly ILogger<AzureDeploymentService> _logger;
    private readonly IMapper _mapper;
    private readonly ArmClient _armClient;
    private readonly IConfiguration _configuration;

    public AzureDeploymentService(
        IUnitOfWork unitOfWork,
        ITemplateService templateService,
        ILogger<AzureDeploymentService> logger,
        IMapper mapper,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _templateService = templateService;
        _logger = logger;
        _mapper = mapper;
        _configuration = configuration;
        _armClient = new ArmClient(new DefaultAzureCredential());
    }

    public async Task<DeploymentDto> CreateDeploymentAsync(CreateDeploymentRequest request, Guid tenantId, Guid userId)
    {
        try
        {
            // Validate template exists and user has access
            var template = await _templateService.GetTemplateAsync(request.TemplateId, tenantId);
            if (template == null)
            {
                throw new ArgumentException($"Template {request.TemplateId} not found or access denied");
            }

            await _unitOfWork.BeginTransactionAsync();

            var deployment = new DeploymentDto
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                TemplateId = request.TemplateId,
                ParametersJson = request.ParametersJson,
                SubscriptionId = request.SubscriptionId,
                ResourceGroupName = request.ResourceGroupName,
                Status = DeploymentStatus.Pending,
                TenantId = tenantId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            var repository = _unitOfWork.Repository<Data.Deployment>();
            var deploymentEntity = _mapper.Map<Data.Deployment>(deployment);
            var createdDeployment = await repository.AddAsync(deploymentEntity);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Created deployment {DeploymentId} for tenant {TenantId}", createdDeployment.Id, tenantId);
            return _mapper.Map<DeploymentDto>(createdDeployment);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating deployment for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<DeploymentDto?> GetDeploymentAsync(Guid deploymentId, Guid tenantId)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.Deployment>();
            var deployment = await repository.GetByIdAsync(deploymentId, tenantId);
            return deployment != null ? _mapper.Map<DeploymentDto>(deployment) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deployment {DeploymentId} for tenant {TenantId}", deploymentId, tenantId);
            throw;
        }
    }

    public async Task<IEnumerable<DeploymentDto>> GetDeploymentsAsync(Guid tenantId, int page, int pageSize)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.Deployment>();
            var deployments = await repository.GetPagedAsync(page, pageSize, tenantId);
            return _mapper.Map<IEnumerable<DeploymentDto>>(deployments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deployments for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<DeploymentDto> UpdateDeploymentStatusAsync(Guid deploymentId, DeploymentStatus status, string? errorMessage = null)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.Deployment>();
            var deployment = await repository.GetByIdAsync(deploymentId);
            
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            deployment.Status = status;
            deployment.ErrorMessage = errorMessage;

            switch (status)
            {
                case DeploymentStatus.Running:
                    deployment.StartedAt = DateTime.UtcNow;
                    break;
                case DeploymentStatus.Succeeded:
                case DeploymentStatus.Failed:
                case DeploymentStatus.Cancelled:
                    deployment.CompletedAt = DateTime.UtcNow;
                    break;
            }

            var updatedDeployment = await repository.UpdateAsync(deployment);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Updated deployment {DeploymentId} status to {Status}", deploymentId, status);
            return _mapper.Map<DeploymentDto>(updatedDeployment);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error updating deployment {DeploymentId} status", deploymentId);
            throw;
        }
    }

    public async Task<bool> CancelDeploymentAsync(Guid deploymentId, Guid tenantId)
    {
        try
        {
            var deployment = await GetDeploymentAsync(deploymentId, tenantId);
            if (deployment == null || deployment.Status != DeploymentStatus.Running)
            {
                return false;
            }

            // Cancel the actual Azure deployment if it exists
            if (!string.IsNullOrEmpty(deployment.DeploymentId))
            {
                try
                {
                    var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{deployment.SubscriptionId}"));
                    var resourceGroups = subscription.GetResourceGroups();
                    var resourceGroup = await resourceGroups.GetAsync(deployment.ResourceGroupName);
                    var deployments = resourceGroup.Value.GetArmDeployments();
                    var azureDeployment = await deployments.GetAsync(deployment.DeploymentId);
                    
                    await azureDeployment.Value.CancelAsync();
                    _logger.LogInformation("Cancelled Azure deployment {AzureDeploymentId}", deployment.DeploymentId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cancel Azure deployment {AzureDeploymentId}, updating status only", deployment.DeploymentId);
                }
            }

            await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.Cancelled);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling deployment {DeploymentId} for tenant {TenantId}", deploymentId, tenantId);
            throw;
        }
    }

    public async Task<DeploymentDto> StartDeploymentAsync(Guid deploymentId, Guid tenantId)
    {
        try
        {
            var deployment = await GetDeploymentAsync(deploymentId, tenantId);
            if (deployment == null)
            {
                throw new ArgumentException($"Deployment {deploymentId} not found");
            }

            if (deployment.Status != DeploymentStatus.Pending)
            {
                throw new InvalidOperationException($"Deployment {deploymentId} is not in pending status");
            }

            // Get the template
            var template = await _templateService.GetTemplateAsync(deployment.TemplateId, tenantId);
            if (template == null)
            {
                throw new ArgumentException($"Template {deployment.TemplateId} not found");
            }

            // Update status to running
            deployment = await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.Running);

            // Start the actual Azure deployment asynchronously
            _ = Task.Run(async () => await ExecuteAzureDeploymentAsync(deployment, template));

            _logger.LogInformation("Started deployment {DeploymentId} for tenant {TenantId}", deploymentId, tenantId);
            return deployment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting deployment {DeploymentId} for tenant {TenantId}", deploymentId, tenantId);
            await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.Failed, ex.Message);
            throw;
        }
    }

    public async Task<object?> GetDeploymentOutputsAsync(Guid deploymentId, Guid tenantId)
    {
        try
        {
            var deployment = await GetDeploymentAsync(deploymentId, tenantId);
            if (deployment == null || string.IsNullOrEmpty(deployment.DeploymentId))
            {
                return null;
            }

            var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{deployment.SubscriptionId}"));
            var resourceGroups = subscription.GetResourceGroups();
            var resourceGroup = await resourceGroups.GetAsync(deployment.ResourceGroupName);
            var deployments = resourceGroup.Value.GetArmDeployments();
            var azureDeployment = await deployments.GetAsync(deployment.DeploymentId);

            var properties = azureDeployment.Value.Data.Properties;
            return properties?.Outputs?.ToObjectFromJson<object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deployment outputs for {DeploymentId}", deploymentId);
            throw;
        }
    }

    private async Task ExecuteAzureDeploymentAsync(DeploymentDto deployment, TemplateDto template)
    {
        try
        {
            _logger.LogInformation("Executing Azure deployment {DeploymentId}", deployment.Id);

            // Parse template and parameters
            var templateContent = BinaryData.FromString(template.TemplateContent);
            var parametersContent = string.IsNullOrEmpty(deployment.ParametersJson) 
                ? BinaryData.FromString("{}") 
                : BinaryData.FromString(deployment.ParametersJson);

            // Get subscription and resource group
            var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{deployment.SubscriptionId}"));
            var resourceGroups = subscription.GetResourceGroups();
            var resourceGroup = await resourceGroups.GetAsync(deployment.ResourceGroupName);

            // Create deployment content
            var deploymentContent = new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = templateContent,
                Parameters = parametersContent
            });

            // Start the deployment
            var deployments = resourceGroup.Value.GetArmDeployments();
            var operation = await deployments.CreateOrUpdateAsync(Azure.WaitUntil.Started, deployment.Name, deploymentContent);

            // Update with Azure deployment ID
            await UpdateDeploymentWithAzureIdAsync(deployment.Id, operation.Value.Data.Name);

            // Wait for completion
            var result = await operation.WaitForCompletionAsync();

            // Update final status
            var finalStatus = DeploymentStatus.Failed;
            if (result.Value.Data.Properties?.ProvisioningState == ResourcesProvisioningState.Succeeded)
                finalStatus = DeploymentStatus.Succeeded;
            else if (result.Value.Data.Properties?.ProvisioningState == ResourcesProvisioningState.Canceled)
                finalStatus = DeploymentStatus.Cancelled;
            else if (result.Value.Data.Properties?.ProvisioningState == ResourcesProvisioningState.Failed)
                finalStatus = DeploymentStatus.Failed;

            string? errorMessage = null;
            if (finalStatus == DeploymentStatus.Failed)
            {
                errorMessage = result.Value.Data.Properties?.Error?.Message ?? "Deployment failed";
            }

            await UpdateDeploymentStatusAsync(deployment.Id, finalStatus, errorMessage);

            _logger.LogInformation("Completed Azure deployment {DeploymentId} with status {Status}", deployment.Id, finalStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Azure deployment {DeploymentId}", deployment.Id);
            await UpdateDeploymentStatusAsync(deployment.Id, DeploymentStatus.Failed, ex.Message);
        }
    }

    private async Task UpdateDeploymentWithAzureIdAsync(Guid deploymentId, string azureDeploymentId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.Deployment>();
            var deployment = await repository.GetByIdAsync(deploymentId);
            
            if (deployment != null)
            {
                deployment.DeploymentId = azureDeploymentId;
                await repository.UpdateAsync(deployment);
                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error updating deployment {DeploymentId} with Azure ID", deploymentId);
        }
    }
}