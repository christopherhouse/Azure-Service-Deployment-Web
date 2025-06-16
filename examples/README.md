# Example Bicep Templates

This directory contains example Bicep templates and parameter files that you can use to test the Azure Bicep Deployment Web App.

## Storage Account Example

- **Template**: `storage-account.bicep` - Creates a basic Azure Storage Account
- **Parameters**: `storage-account-parameters.json` - Example parameters for the storage account

### How to Use

1. Convert the Bicep template to JSON format (required for deployment):
   ```bash
   az bicep build --file storage-account.bicep
   ```

2. Upload the generated `storage-account.json` file as the template
3. Upload `storage-account-parameters.json` as the parameters file
4. Click "Deploy to Azure"

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

1. Write your Bicep template
2. Compile to JSON: `az bicep build --file yourtemplate.bicep`
3. Create a parameters JSON file
4. Test deployment using the web app