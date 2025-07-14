import { AzureResourceDiscoveryService } from './azureResourceDiscoveryService';
import { TokenCredential } from '@azure/core-auth';

// Mock the Azure SDK completely to avoid dependency issues
jest.mock('@azure/arm-subscriptions', () => ({}));
jest.mock('@azure/arm-resources', () => ({}));

describe('AzureResourceDiscoveryService', () => {
  let mockCredential: TokenCredential;
  let service: AzureResourceDiscoveryService;

  beforeEach(() => {
    mockCredential = {} as TokenCredential;
    service = new AzureResourceDiscoveryService(mockCredential);
  });

  it('should be instantiated with a credential', () => {
    expect(service).toBeInstanceOf(AzureResourceDiscoveryService);
  });
});