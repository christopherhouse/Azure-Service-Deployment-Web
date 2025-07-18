@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, test, prod)')
param environment string

@description('Application name prefix')
param appName string

@description('App Configuration Service name')
param appConfigurationName string

@description('Managed Identity Principal ID for key vault access')
param managedIdentityPrincipalId string

// Create App Configuration Service
resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: appConfigurationName
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    enablePurgeProtection: false
    publicNetworkAccess: 'Enabled'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Add sample configuration values
resource billingMockFeatureFlag 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfiguration
  name: '.appconfig.featureflag~2FBillingMock'
  properties: {
    value: jsonEncode({
      id: 'BillingMock'
      description: 'Mock billing functionality for development and testing'
      enabled: true
      conditions: {
        client_filters: []
      }
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
    tags: {
      environment: environment
      service: 'billing'
    }
  }
}

resource templateLibraryConfig 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfiguration
  name: 'TemplateLibrary:MaxTemplatesPerTenant'
  properties: {
    value: '1000'
    tags: {
      environment: environment
      service: 'template-library'
    }
  }
}

resource deploymentConfig 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfiguration
  name: 'Deployment:MaxConcurrentDeployments'
  properties: {
    value: '10'
    tags: {
      environment: environment
      service: 'deployment'
    }
  }
}

// Assign App Configuration Data Reader role to the managed identity
resource appConfigDataReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: appConfiguration
  name: guid(appConfiguration.id, managedIdentityPrincipalId, 'App Configuration Data Reader')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '516239f1-63e1-4d78-a4de-a74fb236a071') // App Configuration Data Reader
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output appConfigurationId string = appConfiguration.id
output appConfigurationName string = appConfiguration.name
output appConfigurationEndpoint string = appConfiguration.properties.endpoint