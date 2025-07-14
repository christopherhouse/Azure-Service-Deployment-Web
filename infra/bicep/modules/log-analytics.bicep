@description('The name of the Log Analytics workspace')
param name string

@description('The location for the resource')
param location string = resourceGroup().location

@description('The retention period in days for the Log Analytics workspace')
@minValue(30)
@maxValue(730)
param retentionInDays int = 30

@description('Tags to apply to the resource')
param tags object = {}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: -1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

@description('The resource ID of the Log Analytics workspace')
output workspaceId string = logAnalyticsWorkspace.id

@description('The name of the Log Analytics workspace')
output workspaceName string = logAnalyticsWorkspace.name

@description('The customer ID (workspace ID) of the Log Analytics workspace')
output customerId string = logAnalyticsWorkspace.properties.customerId