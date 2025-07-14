#!/bin/bash

# Azure Service Deployment Web - Infrastructure Deployment Script
# This script deploys the infrastructure for the Azure Service Deployment Web application

set -e

# Default values
RESOURCE_GROUP=""
WORKLOAD_NAME="azdeployment"
ENVIRONMENT_NAME="dev"
LOCATION="East US 2"
SUBSCRIPTION_ID=""

# Function to show usage
show_usage() {
    echo "Usage: $0 -g <resource-group> [-w <workload-name>] [-e <environment>] [-l <location>] [-s <subscription-id>]"
    echo ""
    echo "Required:"
    echo "  -g    Resource group name"
    echo ""
    echo "Optional:"
    echo "  -w    Workload name (default: azdeployment)"
    echo "  -e    Environment name: dev, staging, prod (default: dev)"
    echo "  -l    Azure location (default: East US 2)"
    echo "  -s    Azure subscription ID (uses current subscription if not specified)"
    echo "  -h    Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 -g rg-azdeployment-dev"
    echo "  $0 -g rg-azdeployment-prod -e prod -l \"West US 2\""
    echo "  $0 -g rg-azdeployment-test -w myapp -e staging"
}

# Parse command line arguments
while getopts "g:w:e:l:s:h" opt; do
    case $opt in
        g)
            RESOURCE_GROUP="$OPTARG"
            ;;
        w)
            WORKLOAD_NAME="$OPTARG"
            ;;
        e)
            ENVIRONMENT_NAME="$OPTARG"
            ;;
        l)
            LOCATION="$OPTARG"
            ;;
        s)
            SUBSCRIPTION_ID="$OPTARG"
            ;;
        h)
            show_usage
            exit 0
            ;;
        \?)
            echo "Invalid option: -$OPTARG" >&2
            show_usage
            exit 1
            ;;
    esac
done

# Validate required parameters
if [ -z "$RESOURCE_GROUP" ]; then
    echo "Error: Resource group name is required (-g)"
    show_usage
    exit 1
fi

# Validate environment name
if [[ ! "$ENVIRONMENT_NAME" =~ ^(dev|staging|prod)$ ]]; then
    echo "Error: Environment name must be one of: dev, staging, prod"
    exit 1
fi

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

echo "Azure Service Deployment Web - Infrastructure Deployment"
echo "========================================================"
echo "Resource Group: $RESOURCE_GROUP"
echo "Workload Name:  $WORKLOAD_NAME"
echo "Environment:    $ENVIRONMENT_NAME"
echo "Location:       $LOCATION"
echo ""

# Set subscription if provided
if [ -n "$SUBSCRIPTION_ID" ]; then
    echo "Setting subscription to: $SUBSCRIPTION_ID"
    az account set --subscription "$SUBSCRIPTION_ID"
fi

# Show current subscription
CURRENT_SUBSCRIPTION=$(az account show --query "name" -o tsv)
echo "Using subscription: $CURRENT_SUBSCRIPTION"
echo ""

# Check if resource group exists, create if it doesn't
echo "Checking if resource group '$RESOURCE_GROUP' exists..."
if ! az group show --name "$RESOURCE_GROUP" >/dev/null 2>&1; then
    echo "Resource group does not exist. Creating..."
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
    echo "Resource group created successfully."
else
    echo "Resource group already exists."
fi
echo ""

# Validate the template
echo "Validating Bicep template..."
az deployment group validate \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$SCRIPT_DIR/main.bicep" \
    --parameters \
        workloadName="$WORKLOAD_NAME" \
        environmentName="$ENVIRONMENT_NAME" \
        location="$LOCATION" \
    --query "error" \
    --output table

if [ $? -eq 0 ]; then
    echo "Template validation successful."
else
    echo "Template validation failed. Please check the errors above."
    exit 1
fi
echo ""

# Deploy the infrastructure
echo "Deploying infrastructure..."
echo "This may take several minutes..."

DEPLOYMENT_NAME="deploy-$WORKLOAD_NAME-$ENVIRONMENT_NAME-$(date +%Y%m%d-%H%M%S)"

az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --template-file "$SCRIPT_DIR/main.bicep" \
    --parameters \
        workloadName="$WORKLOAD_NAME" \
        environmentName="$ENVIRONMENT_NAME" \
        location="$LOCATION" \
    --output table

if [ $? -eq 0 ]; then
    echo ""
    echo "🎉 Deployment completed successfully!"
    echo ""
    echo "Getting deployment outputs..."
    az deployment group show \
        --resource-group "$RESOURCE_GROUP" \
        --name "$DEPLOYMENT_NAME" \
        --query "properties.outputs" \
        --output table
else
    echo ""
    echo "❌ Deployment failed. Please check the errors above."
    exit 1
fi