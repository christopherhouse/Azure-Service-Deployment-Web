# Azure Service Deployment Web - Infrastructure as Code

This directory contains Bicep templates for deploying the Azure infrastructure required by the Azure Service Deployment Web application.

## 🏗️ Architecture

The infrastructure includes the following Azure resources:

- **Azure App Service Plan** (P0v3 SKU) - Premium v3 tier for hosting the web application
- **Azure Web App** (.NET 8 runtime) - Linux-based web application hosting
- **Azure Key Vault** - Secure storage for application secrets and certificates
- **Azure SignalR Service** (Standard SKU) - Real-time communication service
- **Azure Redis Cache** (C0 SKU) - In-memory caching service
- **Log Analytics Workspace** - Centralized logging and monitoring

All resources are configured to send diagnostic logs and metrics to the Log Analytics workspace for centralized monitoring.

## 📁 Directory Structure

```
infra/bicep/
├── main.bicep                      # Main orchestration template
├── modules/                        # Resource-specific modules
│   ├── app-service.bicep           # App Service Plan & Web App
│   ├── key-vault.bicep             # Key Vault
│   ├── log-analytics.bicep         # Log Analytics Workspace
│   ├── redis.bicep                 # Redis Cache
│   └── signalr.bicep               # SignalR Service
├── parameters/
│   └── main.bicepparam             # Example parameter file
└── README.md                       # This file
```

## 🚀 Deployment

### Prerequisites

- Azure CLI installed and configured
- Azure subscription with appropriate permissions
- Resource group created for deployment

### Quick Start

1. **Clone the repository** (if not already done):
   ```bash
   git clone <repository-url>
   cd Azure-Service-Deployment-Web/infra/bicep
   ```

2. **Create a resource group**:
   ```bash
   az group create --name "rg-azdeployment-dev" --location "East US 2"
   ```

3. **Customize parameters** (optional):
   Edit `parameters/main.bicepparam` to adjust configuration values.

4. **Deploy the infrastructure**:
   ```bash
   az deployment group create \
     --resource-group "rg-azdeployment-dev" \
     --template-file main.bicep \
     --parameters parameters/main.bicepparam
   ```

### Parameter Configuration

The main parameters you can configure:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `workloadName` | string | `azdeployment` | Name of the workload (used in resource naming) |
| `environmentName` | string | `dev` | Environment name (dev/staging/prod) |
| `location` | string | `East US 2` | Azure region for deployment |
| `logAnalyticsRetentionInDays` | int | `90` | Log retention period (30-730 days) |
| `tags` | object | See example | Resource tags for organization |

### Example Deployment Commands

**Development Environment:**
```bash
az deployment group create \
  --resource-group "rg-azdeployment-dev" \
  --template-file main.bicep \
  --parameters workloadName=azdeployment environmentName=dev location="East US 2"
```

**Production Environment:**
```bash
az deployment group create \
  --resource-group "rg-azdeployment-prod" \
  --template-file main.bicep \
  --parameters workloadName=azdeployment environmentName=prod location="East US 2" logAnalyticsRetentionInDays=365
```

## 🏷️ Naming Convention

Resources follow Azure Well-Architected Framework naming best practices:

| Resource Type | Naming Pattern | Example |
|---------------|----------------|---------|
| Log Analytics | `log-{workload}-{env}-{unique}` | `log-azdeployment-dev-abc123` |
| Key Vault | `kv-{workload}-{env}-{unique}` | `kv-azdeployment-dev-abc123` |
| Redis Cache | `redis-{workload}-{env}-{unique}` | `redis-azdeployment-dev-abc123` |
| SignalR Service | `signalr-{workload}-{env}-{unique}` | `signalr-azdeployment-dev-abc123` |
| App Service Plan | `asp-{workload}-{env}-{unique}` | `asp-azdeployment-dev-abc123` |
| Web App | `app-{workload}-{env}-{unique}` | `app-azdeployment-dev-abc123` |

The `{unique}` suffix is generated using `uniqueString(resourceGroup().id)` to ensure global uniqueness.

## 📊 Monitoring & Diagnostics

All resources are configured with diagnostic settings that send logs and metrics to the Log Analytics workspace:

- **App Service**: HTTP logs, console logs, application logs, audit logs, platform logs, and metrics
- **Key Vault**: All logs and metrics
- **Redis Cache**: All logs and metrics
- **SignalR Service**: All logs and metrics
- **App Service Plan**: Metrics

## 🔧 Customization

### Adding New Resources

1. Create a new module in the `modules/` directory
2. Follow the existing naming conventions and patterns
3. Include diagnostic settings that send to Log Analytics
4. Add the module reference to `main.bicep`
5. Update this README with the new resource information

### Modifying Existing Resources

Each module is self-contained and can be modified independently:

- **App Service**: Modify `modules/app-service.bicep` to change SKUs, runtime versions, or configuration
- **Key Vault**: Adjust access policies, SKU, or features in `modules/key-vault.bicep`
- **Redis Cache**: Change SKU or configuration in `modules/redis.bicep`
- **SignalR**: Modify service tier or features in `modules/signalr.bicep`
- **Log Analytics**: Adjust retention or features in `modules/log-analytics.bicep`

## 🧪 Validation

All templates have been validated using Azure CLI:

```bash
# Validate main template
az bicep build --file main.bicep

# Validate all modules
cd modules
for file in *.bicep; do az bicep build --file "$file"; done
```

## 🔐 Security Considerations

- **Key Vault**: RBAC authorization enabled, soft delete enabled
- **Web App**: HTTPS only, minimum TLS 1.2, FTP over SSL only
- **Redis**: SSL enforced, minimum TLS 1.2
- **SignalR**: Public network access enabled (customize as needed)
- **Diagnostic Settings**: All resources send logs to Log Analytics for security monitoring

## 📋 Outputs

The main template provides the following outputs for integration:

- Log Analytics workspace ID and name
- Key Vault ID, name, and URI
- Redis Cache ID, name, and hostname
- SignalR Service ID, name, and hostname
- App Service Plan ID and name
- Web App ID, name, and URL

## 🆘 Troubleshooting

### Common Issues

1. **Resource name conflicts**: Each deployment generates unique names, but if deploying to the same resource group multiple times, ensure workload and environment names are different.

2. **Permission errors**: Ensure the deployment principal has Contributor access to the resource group and appropriate permissions for Key Vault operations.

3. **Location constraints**: Some SKUs may not be available in all regions. Verify SKU availability in your target region.

4. **Parameter validation**: Use `az deployment group validate` to check template and parameter validity before deployment.

### Debugging

Enable deployment debugging:
```bash
az deployment group create \
  --resource-group "your-rg" \
  --template-file main.bicep \
  --parameters parameters/main.bicepparam \
  --debug
```

## 📝 Contributing

When contributing to the infrastructure templates:

1. Follow existing naming conventions
2. Include appropriate diagnostic settings for new resources
3. Update parameter files and documentation
4. Validate templates with `az bicep build`
5. Test deployments in a development environment

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.