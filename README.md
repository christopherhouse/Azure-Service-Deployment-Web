# Azure ARM Template Deployment Web App

A React web application that enables you to deploy Azure resources by uploading ARM templates and parameter files through a user-friendly interface.

## 🚀 Features

- **File Upload Interface**: Upload ARM templates (.json) and parameter files (.json)
- **Azure Authentication**: Secure authentication using Microsoft Entra ID (Azure AD) with OAuth 2.0 auth code flow
- **Real-time Deployment Status**: Visual feedback during deployment with loading indicators
- **Success/Error Handling**: Clear success messages with emojis and detailed error reporting
- **Environment Configuration**: Easy configuration through environment variables

## 📋 Prerequisites

- Node.js 18+ and npm
- Azure subscription
- Azure AD app registration for authentication
- Resource group for deploying resources

## 🛠️ Setup Instructions

### 1. Clone and Install

```bash
git clone <repository-url>
cd Azure-Service-Deployment-Web
npm install
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