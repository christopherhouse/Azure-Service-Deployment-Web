import React from 'react';
import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../authConfig';
import './LoginButton.css';

export const LoginButton: React.FC = () => {
  const { instance, accounts } = useMsal();

  const handleLogin = () => {
    instance.loginPopup(loginRequest).catch(e => {
      console.error(e);
    });
  };

  const handleLogout = () => {
    instance.logoutPopup().catch(e => {
      console.error(e);
    });
  };

  if (accounts.length > 0) {
    return (
      <div className="auth-container">
        <div className="user-info">
          <span>👋 Hello, {accounts[0].name || accounts[0].username}</span>
        </div>
        <button onClick={handleLogout} className="auth-button logout-button">
          🚪 Sign Out
        </button>
      </div>
    );
  }

  return (
    <div className="auth-container">
      <button onClick={handleLogin} className="auth-button login-button">
        🔐 Sign in with Microsoft
      </button>
    </div>
  );
};