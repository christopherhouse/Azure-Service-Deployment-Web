import React from 'react';
import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../services/authConfig';

const Login: React.FC = () => {
  const { instance } = useMsal();

  const handleLogin = () => {
    instance.loginRedirect(loginRequest);
  };

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-md-6 col-lg-5">
            <div className="card shadow">
              <div className="card-body p-5">
                <div className="text-center mb-4">
                  <i className="fas fa-cloud fa-3x text-primary mb-3"></i>
                  <h2 className="mb-3">Azure Deployment SaaS</h2>
                  <p className="text-muted">
                    Deploy ARM templates with ease using our cloud-native platform
                  </p>
                </div>

                <div className="d-grid gap-3">
                  <button
                    className="btn btn-primary btn-lg"
                    onClick={handleLogin}
                  >
                    <i className="fab fa-microsoft me-2"></i>
                    Sign in with Microsoft
                  </button>
                </div>

                <div className="row mt-4 text-center">
                  <div className="col-md-4">
                    <i className="fas fa-file-code fa-2x text-primary mb-2"></i>
                    <h6>Template Library</h6>
                    <small className="text-muted">Manage ARM templates</small>
                  </div>
                  <div className="col-md-4">
                    <i className="fas fa-rocket fa-2x text-primary mb-2"></i>
                    <h6>Easy Deployment</h6>
                    <small className="text-muted">Deploy to Azure</small>
                  </div>
                  <div className="col-md-4">
                    <i className="fas fa-search fa-2x text-primary mb-2"></i>
                    <h6>AI Search</h6>
                    <small className="text-muted">Find templates fast</small>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;