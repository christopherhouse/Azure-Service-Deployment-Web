using Azure.Core;

namespace AzureDeploymentWeb.Services
{
    public interface IUserTokenService
    {
        Task<TokenCredential?> GetUserTokenCredentialAsync();
        Task<string?> GetAccessTokenAsync();
    }
}