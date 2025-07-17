namespace AzureDeploymentSaaS.Shared.Contracts.Models;

/// <summary>
/// Represents a user in the system
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Represents a tenant/organization in the system
/// </summary>
public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; }
    public int MaxUsers { get; set; }
    public int MaxTemplates { get; set; }
}

/// <summary>
/// Represents an ARM template in the library
/// </summary>
public class TemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TemplateContent { get; set; } = string.Empty;
    public string? ParametersContent { get; set; }
    public List<string> Tags { get; set; } = new();
    public Guid TenantId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public int Version { get; set; }
    public bool IsPublic { get; set; }
}

/// <summary>
/// Represents a deployment request
/// </summary>
public class DeploymentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public string ParametersJson { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public DeploymentStatus Status { get; set; }
    public Guid TenantId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DeploymentId { get; set; }
}

/// <summary>
/// Subscription plans available
/// </summary>
public enum SubscriptionPlan
{
    Free = 0,
    Basic = 1,
    Professional = 2,
    Enterprise = 3
}

/// <summary>
/// Deployment status enumeration
/// </summary>
public enum DeploymentStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4
}
