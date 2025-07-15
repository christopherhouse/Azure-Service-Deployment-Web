using '../main.bicep'

// Required parameters
param workloadName = 'azdeployment'
param environmentName = 'dev'
param location = 'eastus2'

// Azure AD authentication parameters (example values)
param azureAdInstance = 'https://login.microsoftonline.com'
param azureAdClientId = '00000000-0000-0000-0000-000000000000'
param azureAdClientSecret = 'example-client-secret-value'
param azureAdCallbackPath = '/signin-oidc'

// Optional parameters with example values
param logAnalyticsRetentionInDays = 90

// Tags can be customized as needed
param tags = {
  environment: 'dev'
  workload: 'azdeployment'
  deployedBy: 'bicep'
  costCenter: 'engineering'
  project: 'azure-deployment-web'
}
