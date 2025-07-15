@description('The name of the App Service Plan')
param appServicePlanName string

@description('The name of the Web App')
param webAppName string

@description('The environment name for app settings')
param environmentName string

@description('The location for the resource')
param location string = resourceGroup().location

@description('Tags to apply to the resource')
param tags object = {}

@description('The resource ID of the Log Analytics workspace for diagnostic settings')
param logAnalyticsWorkspaceId string

@description('The name of the user assigned managed identity')
param userAssignedManagedIdentityName string

@description('Azure AD instance URL')
param azureAdInstance string

@description('Azure AD client ID')
param azureAdClientId string

@description('Azure AD client secret Key Vault reference URI')
param azureAdClientSecretUri string

@description('Azure AD callback path')
param azureAdCallbackPath string

@description('The startup command for the web app (siteConfig.appCommandLine)')
param appStartupCommand string = ''

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'P0v3'
    tier: 'Premium0V3'
    size: 'P0v3'
    family: 'Pv3'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    perSiteScaling: false
    elasticScaleEnabled: false
    maximumElasticWorkerCount: 1
    isSpot: false
    reserved: true
    isXenon: false
    hyperV: false
    targetWorkerCount: 0
    targetWorkerSizeId: 0
    zoneRedundant: false
  }
}

resource existingUami 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: userAssignedManagedIdentityName
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${existingUami.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    reserved: true // Required for Linux
    siteConfig: {
      linuxFxVersion: 'DOTNET|8.0'
      netFrameworkVersion: 'v4.0'
      numberOfWorkers: 1
      alwaysOn: true
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      http20Enabled: true
      publicNetworkAccess: 'Enabled'
      keyVaultReferenceIdentity: existingUami.id
      appCommandLine: appStartupCommand
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName
        }
        {
          name: 'AzureAd__Instance'
          value: azureAdInstance
        }
        {
          name: 'AzureAd__ClientId'
          value: azureAdClientId
        }
        {
          name: 'AzureAd__ClientSecret'
          value: '@Microsoft.KeyVault(SecretUri=${azureAdClientSecretUri})'
        }
        {
          name: 'AzureAd__CallbackPath'
          value: azureAdCallbackPath
        }
      ]
      virtualApplications: [
        {
          virtualPath: '/'
          physicalPath: 'site\\wwwroot'
          preloadEnabled: true
        }
      ]
    }
    httpsOnly: true
    clientAffinityEnabled: false
  }
}

// Diagnostic settings for App Service Plan
resource appServicePlanDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${appServicePlanName}'
  scope: appServicePlan
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

// Diagnostic settings for Web App
resource webAppDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${webAppName}'
  scope: webApp
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'AppServiceAppLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'AppServiceAuditLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'AppServiceIPSecAuditLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'AppServicePlatformLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

@description('The resource ID of the App Service Plan')
output appServicePlanId string = appServicePlan.id

@description('The name of the App Service Plan')
output appServicePlanName string = appServicePlan.name

@description('The resource ID of the Web App')
output webAppId string = webApp.id

@description('The name of the Web App')
output webAppName string = webApp.name

@description('The default hostname of the Web App')
output webAppDefaultHostName string = webApp.properties.defaultHostName

@description('The URL of the Web App')
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
