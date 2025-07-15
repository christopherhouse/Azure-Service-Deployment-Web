@description('The name of the user assigned managed identity')
param name string

@description('The location for the resource')
param location string = resourceGroup().location

@description('Tags to apply to the resource')
param tags object = {}

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
  tags: tags
}

@description('The resource ID of the user assigned managed identity')
output identityId string = userAssignedIdentity.id

@description('The name of the user assigned managed identity')
output identityName string = userAssignedIdentity.name

@description('The principal ID of the user assigned managed identity')
output principalId string = userAssignedIdentity.properties.principalId

@description('The client ID of the user assigned managed identity')
output clientId string = userAssignedIdentity.properties.clientId