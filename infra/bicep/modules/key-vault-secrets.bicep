@description('The name of the Key Vault')
param keyVaultName string

@description('The name of the secret')
param secretName string

@description('The value of the secret')
@secure()
param secretValue string

@description('The content type of the secret')
param contentType string = 'text/plain'

@description('Tags to apply to the resource')
param tags object = {}

// Reference the existing Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Create Key Vault secret
resource secret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: secretName
  parent: keyVault
  tags: tags
  properties: {
    value: secretValue
    contentType: contentType
  }
}

@description('The URI of the secret in Key Vault')
output secretUri string = secret.properties.secretUri

@description('The resource ID of the secret')
output secretId string = secret.id

@description('The name of the secret')
output secretName string = secret.name