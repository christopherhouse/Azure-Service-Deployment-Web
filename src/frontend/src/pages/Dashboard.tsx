import React from 'react';

const Dashboard: React.FC = () => {
  return (
    <div className="container-fluid py-4">
      <h1>
        <i className="fas fa-tachometer-alt me-2"></i>
        Dashboard
      </h1>
      <div className="row">
        <div className="col-md-3">
          <div className="card text-white bg-primary mb-3">
            <div className="card-body">
              <div className="d-flex justify-content-between">
                <div>
                  <h5 className="card-title">Templates</h5>
                  <h2>42</h2>
                </div>
                <i className="fas fa-file-code fa-2x"></i>
              </div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card text-white bg-success mb-3">
            <div className="card-body">
              <div className="d-flex justify-content-between">
                <div>
                  <h5 className="card-title">Deployments</h5>
                  <h2>128</h2>
                </div>
                <i className="fas fa-rocket fa-2x"></i>
              </div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card text-white bg-warning mb-3">
            <div className="card-body">
              <div className="d-flex justify-content-between">
                <div>
                  <h5 className="card-title">Running</h5>
                  <h2>3</h2>
                </div>
                <i className="fas fa-spinner fa-2x"></i>
              </div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card text-white bg-info mb-3">
            <div className="card-body">
              <div className="d-flex justify-content-between">
                <div>
                  <h5 className="card-title">Success Rate</h5>
                  <h2>94%</h2>
                </div>
                <i className="fas fa-chart-line fa-2x"></i>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;