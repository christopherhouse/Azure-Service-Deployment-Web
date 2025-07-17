using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureDeploymentWeb.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DeploymentStatus
    {
        Unknown,
        Queued,
        Started,
        Accepted,
        Running,
        Succeeded,
        Failed,
        Canceled,
        Error
    }

    public static class DeploymentStatusExtensions
    {
        public static DeploymentStatus FromString(string status)
        {
            return status?.ToLower() switch
            {
                "succeeded" => DeploymentStatus.Succeeded,
                "failed" => DeploymentStatus.Failed,
                "running" => DeploymentStatus.Running,
                "accepted" => DeploymentStatus.Accepted,
                "canceled" => DeploymentStatus.Canceled,
                "queued" => DeploymentStatus.Queued,
                "error" => DeploymentStatus.Error,
                _ => DeploymentStatus.Unknown
            };
        }
    }

    public class DeploymentViewModel
    {
        [Required(ErrorMessage = "ARM template file is required")]
        [Display(Name = "ARM Template File")]
        public IFormFile? TemplateFile { get; set; }

        [Required(ErrorMessage = "Parameters file is required")]
        [Display(Name = "Parameters File")]
        public IFormFile? ParametersFile { get; set; }

        [Required(ErrorMessage = "Please select a subscription")]
        [Display(Name = "Subscription")]
        public string? SelectedSubscriptionId { get; set; }

        [Required(ErrorMessage = "Please select a resource group")]
        [Display(Name = "Resource Group")]
        public string? SelectedResourceGroupName { get; set; }

        public DeploymentStatus? DeploymentStatus { get; set; }
        public string? DeploymentMessage { get; set; }
        public string? DeploymentName { get; set; }
        
        // For backwards compatibility - these will be populated from the selected values
        public string? ResourceGroup => SelectedResourceGroupName;
        public string? SubscriptionId => SelectedSubscriptionId;
    }

    public class DeploymentStatusViewModel
    {
        public string? DeploymentName { get; set; }
        public DeploymentStatus? Status { get; set; }
        public string? Message { get; set; }
        public string? ResourceGroup { get; set; }
        public bool IsSuccessful { get; set; }
        public bool IsRunning { get; set; }
        public bool HasError { get; set; }
    }

    public class DeploymentNotification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DeploymentName { get; set; } = string.Empty;
        public DeploymentStatus Status { get; set; } = DeploymentStatus.Unknown;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
        public string ResourceGroup { get; set; } = string.Empty;
        public string? Message { get; set; }
        public bool IsSuccessful => Status == DeploymentStatus.Succeeded;
        public bool IsRunning => Status == DeploymentStatus.Running || Status == DeploymentStatus.Accepted || Status == DeploymentStatus.Started;
        public bool HasError => Status == DeploymentStatus.Failed || Status == DeploymentStatus.Canceled || Status == DeploymentStatus.Error;
        public bool IsCompleted => IsSuccessful || HasError;
    }
}