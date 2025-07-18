using AzureDeploymentSaaS.Shared.Contracts.Models;
using AzureDeploymentSaaS.Shared.Contracts.Services;
using AzureDeploymentSaaS.Shared.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Data = AzureDeploymentSaaS.Shared.Infrastructure.Data;

namespace Identity.Api.Services;

/// <summary>
/// Service for user management operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, Guid tenantId)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.User>();
            var user = await repository.GetByIdAsync(userId, tenantId);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId} for tenant {TenantId}", userId, tenantId);
            throw;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.User>();
            var users = await repository.FindAsync(u => u.Email == email);
            var user = users.FirstOrDefault();
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", email);
            throw;
        }
    }

    public async Task<UserDto> CreateUserAsync(UserDto userDto)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.User>();
            var user = _mapper.Map<Data.User>(userDto);
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            var createdUser = await repository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Created user {UserId} for tenant {TenantId}", createdUser.Id, createdUser.TenantId);
            return _mapper.Map<UserDto>(createdUser);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating user for tenant {TenantId}", userDto.TenantId);
            throw;
        }
    }

    public async Task<UserDto> UpdateUserAsync(UserDto userDto)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.User>();
            var user = _mapper.Map<Data.User>(userDto);

            var updatedUser = await repository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Updated user {UserId} for tenant {TenantId}", updatedUser.Id, updatedUser.TenantId);
            return _mapper.Map<UserDto>(updatedUser);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error updating user {UserId}", userDto.Id);
            throw;
        }
    }

    public async Task<bool> DeactivateUserAsync(Guid userId, Guid tenantId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.User>();
            var user = await repository.GetByIdAsync(userId, tenantId);
            
            if (user == null)
                return false;

            user.IsActive = false;
            await repository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Deactivated user {UserId} for tenant {TenantId}", userId, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error deactivating user {UserId} for tenant {TenantId}", userId, tenantId);
            throw;
        }
    }

    public async Task<IEnumerable<UserDto>> GetUsersByTenantAsync(Guid tenantId, int page, int pageSize)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.User>();
            var users = await repository.GetPagedAsync(page, pageSize, tenantId);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> ValidateUserAsync(Guid userId, Guid tenantId)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.User>();
            var user = await repository.GetByIdAsync(userId, tenantId);
            return user != null && user.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user {UserId} for tenant {TenantId}", userId, tenantId);
            throw;
        }
    }
}

/// <summary>
/// Service for tenant management operations
/// </summary>
public class TenantService : ITenantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantService> _logger;
    private readonly IMapper _mapper;

    public TenantService(IUnitOfWork unitOfWork, ILogger<TenantService> logger, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<TenantDto?> GetTenantByIdAsync(Guid tenantId)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.Tenant>();
            var tenant = await repository.GetByIdAsync(tenantId);
            return tenant != null ? _mapper.Map<TenantDto>(tenant) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<TenantDto?> GetTenantByDomainAsync(string domain)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.Tenant>();
            var tenants = await repository.FindAsync(t => t.Domain == domain);
            var tenant = tenants.FirstOrDefault();
            return tenant != null ? _mapper.Map<TenantDto>(tenant) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant by domain {Domain}", domain);
            throw;
        }
    }

    public async Task<TenantDto> CreateTenantAsync(TenantDto tenantDto)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.Tenant>();
            var tenant = _mapper.Map<Data.Tenant>(tenantDto);
            tenant.CreatedAt = DateTime.UtcNow;
            tenant.IsActive = true;

            var createdTenant = await repository.AddAsync(tenant);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Created tenant {TenantId} with domain {Domain}", createdTenant.Id, createdTenant.Domain);
            return _mapper.Map<TenantDto>(createdTenant);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating tenant with domain {Domain}", tenantDto.Domain);
            throw;
        }
    }

    public async Task<TenantDto> UpdateTenantAsync(TenantDto tenantDto)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.Tenant>();
            var tenant = _mapper.Map<Data.Tenant>(tenantDto);

            var updatedTenant = await repository.UpdateAsync(tenant);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Updated tenant {TenantId}", updatedTenant.Id);
            return _mapper.Map<TenantDto>(updatedTenant);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantDto.Id);
            throw;
        }
    }

    public async Task<bool> ActivateTenantAsync(Guid tenantId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.Tenant>();
            var tenant = await repository.GetByIdAsync(tenantId);
            
            if (tenant == null)
                return false;

            tenant.IsActive = true;
            await repository.UpdateAsync(tenant);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Activated tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error activating tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> DeactivateTenantAsync(Guid tenantId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Data.Tenant>();
            var tenant = await repository.GetByIdAsync(tenantId);
            
            if (tenant == null)
                return false;

            tenant.IsActive = false;
            await repository.UpdateAsync(tenant);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Deactivated tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error deactivating tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<IEnumerable<TenantDto>> GetAllTenantsAsync(int page, int pageSize)
    {
        try
        {
            var repository = _unitOfWork.Repository<Data.Tenant>();
            var tenants = await repository.GetPagedAsync(page, pageSize);
            return _mapper.Map<IEnumerable<TenantDto>>(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenants");
            throw;
        }
    }
}