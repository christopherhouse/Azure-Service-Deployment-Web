using AzureDeploymentWeb.Models;

namespace AzureDeploymentWeb.Services
{
    public interface IDeploymentQueueService
    {
        /// <summary>
        /// Enqueues a deployment job for processing
        /// </summary>
        /// <param name="job">The deployment job to enqueue</param>
        void EnqueueJob(DeploymentJob job);

        /// <summary>
        /// Attempts to dequeue a deployment job for processing
        /// </summary>
        /// <param name="job">The dequeued job, if available</param>
        /// <returns>True if a job was dequeued, false if queue is empty</returns>
        bool TryDequeueJob(out DeploymentJob? job);

        /// <summary>
        /// Gets the current number of jobs in the queue
        /// </summary>
        /// <returns>Number of pending jobs</returns>
        int GetQueueCount();

        /// <summary>
        /// Gets all pending jobs (for monitoring purposes)
        /// </summary>
        /// <returns>Collection of pending jobs</returns>
        IEnumerable<DeploymentJob> GetPendingJobs();
    }
}