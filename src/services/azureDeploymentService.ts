import { ResourceManagementClient, Deployment } from "@azure/arm-resources";
import { TokenCredential } from "@azure/core-auth";

export interface DeploymentParams {
  template: any;
  parameters: any;
  deploymentName: string;
}

export interface DeploymentResult {
  success: boolean;
  deploymentName: string;
  resourceGroupName: string;
  outputs?: any;
  error?: string;
}

export class AzureDeploymentService {
  private client: ResourceManagementClient;
  private subscriptionId: string;
  private resourceGroupName: string;

  constructor(credential: TokenCredential) {
    this.subscriptionId = process.env.REACT_APP_AZURE_SUBSCRIPTION_ID!;
    this.resourceGroupName = process.env.REACT_APP_AZURE_RESOURCE_GROUP!;
    
    if (!this.subscriptionId || !this.resourceGroupName) {
      throw new Error("Missing required environment variables: REACT_APP_AZURE_SUBSCRIPTION_ID or REACT_APP_AZURE_RESOURCE_GROUP");
    }

    this.client = new ResourceManagementClient(credential, this.subscriptionId);
  }

  async deployTemplate(params: DeploymentParams): Promise<DeploymentResult> {
    try {
      const deploymentParameters: Deployment = {
        properties: {
          template: params.template,
          parameters: params.parameters,
          mode: "Incremental",
        },
      };

      console.log(`Starting deployment: ${params.deploymentName}`);
      
      const deployment = await this.client.deployments.beginCreateOrUpdateAndWait(
        this.resourceGroupName,
        params.deploymentName,
        deploymentParameters
      );

      return {
        success: true,
        deploymentName: params.deploymentName,
        resourceGroupName: this.resourceGroupName,
        outputs: deployment.properties?.outputs,
      };
    } catch (error) {
      console.error("Deployment failed:", error);
      return {
        success: false,
        deploymentName: params.deploymentName,
        resourceGroupName: this.resourceGroupName,
        error: error instanceof Error ? error.message : "Unknown deployment error",
      };
    }
  }

  async getDeploymentStatus(deploymentName: string) {
    try {
      const deployment = await this.client.deployments.get(
        this.resourceGroupName,
        deploymentName
      );
      return deployment.properties?.provisioningState;
    } catch (error) {
      console.error("Failed to get deployment status:", error);
      return "Failed";
    }
  }
}