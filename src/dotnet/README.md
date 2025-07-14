# Azure ARM Template Deployment Tool - .NET 8 MVC

A .NET 8 MVC web application that enables you to deploy Azure resources by uploading ARM templates and parameter files through a user-friendly interface.

## 🚀 Features

- **File Upload Interface**: Upload ARM templates (.json) and parameter files (.json) using Razor views
- **Azure Authentication**: Secure authentication using Microsoft Entra ID (Azure AD) with Microsoft.Identity.Web
- **Real-time Deployment Status**: Visual feedback during deployment with loading indicators
- **Success/Error Handling**: Clear success messages with emojis and detailed error reporting
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
     }
   }
   ```

### 3. Run the Application

```bash
dotnet run
```

The app will be available at `https://localhost:5001`

## 🎯 How to Use

1. **Sign In**: Click "Sign in with Microsoft" to authenticate with your Azure account
2. **Upload Files**: 
   - Select your ARM template file (.json)
   - Select your parameters file (.json)
3. **Deploy**: Click "Deploy to Azure" to start the deployment
4. **Monitor**: Watch the real-time status indicator during deployment
5. **Results**: View success message with emojis or detailed error information

## 📁 Project Structure

```
src/dotnet/AzureDeploymentWeb/
├── Controllers/
│   ├── HomeController.cs          # Main controller with authentication check
│   └── DeploymentController.cs    # Deployment functionality
├── Models/
│   ├── ErrorViewModel.cs          # Error handling model
│   └── DeploymentViewModel.cs     # Deployment forms and status models
├── Services/
│   └── AzureDeploymentService.cs  # Azure ARM deployment logic
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
- **Microsoft.Identity.Web**: Azure AD authentication
- **Azure.ResourceManager**: Azure resource management
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

## ⚠️ Important Notes

- This implementation includes a demo Azure deployment service that simulates deployments
- For production use, implement full Azure Resource Manager integration using the Azure.ResourceManager SDK
- The current implementation validates authentication and file uploads but uses mock deployment for demonstration

## 🔗 Related Links

- [Azure ARM Templates Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/)
- [Microsoft.Identity.Web Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [Azure Resource Manager SDK](https://docs.microsoft.com/en-us/dotnet/api/azure.resourcemanager)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)