import React, { useState, useEffect } from 'react';
import { AzureResourceDiscoveryService, AzureResourceGroup } from '../services/azureResourceDiscoveryService';
import { TokenCredential } from '@azure/core-auth';
import './ResourceGroupSelector.css';

interface ResourceGroupSelectorProps {
  credential: TokenCredential | null;
  subscriptionId: string | null;
  selectedResourceGroupName: string | null;
  onResourceGroupChange: (resourceGroupName: string | null) => void;
}

export const ResourceGroupSelector: React.FC<ResourceGroupSelectorProps> = ({
  credential,
  subscriptionId,
  selectedResourceGroupName,
  onResourceGroupChange,
}) => {
  const [resourceGroups, setResourceGroups] = useState<AzureResourceGroup[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadResourceGroups = async () => {
      if (!credential || !subscriptionId) {
        setResourceGroups([]);
        setError(null);
        onResourceGroupChange(null);
        return;
      }

      setLoading(true);
      setError(null);

      try {
        const discoveryService = new AzureResourceDiscoveryService(credential);
        const rgs = await discoveryService.getResourceGroups(subscriptionId);
        setResourceGroups(rgs);
        
        // If a resource group was previously selected but is no longer available, clear it
        if (selectedResourceGroupName && !rgs.some(rg => rg.name === selectedResourceGroupName)) {
          onResourceGroupChange(null);
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to load resource groups';
        setError(errorMessage);
        console.error('Error loading resource groups:', err);
        onResourceGroupChange(null);
      } finally {
        setLoading(false);
      }
    };

    loadResourceGroups();
  }, [credential, subscriptionId, selectedResourceGroupName, onResourceGroupChange]);

  const handleResourceGroupChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const value = event.target.value;
    onResourceGroupChange(value || null);
  };

  if (!credential || !subscriptionId) {
    return (
      <div className="resource-group-selector">
        <label htmlFor="resource-group-select">
          <strong>📁 Select Resource Group:</strong>
        </label>
        <div className="disabled-message">
          Please select a subscription first
        </div>
      </div>
    );
  }

  return (
    <div className="resource-group-selector">
      <label htmlFor="resource-group-select">
        <strong>📁 Select Resource Group:</strong>
      </label>
      
      <div className="resource-group-dropdown-container">
        <select
          id="resource-group-select"
          value={selectedResourceGroupName || ''}
          onChange={handleResourceGroupChange}
          className="resource-group-dropdown"
          disabled={loading || resourceGroups.length === 0}
        >
          <option value="">
            {loading ? 'Loading resource groups...' : 'Select a resource group...'}
          </option>
          {!loading && resourceGroups.map((resourceGroup) => (
            <option key={resourceGroup.name} value={resourceGroup.name}>
              {resourceGroup.name} ({resourceGroup.location})
            </option>
          ))}
        </select>
        
        {loading && (
          <div className="resource-group-loading-overlay">
            <div className="resource-group-spinner"></div>
          </div>
        )}
      </div>
      
      {error && (
        <div className="error-message">
          ❌ {error}
        </div>
      )}
      
      {!loading && !error && resourceGroups.length === 0 && (
        <div className="no-resource-groups">
          ⚠️ No resource groups found in this subscription
        </div>
      )}
    </div>
  );
};