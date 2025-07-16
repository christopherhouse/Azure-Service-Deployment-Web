@description('The name of the Application Insights resource')
param name string

@description('The location for the resource')
param location string = resourceGroup().location

@description('The resource ID of the Log Analytics workspace')
param logAnalyticsWorkspaceId string

@description('Tags to apply to the resource')
param tags object = {}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

@description('The resource ID of the Application Insights component')
output applicationInsightsId string = applicationInsights.id

@description('The name of the Application Insights component')
output applicationInsightsName string = applicationInsights.name

// Connection string and instrumentation key should not be exposed in outputs for security reasons
// These can be retrieved using existing resource references where needed