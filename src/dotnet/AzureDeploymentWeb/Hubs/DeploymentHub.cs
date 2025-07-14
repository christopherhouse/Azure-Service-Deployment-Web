using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AzureDeploymentWeb.Hubs
{
    [Authorize]
    public class DeploymentHub : Hub
    {
        public async Task JoinUserGroup()
        {
            // Add user to their personal group based on user identity
            if (Context.User?.Identity?.Name != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{Context.User.Identity.Name}");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Remove user from their personal group
            if (Context.User?.Identity?.Name != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{Context.User.Identity.Name}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}