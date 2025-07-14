import { ResourceManagementClient, Deployment } from "@azure/arm-resources";
import { TokenCredential } from "@azure/core-auth";

export interface DeploymentParams {
  template: any;
  parameters: any;
  deploymentName: string;
  subscriptionId: string;
  resourceGroupName: string;
}

export interface DeploymentResult {
  success: boolean;
  deploymentName: string;
  resourceGroupName: string;
  outputs?: any;
  error?: string;
}

export class AzureDeploymentService {
  private credential: TokenCredential;

  constructor(credential: TokenCredential) {
    this.credential = credential;
  }

  /**
   * Extracts actual parameter values from ARM parameter file structure.
   * Handles both full ARM parameter files (with $schema, contentVersion, parameters)
   * and direct parameter objects.
   */
  private extractParameters(parameters: any): any {
    // If the parameters object has a 'parameters' property, it's a full ARM parameter file
    if (parameters && typeof parameters === 'object' && parameters.parameters) {
      return parameters.parameters;
    }
    
    // Otherwise, assume it's already in the correct format
    return parameters;
  }

  async deployTemplate(params: DeploymentParams): Promise<DeploymentResult> {
    try {
      // Create client for the specific subscription
      const client = new ResourceManagementClient(this.credential, params.subscriptionId);
      
      // Extract actual parameters from ARM parameter file structure if needed
      const actualParameters = this.extractParameters(params.parameters);
      
      const deploymentParameters: Deployment = {
        properties: {
          template: params.template,
          parameters: actualParameters,
          mode: "Incremental",
        },
      };

      console.log(`Starting deployment: ${params.deploymentName} in subscription ${params.subscriptionId}, resource group ${params.resourceGroupName}`);
      
      const deployment = await client.deployments.beginCreateOrUpdateAndWait(
        params.resourceGroupName,
        params.deploymentName,
        deploymentParameters
      );

      return {
        success: true,
        deploymentName: params.deploymentName,
        resourceGroupName: params.resourceGroupName,
        outputs: deployment.properties?.outputs,
      };
    } catch (error) {
      console.error("Deployment failed:", error);
      return {
        success: false,
        deploymentName: params.deploymentName,
        resourceGroupName: params.resourceGroupName,
        error: error instanceof Error ? error.message : "Unknown deployment error",
      };
    }
  }

  async getDeploymentStatus(deploymentName: string, subscriptionId: string, resourceGroupName: string) {
    try {
      const client = new ResourceManagementClient(this.credential, subscriptionId);
      const deployment = await client.deployments.get(
        resourceGroupName,
        deploymentName
      );
      return deployment.properties?.provisioningState;
    } catch (error) {
      console.error("Failed to get deployment status:", error);
      return "Failed";
    }
  }
}