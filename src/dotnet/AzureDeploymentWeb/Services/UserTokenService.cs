using Azure.Core;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace AzureDeploymentWeb.Services
{
    public class UserTokenService : IUserTokenService
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserTokenService> _logger;

        private const string AzureManagementScope = "https://management.azure.com/user_impersonation";

        public UserTokenService(
            ITokenAcquisition tokenAcquisition,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<UserTokenService> logger)
        {
            _tokenAcquisition = tokenAcquisition;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TokenCredential?> GetUserTokenCredentialAsync()
        {
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return null;
            }

            return new AccessTokenCredential(accessToken);
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                // Check if authentication is configured
                if (!IsAuthenticationConfigured())
                {
                    _logger.LogDebug("Authentication not configured, returning null token");
                    return null;
                }

                var user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogDebug("User not authenticated, returning null token");
                    return null;
                }

                // Get access token for Azure Management API with user impersonation scope
                var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(
                    new[] { AzureManagementScope });

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Failed to acquire access token for user");
                    return null;
                }

                _logger.LogDebug("Successfully acquired access token for user");
                return accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acquiring access token for user");
                throw;
            }
        }

        private bool IsAuthenticationConfigured()
        {
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret);
        }
    }

    /// <summary>
    /// No-op implementation of IUserTokenService for when authentication is not configured
    /// </summary>
    public class NoOpUserTokenService : IUserTokenService
    {
        public Task<TokenCredential?> GetUserTokenCredentialAsync()
        {
            return Task.FromResult<TokenCredential?>(null);
        }

        public Task<string?> GetAccessTokenAsync()
        {
            return Task.FromResult<string?>(null);
        }
    }

    /// <summary>
    /// Simple implementation of TokenCredential that uses a pre-acquired access token
    /// </summary>
    public class AccessTokenCredential : TokenCredential
    {
        private readonly string _accessToken;

        public AccessTokenCredential(string accessToken)
        {
            _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            // For this implementation, we'll return the token with a reasonable expiry
            // In a production scenario, you might want to parse the JWT to get the actual expiry
            return new AccessToken(_accessToken, DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(GetToken(requestContext, cancellationToken));
        }
    }
}