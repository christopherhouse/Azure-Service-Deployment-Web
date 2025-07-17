@description('The name of the Service Bus namespace')
param namespaceName string

@description('The name of the Service Bus topic')
param topicName string

@description('Enable duplicate detection for the topic')
param requiresDuplicateDetection bool = false

@description('The default message time to live (ISO 8601 duration)')
param defaultMessageTimeToLive string = 'P14D'

@description('The maximum size of the topic in megabytes')
@allowed([1024, 2048, 3072, 4096, 5120])
param maxSizeInMegabytes int = 1024

@description('Enable partitioning for the topic')
param enablePartitioning bool = false

// Reference to the existing Service Bus namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' existing = {
  name: namespaceName
}

resource serviceBusTopic 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  name: topicName
  parent: serviceBusNamespace
  properties: {
    requiresDuplicateDetection: requiresDuplicateDetection
    defaultMessageTimeToLive: defaultMessageTimeToLive
    maxSizeInMegabytes: maxSizeInMegabytes
    enablePartitioning: enablePartitioning
    enableBatchedOperations: true
    supportOrdering: false
    autoDeleteOnIdle: 'P10675199DT2H48M5.4775807S' // Max value - effectively disabled
    status: 'Active'
  }
}

@description('The resource ID of the Service Bus topic')
output serviceBusTopicId string = serviceBusTopic.id

@description('The name of the Service Bus topic')
output serviceBusTopicName string = serviceBusTopic.name