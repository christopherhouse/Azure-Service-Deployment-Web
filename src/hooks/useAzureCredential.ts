import { useMsal } from "@azure/msal-react";
import { TokenCredential, AccessToken } from "@azure/core-auth";
import { loginRequest } from "../authConfig";

export class MsalTokenCredential implements TokenCredential {
  constructor(private instance: any) {}

  async getToken(scopes: string | string[]): Promise<AccessToken | null> {
    try {
      const account = this.instance.getAllAccounts()[0];
      if (!account) {
        throw new Error("No account found");
      }

      const scopeArray = Array.isArray(scopes) ? scopes : [scopes];
      const response = await this.instance.acquireTokenSilent({
        ...loginRequest,
        scopes: scopeArray,
        account: account,
      });

      return {
        token: response.accessToken,
        expiresOnTimestamp: response.expiresOn?.getTime() || 0,
      };
    } catch (error) {
      console.error("Failed to acquire token:", error);
      return null;
    }
  }
}

export const useAzureCredential = (): TokenCredential | null => {
  const { instance, accounts } = useMsal();

  if (accounts.length === 0) {
    return null;
  }

  return new MsalTokenCredential(instance);
};