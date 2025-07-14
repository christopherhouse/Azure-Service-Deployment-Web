@description('The name of the workload')
param workloadName string

@description('The environment name (e.g., dev, staging, prod)')
param environmentName string

@description('The location for the resource')
param location string = resourceGroup().location

@description('Tags to apply to the resource')
param tags object = {}

@description('The resource ID of the Log Analytics workspace for diagnostic settings')
param logAnalyticsWorkspaceId string

// Generate unique name following Azure Well-Architected Framework naming convention
var signalRName = 'signalr-${workloadName}-${environmentName}-${uniqueString(resourceGroup().id)}'

resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: signalRName
  location: location
  tags: tags
  sku: {
    name: 'Standard_S1'
    tier: 'Standard'
    capacity: 1
  }
  kind: 'SignalR'
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Default'
      }
      {
        flag: 'EnableConnectivityLogs'
        value: 'True'
      }
      {
        flag: 'EnableMessagingLogs'
        value: 'True'
      }
      {
        flag: 'EnableLiveTrace'
        value: 'True'
      }
    ]
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
    disableAadAuth: false
  }
}

// Diagnostic settings for SignalR Service
resource signalRDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${signalRName}'
  scope: signalR
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
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

@description('The resource ID of the SignalR Service')
output signalRId string = signalR.id

@description('The name of the SignalR Service')
output signalRName string = signalR.name

@description('The hostname of the SignalR Service')
output signalRHostName string = signalR.properties.hostName