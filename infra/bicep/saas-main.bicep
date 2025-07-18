targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, test, prod)')
param environment string = 'dev'

@description('Application name prefix')
param appName string = 'azuredeploy-saas'

@description('Container registry image tag')
param imageTag string = 'latest'

@description('Microsoft Entra External ID Configuration')
param entraExternalId object = {
  tenantId: ''
  clientId: ''
  clientSecret: ''
}

// Generate unique names
var containerAppsEnvironmentName = '${appName}-${environment}-env'
var logAnalyticsWorkspaceName = '${appName}-${environment}-logs'
var appConfigurationName = '${appName}-${environment}-config'
var searchServiceName = '${appName}-${environment}-search'
var cosmosDbAccountName = '${appName}-${environment}-cosmos'
var containerRegistryName = replace('${appName}${environment}acr', '-', '')

// Deploy Container Apps Environment
module containerAppsEnvironment './modules/saas/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  params: {
    location: location
    environment: environment
    appName: appName
    containerAppsEnvironmentName: containerAppsEnvironmentName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
  }
}

// Deploy App Configuration Service
module appConfiguration './modules/saas/app-configuration.bicep' = {
  name: 'app-configuration'
  params: {
    location: location
    environment: environment
    appName: appName
    appConfigurationName: appConfigurationName
    managedIdentityPrincipalId: containerAppsEnvironment.outputs.managedIdentityPrincipalId
  }
}

// Deploy Azure AI Search
module azureAiSearch './modules/saas/azure-ai-search.bicep' = {
  name: 'azure-ai-search'
  params: {
    location: location
    environment: environment
    appName: appName
    searchServiceName: searchServiceName
    managedIdentityPrincipalId: containerAppsEnvironment.outputs.managedIdentityPrincipalId
  }
}

// Deploy Azure Cosmos DB
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosDbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    enableFreeTier: environment == 'dev'
  }
}

// Create Cosmos DB databases
resource templatesDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosDbAccount
  name: 'TemplatesDatabase'
  properties: {
    resource: {
      id: 'TemplatesDatabase'
    }
  }
}

resource usersDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosDbAccount
  name: 'UsersDatabase'
  properties: {
    resource: {
      id: 'UsersDatabase'
    }
  }
}

// Create containers with partition keys for multi-tenancy
resource templatesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: templatesDatabase
  name: 'templates'
  properties: {
    resource: {
      id: 'templates'
      partitionKey: {
        paths: ['/tenantId']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

resource usersContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: usersDatabase
  name: 'users'
  properties: {
    resource: {
      id: 'users'
      partitionKey: {
        paths: ['/tenantId']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

resource tenantsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: usersDatabase
  name: 'tenants'
  properties: {
    resource: {
      id: 'tenants'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

// Deploy Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'disabled'
      }
      retentionPolicy: {
        days: 7
        status: 'disabled'
      }
    }
    encryption: {
      status: 'disabled'
    }
    dataEndpointEnabled: false
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
    zoneRedundancy: 'Disabled'
  }
}

// Assign AcrPull role to managed identity
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(containerRegistry.id, containerAppsEnvironment.outputs.managedIdentityPrincipalId, 'AcrPull')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // AcrPull
    principalId: containerAppsEnvironment.outputs.managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Deploy Container Apps
module containerApps './modules/saas/container-apps.bicep' = {
  name: 'container-apps'
  params: {
    location: location
    environment: environment
    appName: appName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.containerAppsEnvironmentId
    managedIdentityClientId: containerAppsEnvironment.outputs.managedIdentityClientId
    containerRegistryLoginServer: containerRegistry.properties.loginServer
    appConfigurationEndpoint: appConfiguration.outputs.appConfigurationEndpoint
    cosmosDbEndpoint: cosmosDbAccount.properties.documentEndpoint
    searchServiceEndpoint: azureAiSearch.outputs.searchServiceEndpoint
    imageTag: imageTag
  }
  dependsOn: [
    acrPullRoleAssignment
  ]
}

// Output values
output containerAppsEnvironmentId string = containerAppsEnvironment.outputs.containerAppsEnvironmentId
output appConfigurationEndpoint string = appConfiguration.outputs.appConfigurationEndpoint
output searchServiceEndpoint string = azureAiSearch.outputs.searchServiceEndpoint
output cosmosDbAccountName string = cosmosDbAccount.name
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
output containerRegistryName string = containerRegistry.name
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output managedIdentityClientId string = containerAppsEnvironment.outputs.managedIdentityClientId
output templateLibraryApiUrl string = containerApps.outputs.templateLibraryApiUrl
output identityApiUrl string = containerApps.outputs.identityApiUrl
output frontendUrl string = containerApps.outputs.frontendUrl