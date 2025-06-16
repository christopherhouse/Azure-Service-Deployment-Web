import React from 'react';
import './DeploymentStatus.css';

export type DeploymentState = 'idle' | 'running' | 'success' | 'error';

interface DeploymentStatusProps {
  state: DeploymentState;
  message?: string;
  deploymentName?: string;
  resourceGroup?: string;
}

export const DeploymentStatus: React.FC<DeploymentStatusProps> = ({
  state,
  message,
  deploymentName,
  resourceGroup,
}) => {
  const renderStatus = () => {
    switch (state) {
      case 'idle':
        return null;
      
      case 'running':
        return (
          <div className="status-container status-running">
            <div className="spinner"></div>
            <div className="status-content">
              <h3>🚀 Deployment in Progress</h3>
              <p>Deploying to resource group: <strong>{resourceGroup}</strong></p>
              <p>Deployment name: <strong>{deploymentName}</strong></p>
              <p>Please wait while we deploy your resources...</p>
            </div>
          </div>
        );
      
      case 'success':
        return (
          <div className="status-container status-success">
            <div className="status-content">
              <h3>🎉 Deployment Successful! ✨</h3>
              <p>🎊 Your Azure resources have been deployed successfully! 🎊</p>
              <p>Resource Group: <strong>{resourceGroup}</strong></p>
              <p>Deployment: <strong>{deploymentName}</strong></p>
              {message && <p className="success-message">{message}</p>}
            </div>
          </div>
        );
      
      case 'error':
        return (
          <div className="status-container status-error">
            <div className="status-content">
              <h3>🛑 Deployment Failed</h3>
              <p>❌ There was an error deploying your resources.</p>
              <p>Resource Group: <strong>{resourceGroup}</strong></p>
              <p>Deployment: <strong>{deploymentName}</strong></p>
              {message && (
                <div className="error-message">
                  <strong>Error Details:</strong>
                  <pre>{message}</pre>
                </div>
              )}
            </div>
          </div>
        );
      
      default:
        return null;
    }
  };

  return renderStatus();
};