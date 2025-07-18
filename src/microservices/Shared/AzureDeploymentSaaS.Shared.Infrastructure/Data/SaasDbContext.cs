using Microsoft.EntityFrameworkCore;
using AzureDeploymentSaaS.Shared.Contracts.Models;

namespace AzureDeploymentSaaS.Shared.Infrastructure.Data;

/// <summary>
/// Main database context for the SaaS application
/// </summary>
public class SaasDbContext : DbContext
{
    public SaasDbContext(DbContextOptions<SaasDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<Template> Templates { get; set; } = null!;
    public DbSet<Deployment> Deployments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Cosmos DB partition keys
        modelBuilder.HasDefaultContainer("SaasData");

        // Configure Tenant entity
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasPartitionKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Domain).IsUnique();
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasPartitionKey(e => e.TenantId);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            
            // Relationship with Tenant
            entity.HasOne<Tenant>()
                  .WithMany()
                  .HasForeignKey(e => e.TenantId);
        });

        // Configure Template entity
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasPartitionKey(e => e.TenantId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TemplateContent).IsRequired();
            
            // Relationship with Tenant
            entity.HasOne<Tenant>()
                  .WithMany()
                  .HasForeignKey(e => e.TenantId);
                  
            // Relationship with User (CreatedBy)
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy);
        });

        // Configure Deployment entity
        modelBuilder.Entity<Deployment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasPartitionKey(e => e.TenantId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SubscriptionId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.ResourceGroupName).IsRequired().HasMaxLength(90);
            entity.Property(e => e.ParametersJson).IsRequired();
            
            // Relationships
            entity.HasOne<Tenant>()
                  .WithMany()
                  .HasForeignKey(e => e.TenantId);
                  
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy);
                  
            entity.HasOne<Template>()
                  .WithMany()
                  .HasForeignKey(e => e.TemplateId);
        });
    }
}

/// <summary>
/// Entity models for database persistence
/// </summary>
public class User
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

public class Tenant
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

public class Template
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

public class Deployment
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