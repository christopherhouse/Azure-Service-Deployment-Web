using '../main.bicep'

// Required parameters
param workloadName = 'azdeployment'
param environmentName = 'dev'
param location = 'eastus2'

// Azure AD authentication parameters (example values)
param azureAdInstance = 'https://login.microsoftonline.com'
param azureAdClientId = '5a28f6cf-be38-4d56-8317-84a5f96c5b73'
param azureAdClientSecret = ''
param azureAdCallbackPath = '/signin-oidc'

// Optional parameters with example values
param logAnalyticsRetentionInDays = 90
param appStartupCommand = 'dotnet AzureDeploymentWeb.dll'

// Tags can be customized as needed
param tags = {
  environment: 'dev'
  workload: 'azdeployment'
  deployedBy: 'bicep'
  costCenter: 'engineering'
  project: 'azure-deployment-web'
}
