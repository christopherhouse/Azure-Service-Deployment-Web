import React from 'react';
import { render, screen } from '@testing-library/react';

// Mock MSAL components to avoid crypto issues in tests
jest.mock('@azure/msal-react', () => ({
  MsalProvider: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  useMsal: () => ({
    instance: {},
    accounts: [],
  }),
}));

jest.mock('@azure/msal-browser', () => ({
  PublicClientApplication: jest.fn(),
}));

// Mock Azure SDK modules to avoid dependency issues in tests
jest.mock('./services/azureDeploymentService', () => ({
  AzureDeploymentService: jest.fn(),
}));

jest.mock('./services/azureResourceDiscoveryService', () => ({
  AzureResourceDiscoveryService: jest.fn(),
}));

jest.mock('./components/SubscriptionSelector', () => ({
  SubscriptionSelector: () => <div data-testid="subscription-selector">Subscription Selector</div>,
}));

jest.mock('./components/ResourceGroupSelector', () => ({
  ResourceGroupSelector: () => <div data-testid="resource-group-selector">Resource Group Selector</div>,
}));

jest.mock('./hooks/useAzureCredential', () => ({
  useAzureCredential: () => null,
}));

import App from './App';

test('renders Azure ARM Template Deployment Tool', () => {
  render(<App />);
  const titleElement = screen.getByText(/Azure ARM Template Deployment Tool/i);
  expect(titleElement).toBeInTheDocument();
});
