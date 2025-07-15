@description('The name of the Key Vault to store secrets')
param keyVaultName string

@description('The name of the Redis Cache resource')
param redisResourceName string

@description('The name of the SignalR resource')
param signalRResourceName string

@description('Tags to apply to the secrets')
param tags object = {}

// Reference existing Redis resource
resource redisCache 'Microsoft.Cache/redis@2024-03-01' existing = {
  name: redisResourceName
}

// Reference existing SignalR resource
resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
  name: signalRResourceName
}

// Get Redis connection string
var redisKeys = redisCache.listKeys()
var redisHostName = redisCache.properties.hostName
var redisSslPort = '6380'
var redisFullConnectionString = '${redisHostName}:${redisSslPort},password=${redisKeys.primaryKey},ssl=True,abortConnect=False'

// Get SignalR connection string
var signalRKeys = signalR.listKeys()
var signalRConnectionString = signalRKeys.primaryConnectionString

// Create Redis connection string secret
resource redisConnSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  name: '${keyVaultName}/Cache--Redis--ConnectionString'
  properties: {
    value: redisFullConnectionString
    contentType: 'text/plain'
  }
}

// Create SignalR connection string secret
resource signalRConnSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  name: '${keyVaultName}/AzureSignalR--ConnectionString'
  properties: {
    value: signalRConnectionString
    contentType: 'text/plain'
  }
}

output redisConnectionStringSecretUri string = redisConnSecret.id
output signalRConnectionStringSecretUri string = signalRConnSecret.id
