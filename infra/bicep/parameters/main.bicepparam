using '../main.bicep'

// Required parameters
param workloadName = 'azdeployment'
param environmentName = 'dev'
param location = 'eastus2'

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
