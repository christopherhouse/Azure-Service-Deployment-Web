import React, { useState, useEffect } from 'react';
import { AzureResourceDiscoveryService, AzureSubscription } from '../services/azureResourceDiscoveryService';
import { TokenCredential } from '@azure/core-auth';
import './SubscriptionSelector.css';

interface SubscriptionSelectorProps {
  credential: TokenCredential | null;
  selectedSubscriptionId: string | null;
  onSubscriptionChange: (subscriptionId: string | null) => void;
}

export const SubscriptionSelector: React.FC<SubscriptionSelectorProps> = ({
  credential,
  selectedSubscriptionId,
  onSubscriptionChange,
}) => {
  const [subscriptions, setSubscriptions] = useState<AzureSubscription[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadSubscriptions = async () => {
      if (!credential) {
        setSubscriptions([]);
        setError(null);
        return;
      }

      setLoading(true);
      setError(null);

      try {
        const discoveryService = new AzureResourceDiscoveryService(credential);
        const subs = await discoveryService.getSubscriptions();
        setSubscriptions(subs);
        
        // If no subscription is selected but we have subscriptions, select the first one
        if (!selectedSubscriptionId && subs.length > 0) {
          onSubscriptionChange(subs[0].id);
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to load subscriptions';
        setError(errorMessage);
        console.error('Error loading subscriptions:', err);
      } finally {
        setLoading(false);
      }
    };

    loadSubscriptions();
  }, [credential, selectedSubscriptionId, onSubscriptionChange]);

  const handleSubscriptionChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const value = event.target.value;
    onSubscriptionChange(value || null);
  };

  if (!credential) {
    return null;
  }

  return (
    <div className="subscription-selector">
      <label htmlFor="subscription-select">
        <strong>📋 Select Subscription:</strong>
      </label>
      
      {loading && (
        <div className="loading-indicator">
          ⏳ Loading subscriptions...
        </div>
      )}
      
      {error && (
        <div className="error-message">
          ❌ {error}
        </div>
      )}
      
      {!loading && !error && (
        <select
          id="subscription-select"
          value={selectedSubscriptionId || ''}
          onChange={handleSubscriptionChange}
          className="subscription-dropdown"
          disabled={subscriptions.length === 0}
        >
          <option value="">Select a subscription...</option>
          {subscriptions.map((subscription) => (
            <option key={subscription.id} value={subscription.id}>
              {subscription.displayName} ({subscription.id})
            </option>
          ))}
        </select>
      )}
      
      {!loading && !error && subscriptions.length === 0 && (
        <div className="no-subscriptions">
          ⚠️ No enabled subscriptions found
        </div>
      )}
    </div>
  );
};