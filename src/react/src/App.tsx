import React, { useState } from 'react';
import { MsalProvider } from '@azure/msal-react';
import { PublicClientApplication } from '@azure/msal-browser';
import { msalConfig } from './authConfig';
import { FileUpload } from './components/FileUpload';
import { DeploymentStatus, DeploymentState } from './components/DeploymentStatus';
import { LoginButton } from './components/LoginButton';
import { SubscriptionSelector } from './components/SubscriptionSelector';
import { ResourceGroupSelector } from './components/ResourceGroupSelector';
import { AzureDeploymentService } from './services/azureDeploymentService';
import { useAzureCredential } from './hooks/useAzureCredential';
import './App.css';

const msalInstance = new PublicClientApplication(msalConfig);

const AppContent: React.FC = () => {
  const [templateFile, setTemplateFile] = useState<File | null>(null);
  const [parametersFile, setParametersFile] = useState<File | null>(null);
  const [deploymentState, setDeploymentState] = useState<DeploymentState>('idle');
  const [deploymentMessage, setDeploymentMessage] = useState<string>('');
  const [deploymentName, setDeploymentName] = useState<string>('');
  const [selectedSubscriptionId, setSelectedSubscriptionId] = useState<string | null>(null);
  const [selectedResourceGroupName, setSelectedResourceGroupName] = useState<string | null>(null);
  
  const credential = useAzureCredential();

  const generateDeploymentName = () => {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5);
    return `arm-deployment-${timestamp}`;
  };

  const parseFile = async (file: File): Promise<any> => {
    const content = await file.text();
    try {
      return JSON.parse(content);
    } catch (error) {
      throw new Error(`Invalid JSON in file ${file.name}: ${error}`);
    }
  };

  const handleDeploy = async () => {
    if (!templateFile || !parametersFile) {
      alert('Please select both template and parameters files');
      return;
    }

    if (!credential) {
      alert('Please sign in to Azure first');
      return;
    }

    if (!selectedSubscriptionId) {
      alert('Please select a subscription');
      return;
    }

    if (!selectedResourceGroupName) {
      alert('Please select a resource group');
      return;
    }

    try {
      setDeploymentState('running');
      const newDeploymentName = generateDeploymentName();
      setDeploymentName(newDeploymentName);
      setDeploymentMessage('');

      // Parse the files
      const template = await parseFile(templateFile);
      const parameters = await parseFile(parametersFile);

      // Create deployment service and deploy
      const deploymentService = new AzureDeploymentService(credential);
      const result = await deploymentService.deployTemplate({
        template,
        parameters,
        deploymentName: newDeploymentName,
        subscriptionId: selectedSubscriptionId,
        resourceGroupName: selectedResourceGroupName,
      });

      if (result.success) {
        setDeploymentState('success');
        setDeploymentMessage('Resources deployed successfully!');
      } else {
        setDeploymentState('error');
        setDeploymentMessage(result.error || 'Unknown error occurred');
      }
    } catch (error) {
      setDeploymentState('error');
      setDeploymentMessage(error instanceof Error ? error.message : 'Unknown error occurred');
    }
  };

  const canDeploy = templateFile && parametersFile && credential && selectedSubscriptionId && selectedResourceGroupName && deploymentState !== 'running';

  return (
    <div className="App">
      <header className="App-header">
        <h1>🚀 Azure ARM Template Deployment Tool</h1>
        <p>Deploy Azure resources using ARM templates</p>
      </header>
      
      <main className="App-main">
        <LoginButton />
        
        {credential && (
          <>
            <SubscriptionSelector
              credential={credential}
              selectedSubscriptionId={selectedSubscriptionId}
              onSubscriptionChange={setSelectedSubscriptionId}
            />
            
            <ResourceGroupSelector
              credential={credential}
              subscriptionId={selectedSubscriptionId}
              selectedResourceGroupName={selectedResourceGroupName}
              onResourceGroupChange={setSelectedResourceGroupName}
            />
            
            <FileUpload
              onTemplateFileChange={setTemplateFile}
              onParametersFileChange={setParametersFile}
              templateFile={templateFile}
              parametersFile={parametersFile}
            />
            
            <div className="deploy-section">
              <button
                onClick={handleDeploy}
                disabled={!canDeploy}
                className={`deploy-button ${!canDeploy ? 'disabled' : ''}`}
              >
                {deploymentState === 'running' ? '⏳ Deploying...' : '🚀 Deploy to Azure'}
              </button>
              
              {selectedSubscriptionId && selectedResourceGroupName && (
                <div className="deployment-info">
                  <p><strong>Target Subscription:</strong> {selectedSubscriptionId}</p>
                  <p><strong>Target Resource Group:</strong> {selectedResourceGroupName}</p>
                </div>
              )}
            </div>
            
            <DeploymentStatus
              state={deploymentState}
              message={deploymentMessage}
              deploymentName={deploymentName}
              resourceGroup={selectedResourceGroupName || undefined}
            />
          </>
        )}
        
        {!credential && (
          <div className="not-authenticated">
            <h3>🔐 Authentication Required</h3>
            <p>Please sign in with your Microsoft account to deploy Azure resources.</p>
          </div>
        )}
      </main>
    </div>
  );
};

function App() {
  return (
    <MsalProvider instance={msalInstance}>
      <AppContent />
    </MsalProvider>
  );
}

export default App;
