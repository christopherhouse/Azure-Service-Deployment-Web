import { PublicClientApplication, Configuration } from '@azure/msal-browser';

// MSAL configuration
const msalConfig: Configuration = {
  auth: {
    clientId: process.env.REACT_APP_CLIENT_ID || 'your-client-id',
    authority: process.env.REACT_APP_AUTHORITY || 'https://login.microsoftonline.com/common',
    redirectUri: process.env.REACT_APP_REDIRECT_URI || window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
};

// Create MSAL instance
export const msalInstance = new PublicClientApplication(msalConfig);

// Login request configuration
export const loginRequest = {
  scopes: ['openid', 'profile', 'email', 'User.Read'],
};

// API scopes for different services
export const apiScopes = {
  templateLibrary: [`api://${process.env.REACT_APP_TEMPLATE_LIBRARY_CLIENT_ID || 'template-library-api'}/access_as_user`],
  deployment: [`api://${process.env.REACT_APP_DEPLOYMENT_CLIENT_ID || 'deployment-api'}/access_as_user`],
  identity: [`api://${process.env.REACT_APP_IDENTITY_CLIENT_ID || 'identity-api'}/access_as_user`],
};