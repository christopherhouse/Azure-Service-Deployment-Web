import { Configuration, PopupRequest } from "@azure/msal-browser";

// MSAL Configuration
export const msalConfig: Configuration = {
  auth: {
    clientId: process.env.REACT_APP_AZURE_CLIENT_ID || "",
    authority: `https://login.microsoftonline.com/${process.env.REACT_APP_AZURE_TENANT_ID || "common"}`,
    redirectUri: process.env.REACT_APP_AZURE_REDIRECT_URI || window.location.origin,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
};

// Add scopes for Azure Resource Manager
export const loginRequest: PopupRequest = {
  scopes: [
    "https://management.azure.com/user_impersonation",
    "User.Read"
  ],
};

// Graph API request
export const graphConfig = {
  graphMeEndpoint: "https://graph.microsoft.com/v1.0/me",
};