@description('The name of the Service Bus namespace')
param namespaceName string

@description('The name of the Service Bus topic')
param topicName string

@description('The name of the Service Bus subscription')
param subscriptionName string

@description('The default message time to live (ISO 8601 duration)')
param defaultMessageTimeToLive string = 'P14D'

@description('The lock duration for the subscription (ISO 8601 duration)')
param lockDuration string = 'PT1M'

@description('Enable dead lettering on message expiration')
param deadLetteringOnMessageExpiration bool = true

@description('Enable dead lettering on filter evaluation exceptions')
param deadLetteringOnFilterEvaluationExceptions bool = true

@description('Maximum delivery count before dead lettering')
param maxDeliveryCount int = 10

@description('Enable session support')
param requiresSession bool = false

// Reference to the existing Service Bus topic
resource serviceBusTopic 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' existing = {
  name: '${namespaceName}/${topicName}'
}

resource serviceBusSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  name: subscriptionName
  parent: serviceBusTopic
  properties: {
    defaultMessageTimeToLive: defaultMessageTimeToLive
    lockDuration: lockDuration
    deadLetteringOnMessageExpiration: deadLetteringOnMessageExpiration
    deadLetteringOnFilterEvaluationExceptions: deadLetteringOnFilterEvaluationExceptions
    maxDeliveryCount: maxDeliveryCount
    requiresSession: requiresSession
    enableBatchedOperations: true
    autoDeleteOnIdle: 'P10675199DT2H48M5.4775807S' // Max value - effectively disabled
    status: 'Active'
  }
}

// Create a default rule that accepts all messages (since requirement is "all-messages")
resource defaultSubscriptionRule 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2024-01-01' = {
  name: '$Default'
  parent: serviceBusSubscription
  properties: {
    filterType: 'SqlFilter'
    sqlFilter: {
      sqlExpression: '1=1' // Always true - accepts all messages
    }
    action: {}
  }
}

@description('The resource ID of the Service Bus subscription')
output serviceBusSubscriptionId string = serviceBusSubscription.id

@description('The name of the Service Bus subscription')
output serviceBusSubscriptionName string = serviceBusSubscription.name