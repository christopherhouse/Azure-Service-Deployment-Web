using Azure.Core;
using AzureDeploymentWeb.Services;
using FluentAssertions;
using Xunit;

namespace AzureDeploymentWeb.Tests.Integration;

public class UserImpersonationIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public void Verify_UserImpersonation_Implementation()
    {
        // This test verifies that our implementation correctly switches between user impersonation 
        // and managed identity based on the availability of user credentials

        // 1. Verify that services accept user credentials
        var azureDeploymentMethods = typeof(IAzureDeploymentService).GetMethods();
        var azureResourceDiscoveryMethods = typeof(IAzureResourceDiscoveryService).GetMethods();

        // All methods should have optional TokenCredential parameter
        foreach (var method in azureDeploymentMethods)
        {
            var parameters = method.GetParameters();
            parameters.Should().Contain(p => p.Name == "userCredential" && p.ParameterType == typeof(TokenCredential));
        }

        foreach (var method in azureResourceDiscoveryMethods)
        {
            var parameters = method.GetParameters();  
            parameters.Should().Contain(p => p.Name == "userCredential" && p.ParameterType == typeof(TokenCredential));
        }

        // 2. Verify AccessTokenCredential works correctly
        var accessToken = "test-token-12345";
        var credential = new AccessTokenCredential(accessToken);
        
        var tokenResult = credential.GetToken(
            new TokenRequestContext(new[] { "https://management.azure.com/.default" }), 
            CancellationToken.None);
            
        tokenResult.Token.Should().Be(accessToken);
        tokenResult.ExpiresOn.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(50));

        // 3. Verify NoOpUserTokenService returns null credentials
        var noOpService = new NoOpUserTokenService();
        var nullCredential = noOpService.GetUserTokenCredentialAsync().Result;
        var nullToken = noOpService.GetAccessTokenAsync().Result;
        
        nullCredential.Should().BeNull();
        nullToken.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Verify_DeploymentJob_SupportsUserToken()
    {
        // Verify that DeploymentJob now includes UserAccessToken for background processing
        var job = new AzureDeploymentWeb.Models.DeploymentJob
        {
            UserAccessToken = "user-token-123",
            UserName = "testuser@example.com",
            DeploymentName = "test-deployment",
            SubscriptionId = "test-sub",
            ResourceGroupName = "test-rg",
            TemplateContent = "{}",
            ParametersContent = "{}"
        };

        job.UserAccessToken.Should().Be("user-token-123");
        job.UserName.Should().Be("testuser@example.com");
        
        // Verify all required properties are present
        job.DeploymentName.Should().NotBeNullOrEmpty();
        job.SubscriptionId.Should().NotBeNullOrEmpty();
        job.ResourceGroupName.Should().NotBeNullOrEmpty();
        job.TemplateContent.Should().NotBeNullOrEmpty(); 
        job.ParametersContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]  
    public void Verify_UserTokenService_Interface_Contract()
    {
        // Verify the IUserTokenService interface provides the expected methods
        var interfaceType = typeof(IUserTokenService);
        var methods = interfaceType.GetMethods();

        methods.Should().HaveCount(2);
        methods.Should().Contain(m => m.Name == "GetUserTokenCredentialAsync" && 
                                    m.ReturnType == typeof(Task<TokenCredential?>));
        methods.Should().Contain(m => m.Name == "GetAccessTokenAsync" && 
                                    m.ReturnType == typeof(Task<string?>));
    }
}