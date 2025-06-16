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

jest.mock('./hooks/useAzureCredential', () => ({
  useAzureCredential: () => null,
}));

import App from './App';

test('renders Azure ARM Template Deployment Tool', () => {
  render(<App />);
  const titleElement = screen.getByText(/Azure ARM Template Deployment Tool/i);
  expect(titleElement).toBeInTheDocument();
});
