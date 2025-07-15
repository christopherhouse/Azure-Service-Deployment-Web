# Azure Service Deployment Web

A full-stack web application that enables you to deploy Azure resources through multiple interfaces:
- **React Frontend**: Upload ARM templates and parameter files through a user-friendly interface
- **.NET 8 MVC Backend**: API and web interface for Azure resource management
- **GitHub Actions**: Automated CI/CD deployment pipeline for infrastructure and applications

## 🚀 Features

- **File Upload Interface**: Upload ARM templates (.json) and parameter files (.json)
- **Azure Authentication**: Secure authentication using Microsoft Entra ID (Azure AD) with OAuth 2.0 auth code flow
- **Real-time Deployment Status**: Visual feedback during deployment with loading indicators
- **Success/Error Handling**: Clear success messages with emojis and detailed error reporting
- **Environment Configuration**: Easy configuration through environment variables
- **.NET 8 MVC Backend**: Robust API and web interface built with ASP.NET Core
- **Bicep Infrastructure**: Infrastructure as Code using Azure Bicep templates
- **GitHub Actions CI/CD**: Automated deployment pipeline with federated credentials

## 📋 Prerequisites

- Node.js 18+ and npm (for React frontend)
- .NET 8 SDK (for backend development)
- Azure subscription
- Azure AD app registration for authentication
- Resource group for deploying resources
- Azure service principal with federated credentials (for GitHub Actions)

## 🛠️ Setup Instructions

### Option 1: GitHub Actions Deployment (Recommended)

This repository includes a GitHub Actions workflow that automatically builds and deploys both the infrastructure and application.

#### Azure Setup for GitHub Actions

1. **Create Azure Service Principal with OIDC**:
   ```bash
   # Create a service principal
   az ad sp create-for-rbac --name "GitHub-Actions-OIDC" --role contributor --scopes /subscriptions/{subscription-id} --json-auth
   
   # Create federated credentials for GitHub Actions
   az ad app federated-credential create --id {app-id} --parameters '{
     "name": "GitHub-Actions",
     "issuer": "https://token.actions.githubusercontent.com",
     "subject": "repo:{your-github-username}/{your-repo-name}:ref:refs/heads/main",
     "description": "GitHub Actions OIDC",
     "audiences": ["api://AzureADTokenExchange"]
   }'
   ```

2. **Configure GitHub Repository Secrets**:
   - `AZURE_CLIENT_ID`: Application (client) ID of the service principal
   - `AZURE_TENANT_ID`: Directory (tenant) ID
   - `AZURE_SUBSCRIPTION_ID`: Your Azure subscription ID
   - `AZURE_AD_INSTANCE`: Azure AD instance URL (e.g., `https://login.microsoftonline.com`)
   - `AZURE_AD_CLIENT_ID`: Application (client) ID for web app authentication
   - `AZURE_AD_CLIENT_SECRET`: Client secret for web app authentication
   - `AZURE_AD_CALLBACK_PATH`: Authentication callback path (e.g., `/signin-oidc`)

3. **Trigger Deployment**:
   - Push to the `main` branch to trigger automatic deployment
   - Or manually trigger the workflow from GitHub Actions tab

The workflow will:
- Build the .NET 8 MVC application
- Deploy Bicep infrastructure to Azure
- Deploy the web application to the created App Service

### Option 2: Local Development Setup

#### For React Frontend Development

### 1. Clone and Install

```bash
git clone <repository-url>
cd Azure-Service-Deployment-Web/src/react
npm install
```

#### For .NET Backend Development

```bash
cd Azure-Service-Deployment-Web/src/dotnet/AzureDeploymentWeb
dotnet restore
dotnet build
dotnet run
```

### 2. Azure AD App Registration

1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
2. Click "New registration"
3. Set application name (e.g., "ARM Template Deployment Tool")
4. Set redirect URI: `http://localhost:3000` (for development)
5. Note the **Application (client) ID** and **Directory (tenant) ID**

### 3. Environment Configuration

1. Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```

2. Fill in your Azure configuration in `.env`:
   ```env
   REACT_APP_AZURE_CLIENT_ID=your-client-id-here
   REACT_APP_AZURE_TENANT_ID=your-tenant-id-here
   REACT_APP_AZURE_SUBSCRIPTION_ID=your-subscription-id-here
   REACT_APP_AZURE_RESOURCE_GROUP=your-resource-group-here
   REACT_APP_AZURE_REDIRECT_URI=http://localhost:3000
   ```

### 4. Run the Application

```bash
npm start
```

The app will be available at `http://localhost:3000`

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
src/
├── components/           # React components
│   ├── FileUpload.tsx   # File upload interface
│   ├── DeploymentStatus.tsx  # Status display component
│   └── LoginButton.tsx  # Authentication component
├── services/            # Business logic
│   └── azureDeploymentService.ts  # Azure ARM deployment logic
├── hooks/               # Custom React hooks
│   └── useAzureCredential.ts  # Azure authentication hook
├── authConfig.ts        # MSAL configuration
├── App.tsx             # Main application component
└── App.css             # Styling
```

## 🔧 Extension Guide

### Adding New File Types

To support additional file types, modify the `accept` attribute in `FileUpload.tsx`:

```tsx
// In src/components/FileUpload.tsx
<input
  type="file"
  accept=".json,.yaml,.yml"  // Add new extensions
  onChange={handleTemplateFileChange}
/>
```

### Custom Deployment Logic

Extend the `AzureDeploymentService` class in `src/services/azureDeploymentService.ts`:

```tsx
export class AzureDeploymentService {
  // Add custom deployment methods
  async deployWithValidation(params: DeploymentParams) {
    // Custom validation logic
    return this.deployTemplate(params);
  }
}
```

### Adding New UI Components

1. Create component in `src/components/`
2. Add corresponding CSS file
3. Import and use in `App.tsx`

Example:
```tsx
// src/components/NewComponent.tsx
import React from 'react';
import './NewComponent.css';

export const NewComponent: React.FC = () => {
  return <div>New Component</div>;
};
```

### Environment Variables

Add new environment variables in `.env.example` and `.env`:

```env
REACT_APP_CUSTOM_SETTING=value
```

Access in code:
```tsx
const customSetting = process.env.REACT_APP_CUSTOM_SETTING;
```

## 🔄 GitHub Actions Workflow

This repository includes a comprehensive CI/CD pipeline (`.github/workflows/deploy.yml`) that:

### Build Stage
- Checks out the code
- Sets up .NET 8 SDK
- Restores dependencies
- Builds the application in Release configuration
- Publishes the application
- Uploads build artifacts

### Deploy Stage (main branch only)
- Downloads build artifacts
- Authenticates with Azure using OIDC/federated credentials
- Creates Azure resource group if it doesn't exist
- Deploys Bicep infrastructure templates
- Deploys the web application to the created App Service

### Required GitHub Secrets
- `AZURE_CLIENT_ID`: Service principal client ID
- `AZURE_TENANT_ID`: Azure tenant ID  
- `AZURE_SUBSCRIPTION_ID`: Azure subscription ID
- `AZURE_AD_INSTANCE`: Azure AD instance URL (e.g., `https://login.microsoftonline.com`)
- `AZURE_AD_CLIENT_ID`: Application (client) ID for web app authentication
- `AZURE_AD_CLIENT_SECRET`: Client secret for web app authentication
- `AZURE_AD_CALLBACK_PATH`: Authentication callback path (e.g., `/signin-oidc`)

### Workflow Configuration
The workflow can be customized via environment variables in the workflow file:
- `AZURE_RESOURCE_GROUP`: Target resource group name
- `AZURE_REGION`: Azure region for deployment
- `DOTNET_VERSION`: .NET SDK version to use

### Manual Trigger
The workflow can be manually triggered from the GitHub Actions tab using the "workflow_dispatch" event.

## 🧪 Testing

Run tests:
```bash
npm test
```

Build for production:
```bash
npm run build
```

## 🚀 Deployment

### Development
```bash
npm start
```

### Production Build
```bash
npm run build
```

The build artifacts will be in the `build/` directory, ready for deployment to any static hosting service.

### Azure Static Web Apps
1. Build the project: `npm run build`
2. Deploy the `build/` folder to Azure Static Web Apps
3. Update the redirect URI in your Azure AD app registration

## 🔐 Security Considerations

- **Environment Variables**: Never commit `.env` files with real credentials
- **HTTPS**: Use HTTPS in production (update redirect URI accordingly)
- **Permissions**: Ensure Azure AD app has minimal required permissions
- **Resource Group**: Use a dedicated resource group for testing

## 🆘 Troubleshooting

### Common Issues

1. **Authentication Fails**
   - Verify client ID and tenant ID in `.env`
   - Check redirect URI matches Azure AD app registration
   - Ensure popup blockers are disabled

2. **Deployment Fails**
   - Verify subscription ID and resource group exist
   - Check Azure permissions for the authenticated user
   - Validate ARM template syntax

3. **Build Errors**
   - Clear node_modules: `rm -rf node_modules package-lock.json && npm install`
   - Check for TypeScript errors: `npx tsc --noEmit`

### Debug Mode

Enable debug logging by setting:
```env
REACT_APP_DEBUG=true
```

## 📝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes with tests
4. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🔗 Related Links

- [Azure ARM Templates Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/)
- [MSAL.js Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-js-initializing-client-applications)
- [Azure Resource Manager SDK](https://docs.microsoft.com/en-us/javascript/api/@azure/arm-resources/)

## Available Scripts

In the project directory, you can run:

### `npm start`

Runs the app in the development mode.\
Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

### `npm test`

Launches the test runner in the interactive watch mode.

### `npm run build`

Builds the app for production to the `build` folder.