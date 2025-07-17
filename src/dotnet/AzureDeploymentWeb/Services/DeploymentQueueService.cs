using System.Collections.Concurrent;
using AzureDeploymentWeb.Models;

namespace AzureDeploymentWeb.Services
{
    public class DeploymentQueueService : IDeploymentQueueService
    {
        private readonly ConcurrentQueue<DeploymentJob> _queue = new();
        private readonly ILogger<DeploymentQueueService> _logger;

        public DeploymentQueueService(ILogger<DeploymentQueueService> logger)
        {
            _logger = logger;
        }

        public void EnqueueJob(DeploymentJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            _queue.Enqueue(job);
            _logger.LogInformation("Enqueued deployment job {JobId} for deployment {DeploymentName} by user {UserName}", 
                job.JobId, job.DeploymentName, job.UserName);
        }

        public bool TryDequeueJob(out DeploymentJob? job)
        {
            var result = _queue.TryDequeue(out job);
            if (result && job != null)
            {
                _logger.LogInformation("Dequeued deployment job {JobId} for deployment {DeploymentName}", 
                    job.JobId, job.DeploymentName);
            }
            return result;
        }

        public int GetQueueCount()
        {
            return _queue.Count;
        }

        public IEnumerable<DeploymentJob> GetPendingJobs()
        {
            return _queue.ToArray();
        }
    }
}