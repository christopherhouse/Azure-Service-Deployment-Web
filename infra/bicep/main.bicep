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

@description('Tags to apply to all resources')
param tags object = {
  environment: environmentName
  workload: workloadName
  deployedBy: 'bicep'
}

// Deploy Log Analytics workspace first as other resources depend on it
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'deploy-loganalytics-${uniqueString(deployment().name, location)}'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    retentionInDays: logAnalyticsRetentionInDays
    tags: tags
  }
}

// Deploy Key Vault
module keyVault 'modules/key-vault.bicep' = {
  name: 'deploy-keyvault-${uniqueString(deployment().name, location)}'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    tags: tags
  }
}

// Deploy Redis Cache
module redisCache 'modules/redis.bicep' = {
  name: 'deploy-redis-${uniqueString(deployment().name, location)}'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    tags: tags
  }
}

// Deploy SignalR Service
module signalR 'modules/signalr.bicep' = {
  name: 'deploy-signalr-${uniqueString(deployment().name, location)}'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    tags: tags
  }
}

// Deploy App Service Plan and Web App
module appService 'modules/app-service.bicep' = {
  name: 'deploy-appservice-${uniqueString(deployment().name, location)}'
  params: {
    workloadName: workloadName
    environmentName: environmentName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    tags: tags
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