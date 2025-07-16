# 🚀 Azure ARM Template Deployment Web

A modern .NET 8 MVC web application that simplifies Azure resource deployment through ARM templates with a beautiful, user-friendly interface.

![Homepage](https://github.com/user-attachments/assets/e7ff3081-68ed-4e06-93e3-047380c17e41)

## 🌟 What This Repository Contains

This repository provides a complete solution for deploying Azure resources through ARM templates with:

- **🎨 Modern Web Interface**: Clean, intuitive UI for uploading and deploying ARM templates
- **🔐 Azure AD Authentication**: Secure Microsoft Entra ID integration with OAuth 2.0
- **⚡ Real-time Deployment Tracking**: Live updates using SignalR during deployments
- **🏗️ Infrastructure as Code**: Complete Azure Bicep templates for the entire infrastructure
- **🚀 CI/CD Pipeline**: Automated GitHub Actions workflow for seamless deployments
- **🧪 Comprehensive Testing**: Unit tests and Playwright E2E tests included

## 📸 Application Screenshots

### Homepage - Authentication Required
![Homepage](https://github.com/user-attachments/assets/e7ff3081-68ed-4e06-93e3-047380c17e41)

### Deployment Interface
![Deployment Page](https://github.com/user-attachments/assets/6b6ce7be-02ec-48c9-8cb2-6d648e5160e3)

## 🏗️ Architecture & Technical Overview

### Application Architecture
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   User Browser  │───▶│  ASP.NET Core    │───▶│  Azure Resource │
│                 │    │  MVC Web App     │    │  Manager API    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         │              ┌────────▼────────┐             │
         └──────────────▶│  Azure AD       │◀────────────┘
                        │  Authentication │
                        └─────────────────┘
                                 │
                        ┌────────▼────────┐
                        │   Azure SignalR │
                        │   (Real-time)   │
                        └─────────────────┘
```

### Technology Stack
- **Frontend**: ASP.NET Core MVC with Bootstrap 5, JavaScript, SignalR
- **Backend**: .NET 8, Azure SDK for .NET, Microsoft Identity Web
- **Authentication**: Microsoft Entra ID (Azure AD) with OAuth 2.0
- **Real-time Communication**: Azure SignalR Service
- **Infrastructure**: Azure Bicep templates
- **CI/CD**: GitHub Actions with OIDC authentication
- **Testing**: XUnit (40 unit tests), Playwright E2E tests

## 📁 Project Structure

```
📦 Azure-Service-Deployment-Web
├── 🎯 src/dotnet/AzureDeploymentWeb/     # Main .NET 8 MVC Application
│   ├── Controllers/                      # MVC Controllers
│   ├── Views/                           # Razor Views with Bootstrap UI
│   ├── Services/                        # Azure integration services
│   ├── Models/                          # Data models and view models
│   ├── Hubs/                           # SignalR hubs for real-time updates
│   └── wwwroot/                        # Static files (CSS, JS, images)
├── 🧪 src/dotnet/AzureDeploymentWeb.Tests/ # Unit Tests (XUnit)
├── 🏗️ infra/bicep/                      # Infrastructure as Code
│   ├── main.bicep                       # Main Bicep template
│   ├── modules/                         # Reusable Bicep modules
│   └── parameters/                      # Environment-specific parameters
├── 🎭 tests/playwright/                  # E2E Tests
│   ├── tests/                          # Playwright test files
│   ├── fixtures/                       # Test data and helpers
│   └── playwright.config.ts            # Playwright configuration
├── 📚 examples/                          # Sample ARM templates
├── 🚀 .github/workflows/                 # CI/CD Pipeline
└── 📖 docs/                             # Documentation and images
```

## 🛠️ Application Components

### Core Services
- **`IAzureDeploymentService`**: Handles ARM template deployments
- **`IAzureResourceDiscoveryService`**: Discovers Azure subscriptions and resource groups
- **`DeploymentMonitoringService`**: Background service for tracking deployment status
- **`DeploymentHub`**: SignalR hub for real-time deployment updates

### Key Features
- **📁 File Upload**: Drag-and-drop ARM templates and parameter files
- **🔍 Resource Discovery**: Automatic subscription and resource group discovery
- **📊 Progress Tracking**: Real-time deployment status with detailed logging
- **🛡️ Security**: Secure file validation and Azure RBAC integration
- **🎨 Modern UI**: Responsive design with Bootstrap 5 and custom styling

## 🚀 Quick Start Guide

### Prerequisites
- .NET 8 SDK
- Azure subscription
- Azure AD app registration
- Git

### 1. Clone the Repository
```bash
git clone https://github.com/christopherhouse/Azure-Service-Deployment-Web.git
cd Azure-Service-Deployment-Web
```

### 2. Configure Azure AD Authentication

1. **Create Azure AD App Registration**:
   - Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
   - Click "New registration"
   - Name: "Azure ARM Deployment Tool"
   - Redirect URI: `https://localhost:5001/signin-oidc` (for local development)

2. **Configure Application Settings**:
   ```bash
   cd src/dotnet/AzureDeploymentWeb
   cp appsettings.example.json appsettings.Development.json
   ```

3. **Update `appsettings.Development.json`**:
   ```json
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/",
       "ClientId": "your-client-id-here",
       "ClientSecret": "your-client-secret-here",
       "TenantId": "your-tenant-id-here",
       "CallbackPath": "/signin-oidc"
     }
   }
   ```

### 3. Run the Application
```bash
# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

The application will be available at `https://localhost:5001`

### 4. Test with Sample ARM Templates
Use the sample templates in the `examples/` folder:
- `storage-account.json`: Creates an Azure Storage Account
- `storage-account-parameters.json`: Parameters for the storage account

## 🔄 CI/CD Deployment

### Automated Deployment with GitHub Actions

This repository includes a complete CI/CD pipeline that deploys both infrastructure and application to Azure.

#### Setup GitHub Actions Deployment

1. **Create Azure Service Principal with OIDC**:
   ```bash
   # Create service principal
   az ad sp create-for-rbac --name "GitHub-Actions-Azure-ARM-Deploy" \
     --role contributor \
     --scopes /subscriptions/{your-subscription-id}

   # Create federated credential
   az ad app federated-credential create \
     --id {service-principal-app-id} \
     --parameters '{
       "name": "GitHub-Actions-OIDC",
       "issuer": "https://token.actions.githubusercontent.com",
       "subject": "repo:{your-github-username}/Azure-Service-Deployment-Web:ref:refs/heads/main",
       "description": "GitHub Actions OIDC for Azure ARM Deployment",
       "audiences": ["api://AzureADTokenExchange"]
     }'
   ```

2. **Configure GitHub Secrets**:
   | Secret Name | Description |
   |-------------|-------------|
   | `AZURE_CLIENT_ID` | Service principal client ID for GitHub Actions Azure authentication |
   | `AZURE_TENANT_ID` | Azure tenant ID |
   | `AZURE_SUBSCRIPTION_ID` | Target Azure subscription |
   | `AZURE_AD_CLIENT_SECRET` | Web app authentication client secret |

3. **Deploy**:
   - Push to `main` branch triggers automatic deployment
   - Or manually trigger from GitHub Actions tab

The workflow will:
- ✅ Build and test the .NET application
- 🏗️ Deploy Azure infrastructure using Bicep
- 🚀 Deploy the web application to Azure App Service
- 🧪 Run Playwright E2E tests against the deployed app

## 🧪 Testing

### Unit Tests (.NET)
```bash
# Run all unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### E2E Tests (Playwright)
```bash
cd tests/playwright

# Install dependencies
npm install

# Install browsers
npx playwright install

# Run tests
npm test

# Run tests with UI
npm run test:ui
```

## 🏗️ Infrastructure as Code

### Bicep Templates

The `infra/bicep/` directory contains modular Bicep templates:

```
infra/bicep/
├── main.bicep                    # Main template
├── modules/
│   ├── app-service.bicep        # App Service and plan
│   ├── app-insights.bicep       # Application Insights
│   ├── log-analytics.bicep      # Log Analytics workspace
│   └── signalr.bicep           # Azure SignalR service
└── parameters/
    ├── main.dev.bicepparam      # Development parameters
    └── main.prod.bicepparam     # Production parameters
```

### Deploy Infrastructure Manually
```bash
cd infra/bicep

# Deploy to development
az deployment group create \
  --resource-group rg-arm-deploy-dev \
  --template-file main.bicep \
  --parameters main.dev.bicepparam

# Deploy to production
az deployment group create \
  --resource-group rg-arm-deploy-prod \
  --template-file main.bicep \
  --parameters main.prod.bicepparam
```

## 🔧 Development Guide

### Adding New ARM Template Examples

1. Add your ARM template to `examples/`
2. Create corresponding parameter file
3. Update example documentation

### Extending the Application

1. **New Controllers**: Add to `Controllers/` directory
2. **New Services**: Implement in `Services/` with DI registration
3. **New Views**: Add Razor views to `Views/` directory
4. **New Models**: Add to `Models/` directory

### Custom Deployment Logic

Extend the `AzureDeploymentService`:

```csharp
public class AzureDeploymentService : IAzureDeploymentService
{
    public async Task<DeploymentResult> DeployWithValidationAsync(
        DeploymentRequest request)
    {
        // Add custom validation logic
        await ValidateTemplateAsync(request.Template);
        
        // Deploy with monitoring
        return await DeployTemplateAsync(request);
    }
}
```

## 🔐 Security Best Practices

- 🔑 **Authentication**: Azure AD integration with OAuth 2.0
- 🛡️ **Authorization**: Azure RBAC for resource access
- 🔒 **Secrets Management**: Azure Key Vault integration available
- 📝 **Input Validation**: File type and size validation
- 🌐 **HTTPS**: Enforced in production
- 🔍 **Logging**: Comprehensive audit logging

## 🚨 Troubleshooting

### Common Issues

1. **Authentication Fails**
   ```bash
   # Check Azure AD configuration
   az ad app show --id {your-client-id}
   
   # Verify redirect URIs
   az ad app update --id {your-client-id} --web-redirect-uris "https://localhost:5001/signin-oidc"
   ```

2. **Deployment Fails**
   ```bash
   # Check Azure permissions
   az role assignment list --assignee {your-user-id} --scope /subscriptions/{subscription-id}
   
   # Validate ARM template
   az deployment group validate --resource-group {rg-name} --template-file template.json
   ```

3. **Build Errors**
   ```bash
   # Clean and restore
   dotnet clean
   dotnet restore
   dotnet build
   ```

### Debug Mode

Enable detailed logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "AzureDeploymentWeb": "Debug"
    }
  }
}
```

## 📈 Monitoring & Observability

- **Application Insights**: Automatic telemetry and performance monitoring
- **Structured Logging**: Comprehensive logging with correlation IDs
- **Health Checks**: Built-in health check endpoints
- **SignalR Metrics**: Real-time connection and message metrics

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes and add tests
4. Commit: `git commit -m 'Add amazing feature'`
5. Push: `git push origin feature/amazing-feature`
6. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Useful Resources

- 📚 [Azure ARM Templates Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/)
- 🔐 [Microsoft Identity Web Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- 🚀 [Azure Resource Manager .NET SDK](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/resourcemanager-readme)
- 🏗️ [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- 🎭 [Playwright Testing Documentation](https://playwright.dev/dotnet/)