# Azure ARM Template Deployment Tool - .NET 8 MVC

A .NET 8 MVC web application that enables you to deploy Azure resources by uploading ARM templates and parameter files through a user-friendly interface.

## 🚀 Features

- **File Upload Interface**: Upload ARM templates (.json) and parameter files (.json) using Razor views
- **Dynamic Resource Selection**: Select subscription and resource group from dropdowns populated with your available Azure resources
- **Azure Authentication**: Secure authentication using Microsoft Entra ID (Azure AD) with Microsoft.Identity.Web
- **Real-time Deployment Notifications**: SignalR-powered notifications that update automatically without page refresh
- **Visual Status Indicators**: Color-coded deployment status with success/failure indicators
- **Deployment Monitoring**: Track deployment progress with start time, duration, and resource group information
- **Notification Management**: Individual and bulk notification clearing with persistent notifications until dismissed
- **Success/Error Handling**: Clear success messages with emojis and detailed error reporting
- **Intelligent Caching**: Fast-loading dropdowns with configurable in-memory or Redis caching for Azure resource discovery
- **Configuration**: Easy configuration through appsettings.json

## 📋 Prerequisites

- .NET 8 SDK
- Azure subscription
- Azure AD app registration for authentication
- Resource group for deploying resources

## 🛠️ Setup Instructions

### 1. Azure AD App Registration

1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
2. Click "New registration"
3. Set application name (e.g., "ARM Template Deployment Tool - .NET")
4. Set redirect URI: `https://localhost:5001/signin-oidc` (for development)
5. Note the **Application (client) ID** and **Directory (tenant) ID**
6. Go to "Certificates & secrets" → "Client secrets" → "New client secret"
7. Create a new client secret and note the **secret value** (copy it immediately as it won't be shown again)

### 2. Configuration

1. Copy `appsettings.example.json` to `appsettings.json`:
   ```bash
   cp appsettings.example.json appsettings.json
   ```

2. Fill in your Azure configuration in `appsettings.json`:
   ```json
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/",
       "TenantId": "your-tenant-id-here",
       "ClientId": "your-client-id-here",
       "ClientSecret": "your-client-secret-here",
       "CallbackPath": "/signin-oidc"
     },
     "Azure": {
       "SubscriptionId": "your-subscription-id-here",
       "ResourceGroup": "your-resource-group-here"
     },
     "Cache": {
       "Provider": "Memory",
       "Redis": {
         "ConnectionString": ""
       },
       "SubscriptionsCacheDurationMinutes": 60,
       "ResourceGroupsCacheDurationMinutes": 30
     },
     "AzureSignalR": {
       "ConnectionString": ""
     }
   }
   ```

   **Cache Configuration**:
   - `Provider`: Set to "Memory" for in-memory caching or "Redis" for Azure Redis Cache
   - `Redis.ConnectionString`: Connection string for Azure Redis Cache (required when Provider is "Redis")
   - `SubscriptionsCacheDurationMinutes`: How long to cache subscription lists (default: 60 minutes)
   - `ResourceGroupsCacheDurationMinutes`: How long to cache resource group lists (default: 30 minutes)

   **Note**: The `AzureSignalR` section is optional. If no connection string is provided, the application will use local SignalR. To use Azure SignalR Service for production scaling, provide the connection string from your Azure SignalR Service instance.

### 3. Run the Application

```bash
dotnet run
```

The app will be available at `https://localhost:5001`

## 🎯 How to Use

1. **Sign In**: Click "Sign in with Microsoft" to authenticate with your Azure account
2. **Select Resources**: 
   - Choose your subscription from the dropdown
   - Select your target resource group from the dropdown (populated after subscription selection)
3. **Upload Files**: 
   - Select your ARM template file (.json)
   - Select your parameters file (.json)
4. **Deploy**: Click "Deploy to Azure" to start the deployment
5. **Monitor**: Watch the real-time status indicator during deployment
6. **Results**: View success message with emojis or detailed error information

## 📁 Project Structure

```
src/dotnet/AzureDeploymentWeb/
├── Controllers/
│   ├── HomeController.cs              # Main controller with authentication check
│   ├── DeploymentController.cs        # Deployment functionality
│   ├── AzureResourcesController.cs    # API endpoints for Azure resource discovery
│   └── CacheController.cs             # Cache management and diagnostics
├── Models/
│   ├── ErrorViewModel.cs              # Error handling model
│   ├── DeploymentViewModel.cs         # Deployment forms and status models
│   └── CacheOptions.cs               # Cache configuration model
├── Services/
│   ├── AzureDeploymentService.cs      # Azure ARM deployment logic
│   ├── AzureResourceDiscoveryService.cs # Azure resource discovery with caching
│   └── DeploymentMonitoringService.cs # Background deployment monitoring
├── Views/
│   ├── Home/
│   │   └── Index.cshtml           # Landing page
│   ├── Deployment/
│   │   └── Index.cshtml           # Main deployment interface
│   └── Shared/
│       ├── _Layout.cshtml         # Main layout with styling
│       └── _LoginPartial.cshtml   # Authentication UI
├── Program.cs                     # Application configuration
├── appsettings.json              # Configuration file
└── appsettings.example.json      # Configuration template
```

## 🔧 Technologies Used

- **ASP.NET Core 8.0**: Web framework
- **SignalR**: Real-time web functionality for deployment notifications (local or Azure SignalR Service)
- **Microsoft.Identity.Web**: Azure AD authentication
- **Azure.ResourceManager**: Azure resource management
- **IDistributedCache**: Caching abstraction with Memory and Redis providers
- **Razor Views**: Server-side UI rendering
- **Bootstrap 5**: UI styling

## 🔐 Security Considerations

- **Confidential Client**: The application is configured as a confidential client with client secret for enhanced security
- **Client Secret**: Store client secret securely (use Azure Key Vault or environment variables in production)
- **Environment Variables**: Never commit `appsettings.json` files with real credentials
- **HTTPS**: Use HTTPS in production (update redirect URI accordingly)
- **Permissions**: Ensure Azure AD app has minimal required permissions
- **Resource Group**: Use a dedicated resource group for testing

## 🧪 Testing

Run tests:
```bash
dotnet test
```

Build for production:
```bash
dotnet build --configuration Release
```

## 🚀 Deployment

### Development
```bash
dotnet run
```

### Production Build
```bash
dotnet publish --configuration Release
```

The build artifacts will be in the `bin/Release/net8.0/publish/` directory.

## 📡 API Endpoints

The application provides REST API endpoints for resource discovery:

- `GET /api/AzureResources/subscriptions` - Get available subscriptions
- `GET /api/AzureResources/resourcegroups/{subscriptionId}` - Get resource groups for a subscription
- `POST /api/Cache/clear` - Clear cached data (requires authentication)
- `GET /api/Cache/info` - Get cache configuration information (requires authentication)

### Cache Management

Use the cache management endpoints to clear cached data when needed:

```bash
# Clear subscriptions cache
curl -X POST https://localhost:5001/api/Cache/clear \
  -H "Content-Type: application/json" \
  -d '{"clearSubscriptions": true}'

# Clear resource groups cache for a specific subscription
curl -X POST https://localhost:5001/api/Cache/clear \
  -H "Content-Type: application/json" \
  -d '{"clearResourceGroups": true, "subscriptionId": "your-subscription-id"}'
```

## ⚡ Performance Features

### Intelligent Caching
- **Subscriptions**: Cached for 60 minutes (configurable) since they rarely change
- **Resource Groups**: Cached for 30 minutes (configurable) to balance freshness and performance
- **Cache Providers**: Support for both in-memory (development) and Redis (production scaling)
- **Cache Keys**: Subscription-specific caching to prevent cross-subscription data leakage

### Configuration
```json
{
  "Cache": {
    "Provider": "Memory|Redis",
    "Redis": {
      "ConnectionString": "your-redis-connection-string"
    },
    "SubscriptionsCacheDurationMinutes": 60,
    "ResourceGroupsCacheDurationMinutes": 30
  }
}

## ⚠️ Important Notes

- This implementation includes a demo Azure deployment service that simulates deployments
- For production use, implement full Azure Resource Manager integration using the Azure.ResourceManager SDK
- The current implementation validates authentication and file uploads but uses mock deployment for demonstration

## 🔗 Related Links

- [Azure ARM Templates Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/)
- [Microsoft.Identity.Web Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [Azure Resource Manager SDK](https://docs.microsoft.com/en-us/dotnet/api/azure.resourcemanager)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)