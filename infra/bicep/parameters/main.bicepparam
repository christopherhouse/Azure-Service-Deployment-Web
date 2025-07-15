using '../main.bicep'

// Required parameters
param workloadName = 'azdeployment'
param environmentName = 'dev'
param location = 'eastus2'

// Azure AD authentication parameters (example values)
param azureAdInstance = 'https://login.microsoftonline.com'
param azureAdClientId = '1bc37b1a-d4a7-4f5c-bdc5-18a3142e73fa'
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
