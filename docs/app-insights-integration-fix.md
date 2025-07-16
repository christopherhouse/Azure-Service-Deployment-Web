# Application Insights Integration Fix

## Overview

This fix addresses the Application Insights integration issue by adding the necessary configuration to enable seamless navigation from the App Service to the Application Insights resource in the Azure portal.

## Changes Made

### 1. Hidden Link Tag (`infra/bicep/modules/app-service.bicep`)

Added a hidden link tag to the App Service resource that creates a portal navigation link to the Application Insights resource:

```bicep
tags: union(tags, {
  'hidden-link:${existingApplicationInsights.id}': 'Resource'
})
```

This tag follows the Azure naming convention for resource linking and enables the Azure portal to display a direct navigation link from the App Service to its associated Application Insights resource.

### 2. Instrumentation Key App Setting

Added the `APPINSIGHTS_INSTRUMENTATIONKEY` app setting alongside the existing `APPLICATIONINSIGHTS_CONNECTION_STRING`:

```bicep
{
  name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
  value: existingApplicationInsights.properties.InstrumentationKey
}
```

This provides additional compatibility and ensures both modern (connection string) and legacy (instrumentation key) Application Insights integration methods are available.

## Benefits

After deploying these changes, users will be able to:

1. **Navigate directly** from the App Service resource to the Application Insights resource in the Azure portal
2. **See the Application Insights configuration** properly linked in the App Service settings
3. **Use both connection string and instrumentation key** for Application Insights integration

## Testing

A test script (`test-app-insights-integration.sh`) has been created to validate:
- ✅ Hidden link tag is present in generated ARM templates
- ✅ Both Application Insights app settings are configured
- ✅ Hidden link tag correctly references the Application Insights component
- ✅ Bicep templates compile without errors

## Deployment

These changes are backward compatible and can be deployed using the existing GitHub Actions workflow. The changes will take effect on the next deployment and require no manual intervention.

## Technical Details

- **Tag Format**: `hidden-link:{ApplicationInsightsResourceId}` with value `Resource`
- **App Settings**: Both `APPLICATIONINSIGHTS_CONNECTION_STRING` and `APPINSIGHTS_INSTRUMENTATIONKEY` are now configured
- **Resource References**: Uses existing Application Insights resource reference for both tag and app settings
- **Compatibility**: Maintains all existing functionality while adding new portal integration features