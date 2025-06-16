import { AzureDeploymentService } from './azureDeploymentService';
import { TokenCredential } from '@azure/core-auth';

// Mock the Azure SDK
jest.mock('@azure/arm-resources', () => ({
  ResourceManagementClient: jest.fn().mockImplementation(() => ({
    deployments: {
      beginCreateOrUpdateAndWait: jest.fn(),
      get: jest.fn(),
    },
  })),
}));

// Mock environment variables
process.env.REACT_APP_AZURE_SUBSCRIPTION_ID = 'test-subscription-id';
process.env.REACT_APP_AZURE_RESOURCE_GROUP = 'test-resource-group';

describe('AzureDeploymentService', () => {
  let mockCredential: TokenCredential;
  let service: AzureDeploymentService;

  beforeEach(() => {
    mockCredential = {} as TokenCredential;
    service = new AzureDeploymentService(mockCredential);
  });

  describe('extractParameters', () => {
    it('should extract parameters from full ARM parameter file structure', () => {
      const fullParameterFile = {
        $schema: 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#',
        contentVersion: '1.0.0.0',
        parameters: {
          storageAccountName: {
            value: 'examplestorage123'
          },
          location: {
            value: 'East US'
          },
          storageAccountSku: {
            value: 'Standard_LRS'
          }
        }
      };

      // Access the private method through type assertion
      const extractParameters = (service as any).extractParameters.bind(service);
      const result = extractParameters(fullParameterFile);

      expect(result).toEqual({
        storageAccountName: {
          value: 'examplestorage123'
        },
        location: {
          value: 'East US'
        },
        storageAccountSku: {
          value: 'Standard_LRS'
        }
      });
    });

    it('should return parameters as-is when already in correct format', () => {
      const directParameters = {
        storageAccountName: {
          value: 'examplestorage123'
        },
        location: {
          value: 'East US'
        },
        storageAccountSku: {
          value: 'Standard_LRS'
        }
      };

      // Access the private method through type assertion
      const extractParameters = (service as any).extractParameters.bind(service);
      const result = extractParameters(directParameters);

      expect(result).toEqual(directParameters);
    });

    it('should handle null/undefined parameters', () => {
      // Access the private method through type assertion
      const extractParameters = (service as any).extractParameters.bind(service);
      
      expect(extractParameters(null)).toBeNull();
      expect(extractParameters(undefined)).toBeUndefined();
    });

    it('should handle empty object', () => {
      // Access the private method through type assertion
      const extractParameters = (service as any).extractParameters.bind(service);
      const result = extractParameters({});

      expect(result).toEqual({});
    });
  });
});