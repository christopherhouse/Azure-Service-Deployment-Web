# 🚀 Azure Service Deployment SaaS

A modern, cloud-native SaaS platform for deploying Azure resources through ARM templates with microservices architecture, multi-tenant support, and AI-powered template discovery.

![SaaS Architecture](docs/images/saas-architecture-diagram.svg)

## 🌟 What This Repository Contains

This repository provides a complete SaaS solution for Azure resource deployment featuring:

### 🏗️ **Modern SaaS Architecture**
- **🔬 Microservices**: Domain-driven .NET 8 APIs containerized for Azure Container Apps
- **⚛️ React SPA Frontend**: Modern web interface with Microsoft Authentication Library (MSAL)
- **🏢 Multi-Tenant Design**: Secure tenant isolation with Azure Cosmos DB partitioning
- **🤖 AI-Powered Search**: Azure AI Search for intelligent template discovery
- **📱 Self-Service Registration**: Microsoft Entra External ID integration

### 🎯 **Core Business Capabilities**
- **📚 Template Library**: Create, store, and manage ARM templates with version control
- **🔍 Intelligent Search**: Find templates by content using "serverFarmId", resource types, etc.
- **🖥️ Browser Editor**: Monaco-based ARM template editing (coming soon)
- **⚡ Real-time Deployments**: Live deployment status with SignalR
- **💳 Subscription Management**: Built-in billing with mock feature flags
- **👥 Account Management**: Tenant administration and user permissions

### ☁️ **Cloud-Native Infrastructure** 
- **🐳 Containerized Services**: All components run on Azure Container Apps
- **⚙️ Centralized Configuration**: Azure App Configuration with feature flags
- **🔐 Secure by Design**: Managed Identity and Azure RBAC integration
- **📊 Observability**: Application Insights telemetry and monitoring
- **🏗️ Infrastructure as Code**: Complete Bicep templates for deployment

## 📁 Project Structure

```
📦 Azure-Service-Deployment-SaaS
├── 🎯 src/microservices/                    # SaaS Microservices Architecture
│   ├── Services/
│   │   ├── Identity.Api/                    # User registration & authentication
│   │   ├── TemplateLibrary.Api/             # ARM template management & AI search
│   │   ├── Deployment.Api/                  # Azure deployment orchestration
│   │   ├── Billing.Api/                     # Subscription & usage management
│   │   └── AccountManagement.Api/           # Tenant & user administration
│   └── Shared/
│       ├── AzureDeploymentSaaS.Shared.Contracts/    # Common DTOs & interfaces
│       └── AzureDeploymentSaaS.Shared.Infrastructure/ # Common utilities
├── 🌐 src/frontend/                         # React SPA Frontend
│   ├── src/components/                      # Reusable UI components
│   ├── src/pages/                           # Application pages
│   ├── src/services/                        # API service clients
│   └── Dockerfile                           # Frontend containerization
├── 🎯 src/dotnet/                           # Original Monolithic App (Legacy)
│   ├── AzureDeploymentWeb/                  # .NET 8 MVC Application
│   └── AzureDeploymentWeb.Tests/            # Unit Tests
├── 🏗️ infra/bicep/                          # Infrastructure as Code
│   ├── saas-main.bicep                      # Main SaaS infrastructure
│   └── modules/saas/                        # SaaS-specific Bicep modules
│       ├── container-apps-environment.bicep # Container Apps environment
│       ├── container-apps.bicep             # Container Apps deployment
│       ├── app-configuration.bicep          # Azure App Configuration
│       └── azure-ai-search.bicep            # Azure AI Search service
├── 🧪 tests/playwright/                      # E2E Tests
└── 📖 docs/                                 # Documentation
    └── ARCHITECTURE_SAAS.md                 # SaaS architecture documentation
```

## 🛠️ Technology Stack

### Backend Services
- **.NET 8 Web APIs**: RESTful microservices with OpenAPI/Swagger
- **Azure SDK**: Native Azure service integration
- **Entity Framework Core**: Cosmos DB integration with LINQ
- **Microsoft Identity Web**: JWT token validation and RBAC

### Frontend Application  
- **React 18**: Modern SPA with hooks and functional components
- **TypeScript**: Type-safe development
- **Microsoft Authentication Library (MSAL)**: Entra External ID integration
- **Monaco Editor**: In-browser ARM template editing
- **Bootstrap 5**: Responsive UI framework

### Azure Platform Services
- **Azure Container Apps**: Serverless container hosting
- **Azure Cosmos DB**: Multi-tenant NoSQL database with partition keys
- **Azure AI Search**: Cognitive search for template discovery
- **Azure App Configuration**: Centralized configuration and feature flags
- **Microsoft Entra External ID**: Customer identity and access management
- **Azure Container Registry**: Private container image storage
- **Application Insights**: APM and distributed tracing

## 🚀 Quick Start Guide

### Prerequisites
- .NET 8 SDK
- Node.js 18+ & npm
- Docker Desktop
- Azure subscription
- Azure CLI

### 1. Clone and Build

```bash
git clone https://github.com/christopherhouse/Azure-Service-Deployment-Web.git
cd Azure-Service-Deployment-Web

# Build microservices
dotnet restore Azure-Service-Deployment-SaaS.sln
dotnet build Azure-Service-Deployment-SaaS.sln

# Build frontend
cd src/frontend
npm install
npm run build
```

### 2. Deploy Infrastructure

```bash
cd infra/bicep

# Create resource group
az group create --name rg-azuredeploy-saas-dev --location eastus

# Deploy SaaS infrastructure
az deployment group create \
  --resource-group rg-azuredeploy-saas-dev \
  --template-file saas-main.bicep \
  --parameters environment=dev
```

### 3. Configure Authentication

1. **Create Entra External ID Tenant**:
   - Go to [Azure Portal](https://portal.azure.com) → Microsoft Entra ID → External Identities
   - Create new External ID tenant for customer registration

2. **Register Applications**:
   - Register SPA application for frontend
   - Register API applications for each microservice
   - Configure redirect URIs and API scopes

3. **Update Configuration**:
   ```bash
   # Set frontend environment variables
   cd src/frontend
   cp .env.example .env.local
   # Edit .env.local with your Entra External ID settings
   ```

### 4. Build and Push Container Images

```bash
# Build and push to Azure Container Registry
az acr login --name {your-acr-name}

# Template Library API
docker build -t {your-acr}.azurecr.io/template-library-api:latest src/microservices/Services/TemplateLibrary.Api
docker push {your-acr}.azurecr.io/template-library-api:latest

# Frontend
docker build -t {your-acr}.azurecr.io/frontend:latest src/frontend  
docker push {your-acr}.azurecr.io/frontend:latest
```

### 5. Deploy Container Apps

```bash
# Update container apps with new images
az containerapp update \
  --name azuredeploy-saas-dev-template-library-api \
  --resource-group rg-azuredeploy-saas-dev \
  --image {your-acr}.azurecr.io/template-library-api:latest
```

## 🔑 Key Features

### 🏢 Multi-Tenant Template Library
```typescript
// Create tenant-scoped template
const template = await templateLibraryService.createTemplate({
  name: "Storage Account with CMK",
  category: "Storage", 
  templateContent: armTemplateJson,
  parametersContent: parametersJson,
  tags: ["storage", "encryption", "cmk"],
  isPublic: false  // Tenant-private template
});
```

### 🔍 AI-Powered Template Search
```typescript
// Search templates by content
const results = await templateLibraryService.searchTemplates(
  "serverFarmId Microsoft.Web"  // Find web app templates
);
```

### 💳 Subscription Management
```csharp
// Feature flag for billing
[FeatureGate("BillingMock")]
public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
{
    // Real billing implementation
    return await _billingService.ProcessPaymentAsync(request);
}
```

### 👥 Self-Service Registration
```typescript
// Users can register with their own identity
const { instance } = useMsal();
await instance.loginRedirect({
  scopes: ["openid", "profile", "email"],
  prompt: "create"  // Force account creation flow
});
```

## 🧪 Testing

### Unit Tests (.NET)
```bash
# Run microservices tests
dotnet test src/microservices/Services/TemplateLibrary.Api.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### E2E Tests (Playwright)
```bash
cd tests/playwright

# Install dependencies
npm install
npx playwright install

# Run against deployed environment
PLAYWRIGHT_BASE_URL=https://your-frontend.azurecontainerapps.io npm test
```

### Integration Tests
```bash
# Test API endpoints
cd src/microservices
dotnet test --filter Category=Integration
```

## 🏗️ Infrastructure as Code

### SaaS Infrastructure Components

The infrastructure is fully automated using Azure Bicep:

```bash
# Main SaaS infrastructure
az deployment group create \
  --template-file infra/bicep/saas-main.bicep \
  --parameters @infra/bicep/parameters/saas.dev.bicepparam
```

**Deployed Resources:**
- Azure Container Apps Environment with Log Analytics
- Azure Cosmos DB with multi-tenant containers
- Azure AI Search service with template indexing
- Azure App Configuration with feature flags
- Azure Container Registry for private images
- Managed Identity for service authentication

### Configuration Management

Feature flags and configuration stored in Azure App Configuration:

```json
{
  "BillingMock": true,
  "TemplateLibrary:MaxTemplatesPerTenant": 1000,
  "Deployment:MaxConcurrentDeployments": 10,
  "SearchService:IndexName": "templates-index"
}
```

## 🔄 CI/CD Pipeline

### GitHub Actions Workflow

```yaml
# .github/workflows/deploy-saas.yml
name: Deploy SaaS Platform

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Build microservices
        run: dotnet build Azure-Service-Deployment-SaaS.sln
        
      - name: Build frontend
        run: |
          cd src/frontend
          npm ci && npm run build
          
      - name: Build and push containers
        run: |
          docker build -t $ACR_NAME/template-library-api:$GITHUB_SHA .
          docker push $ACR_NAME/template-library-api:$GITHUB_SHA
          
      - name: Deploy infrastructure
        run: |
          az deployment group create \
            --template-file infra/bicep/saas-main.bicep \
            --parameters imageTag=$GITHUB_SHA
```

## 📊 Monitoring & Observability

- **Application Insights**: Distributed tracing across microservices
- **Log Analytics**: Centralized logging with correlation IDs
- **Container Apps Metrics**: Scaling and performance monitoring
- **Azure Monitor**: Infrastructure health and alerting

## 🔐 Security Best Practices

- 🔑 **Zero Trust**: Managed Identity for all service-to-service communication
- 🏢 **Multi-Tenant Isolation**: Cosmos DB partition keys and API-level filtering
- 🛡️ **Authentication**: Microsoft Entra External ID with self-service registration
- 🔒 **Secrets Management**: Azure App Configuration and Key Vault integration
- 🌐 **Network Security**: Container Apps with private networking
- 📝 **Compliance**: Built-in audit logging and data residency

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/saas-enhancement`
3. Make your changes following the microservices patterns
4. Add tests for new functionality
5. Update documentation as needed
6. Submit a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Documentation

- 📖 [SaaS Architecture Guide](docs/ARCHITECTURE_SAAS.md)
- 🏗️ [Infrastructure Documentation](infra/bicep/README.md)
- 🧪 [Testing Guide](tests/README.md)
- 🔧 [Development Setup](docs/DEVELOPMENT.md)

---

**Transform your ARM template deployments into a modern SaaS experience! 🚀**

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