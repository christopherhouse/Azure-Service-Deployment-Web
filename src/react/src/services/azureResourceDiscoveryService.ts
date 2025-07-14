import { ResourceManagementClient } from "@azure/arm-resources";
import { SubscriptionClient } from "@azure/arm-subscriptions";
import { TokenCredential } from "@azure/core-auth";

export interface AzureSubscription {
  id: string;
  displayName: string;
  state: string;
}

export interface AzureResourceGroup {
  name: string;
  location: string;
  id: string;
}

export class AzureResourceDiscoveryService {
  private credential: TokenCredential;

  constructor(credential: TokenCredential) {
    this.credential = credential;
  }

  /**
   * Get all subscriptions that the current user has access to
   */
  async getSubscriptions(): Promise<AzureSubscription[]> {
    try {
      const subscriptionClient = new SubscriptionClient(this.credential);
      const subscriptions: AzureSubscription[] = [];
      
      // List all subscriptions
      for await (const subscription of subscriptionClient.subscriptions.list()) {
        if (subscription.subscriptionId && subscription.displayName) {
          subscriptions.push({
            id: subscription.subscriptionId,
            displayName: subscription.displayName,
            state: subscription.state || 'Unknown'
          });
        }
      }

      // Filter to only enabled subscriptions and sort alphabetically by display name
      return subscriptions
        .filter(sub => sub.state === 'Enabled')
        .sort((a, b) => a.displayName.localeCompare(b.displayName));
    } catch (error) {
      console.error("Failed to get subscriptions:", error);
      throw new Error(`Failed to retrieve subscriptions: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Get all resource groups for a specific subscription
   */
  async getResourceGroups(subscriptionId: string): Promise<AzureResourceGroup[]> {
    try {
      const resourceClient = new ResourceManagementClient(this.credential, subscriptionId);
      const resourceGroups: AzureResourceGroup[] = [];
      
      // List all resource groups
      for await (const resourceGroup of resourceClient.resourceGroups.list()) {
        if (resourceGroup.name && resourceGroup.location && resourceGroup.id) {
          resourceGroups.push({
            name: resourceGroup.name,
            location: resourceGroup.location,
            id: resourceGroup.id
          });
        }
      }

      return resourceGroups.sort((a, b) => a.name.localeCompare(b.name));
    } catch (error) {
      console.error("Failed to get resource groups:", error);
      throw new Error(`Failed to retrieve resource groups: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }
}