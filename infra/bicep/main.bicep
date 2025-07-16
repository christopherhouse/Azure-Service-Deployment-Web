targetScope = 'resourceGroup'

@description('The name of the workload')
param workloadName string

@description('The environment name (e.g., dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('The location for all resources')
param location string = resourceGroup().location

@description('The retention period in days for the Log Analytics workspace')
@minValue(30)
@maxValue(730)
param logAnalyticsRetentionInDays int = 30

@description('The Azure AD instance URL')
param azureAdInstance string

@description('The client ID of the app registration used for authentication')
param azureAdClientId string

@description('The client secret of the app registration used for authentication')
@secure()
param azureAdClientSecret string

@description('The callback path for authentication')
param azureAdCallbackPath string

@description('Tags to apply to all resources')
param tags object = {
  environment: environmentName
  workload: workloadName
  deployedBy: 'bicep'
}

@description('The startup command for the web app (siteConfig.appCommandLine)')
param appStartupCommand string = ''

// Generate resource names following Azure Well-Architected Framework naming convention
var logAnalyticsWorkspaceName = 'log-${workloadName}-${environmentName}'
var keyVaultName = 'kv-${workloadName}-${environmentName}'
var redisCacheName = 'redis-${workloadName}-${environmentName}'
var signalRName = 'signalr-${workloadName}-${environmentName}'
var appServicePlanName = 'asp-${workloadName}-${environmentName}'
var webAppName = 'app-${workloadName}-${environmentName}'
var userAssignedIdentityName = 'id-${workloadName}-${environmentName}'

// Deploy Log Analytics workspace first as other resources depend on it
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'deploy-loganalytics-${deployment().name}'
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    retentionInDays: logAnalyticsRetentionInDays
    tags: tags
  }
}

// Deploy User Assigned Managed Identity
module managedIdentity 'modules/managed-identity.bicep' = {
  name: 'deploy-managedidentity-${deployment().name}'
  params: {
    name: userAssignedIdentityName
    location: location
    tags: tags
  }
}

// Deploy Key Vault
module keyVault 'modules/key-vault.bicep' = {
  name: 'deploy-keyvault-${deployment().name}'
  params: {
    name: keyVaultName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    userAssignedManagedIdentityPrincipalId: managedIdentity.outputs.principalId
    tags: tags
  }
}

// Deploy Key Vault secrets
module keyVaultSecrets 'modules/key-vault-secrets.bicep' = {
  name: 'deploy-keyvault-secrets-${deployment().name}'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    secretName: 'azure-ad-client-secret'
    secretValue: azureAdClientSecret
    contentType: 'text/plain'
    tags: tags
  }
}

// Deploy Redis Cache
module redisCache 'modules/redis.bicep' = {
  name: 'deploy-redis-${deployment().name}'
  params: {
    name: redisCacheName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    tags: tags
  }
}

// Deploy SignalR Service
module signalR 'modules/signalr.bicep' = {
  name: 'deploy-signalr-${deployment().name}'
  params: {
    name: signalRName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    tags: tags
  }
}

// Create connection string secrets for Redis and SignalR
module connectionStringSecrets 'modules/connection-string-secrets.bicep' = {
  name: 'connection-string-secrets-${deployment().name}'
  params: {
    keyVaultName: keyVault.outputs.keyVaultName
    redisResourceName: redisCache.outputs.redisCacheName
    signalRResourceName: signalR.outputs.signalRName
  }
}

// Deploy App Service Plan and Web App
module appService 'modules/app-service.bicep' = {
  name: 'deploy-appservice-${deployment().name}'
  params: {
    appServicePlanName: appServicePlanName
    webAppName: webAppName
    environmentName: environmentName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    userAssignedManagedIdentityName: managedIdentity.outputs.identityName
    azureAdInstance: azureAdInstance
    azureAdClientId: azureAdClientId
    azureAdClientSecretUri: keyVaultSecrets.outputs.secretUri
    azureAdCallbackPath: azureAdCallbackPath
    cacheRedisConnectionStringUri: connectionStringSecrets.outputs.redisConnectionStringSecretUri
    azureSignalRConnectionStringUri: connectionStringSecrets.outputs.signalRConnectionStringSecretUri
    tags: tags
    appStartupCommand: appStartupCommand
  }
}

// Outputs for reference by other templates or applications
@description('The resource ID of the Log Analytics workspace')
output logAnalyticsWorkspaceId string = logAnalytics.outputs.workspaceId

@description('The name of the Log Analytics workspace')
output logAnalyticsWorkspaceName string = logAnalytics.outputs.workspaceName

@description('The resource ID of the Key Vault')
output keyVaultId string = keyVault.outputs.keyVaultId

@description('The name of the Key Vault')
output keyVaultName string = keyVault.outputs.keyVaultName

@description('The URI of the Key Vault')
output keyVaultUri string = keyVault.outputs.keyVaultUri

@description('The resource ID of the user assigned managed identity')
output userAssignedManagedIdentityId string = managedIdentity.outputs.identityId

@description('The name of the user assigned managed identity')
output userAssignedManagedIdentityName string = managedIdentity.outputs.identityName

@description('The resource ID of the Redis Cache')
output redisCacheId string = redisCache.outputs.redisCacheId

@description('The name of the Redis Cache')
output redisCacheName string = redisCache.outputs.redisCacheName

@description('The hostname of the Redis Cache')
output redisHostName string = redisCache.outputs.redisHostName

@description('The resource ID of the SignalR Service')
output signalRId string = signalR.outputs.signalRId

@description('The name of the SignalR Service')
output signalRName string = signalR.outputs.signalRName

@description('The hostname of the SignalR Service')
output signalRHostName string = signalR.outputs.signalRHostName

@description('The resource ID of the App Service Plan')
output appServicePlanId string = appService.outputs.appServicePlanId

@description('The name of the App Service Plan')
output appServicePlanName string = appService.outputs.appServicePlanName

@description('The resource ID of the Web App')
output webAppId string = appService.outputs.webAppId

@description('The name of the Web App')
output webAppName string = appService.outputs.webAppName

@description('The URL of the Web App')
output webAppUrl string = appService.outputs.webAppUrl
