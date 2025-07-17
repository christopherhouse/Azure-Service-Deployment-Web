using AzureDeploymentWeb.Models;

namespace AzureDeploymentWeb.Services
{
    public interface IServiceBusDeploymentQueueService : IDeploymentQueueService
    {
        /// <summary>
        /// Starts processing messages from the Service Bus subscription
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartProcessingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops processing messages from the Service Bus subscription
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StopProcessingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the Service Bus queue service is enabled
        /// </summary>
        bool IsEnabled { get; }
    }
}