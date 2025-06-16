# Example ARM Templates

This directory contains example ARM templates and parameter files that you can use to test the Azure ARM Template Deployment Web App.

## Storage Account Example

- **Template**: `storage-account.json` - Creates a basic Azure Storage Account (ARM template)
- **Parameters**: `storage-account-parameters.json` - Example parameters for the storage account

### How to Use

1. Upload the `storage-account.json` file as the ARM template
2. Upload `storage-account-parameters.json` as the parameters file
3. Click "Deploy to Azure"

### Customization

You can modify the parameters in `storage-account-parameters.json`:

- **storageAccountName**: Must be globally unique (3-24 characters, letters and numbers only)
- **location**: Azure region (e.g., "East US", "West Europe")
- **storageAccountSku**: Storage redundancy type

### Example Values

```json
{
  "storageAccountName": {
    "value": "mystorageaccount123"
  },
  "location": {
    "value": "East US"
  },
  "storageAccountSku": {
    "value": "Standard_LRS"
  }
}
```

## Creating Your Own Templates

1. Write your ARM template in JSON format
2. Create a parameters JSON file
3. Test deployment using the web app