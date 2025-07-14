using System.ComponentModel.DataAnnotations;

namespace AzureDeploymentWeb.Models
{
    public class DeploymentViewModel
    {
        [Required(ErrorMessage = "ARM template file is required")]
        [Display(Name = "ARM Template File")]
        public IFormFile? TemplateFile { get; set; }

        [Required(ErrorMessage = "Parameters file is required")]
        [Display(Name = "Parameters File")]
        public IFormFile? ParametersFile { get; set; }

        public string? DeploymentStatus { get; set; }
        public string? DeploymentMessage { get; set; }
        public string? DeploymentName { get; set; }
        public string? ResourceGroup { get; set; }
        public string? SubscriptionId { get; set; }
    }

    public class DeploymentStatusViewModel
    {
        public string? DeploymentName { get; set; }
        public string? Status { get; set; }
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
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
        public string ResourceGroup { get; set; } = string.Empty;
        public string? Message { get; set; }
        public bool IsSuccessful => Status == "Succeeded";
        public bool IsRunning => Status == "Running" || Status == "Accepted" || Status == "Creating";
        public bool HasError => Status == "Failed" || Status == "Canceled";
        public bool IsCompleted => IsSuccessful || HasError;
    }
}