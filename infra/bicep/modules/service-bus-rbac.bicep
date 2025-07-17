@description('The name of the Service Bus namespace')
param serviceBusNamespaceName string

@description('The principal ID of the managed identity to grant permissions')
param principalId string

@description('The type of principal (User, Group, ServicePrincipal)')
@allowed(['User', 'Group', 'ServicePrincipal'])
param principalType string = 'ServicePrincipal'

// Reference existing Service Bus namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: serviceBusNamespaceName
}

// Azure Service Bus Data Owner role definition ID
// This role allows sending and receiving messages and managing entities
var serviceBusDataOwnerRoleId = '090c5cfd-751d-490a-894a-3ce6f1109419'

// Assign Service Bus Data Owner role to the managed identity
resource serviceBusRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, principalId, serviceBusDataOwnerRoleId)
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', serviceBusDataOwnerRoleId)
    principalId: principalId
    principalType: principalType
  }
}

@description('The resource ID of the role assignment')
output roleAssignmentId string = serviceBusRoleAssignment.id

@description('The role definition ID that was assigned')
output roleDefinitionId string = serviceBusDataOwnerRoleId