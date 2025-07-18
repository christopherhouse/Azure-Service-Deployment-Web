import React from 'react';
import { useMsal } from '@azure/msal-react';

const Navbar: React.FC = () => {
  const { instance, accounts } = useMsal();

  const handleLogout = () => {
    instance.logoutRedirect({
      postLogoutRedirectUri: '/',
    });
  };

  const user = accounts[0];

  return (
    <nav className="navbar navbar-expand-lg navbar-dark bg-primary">
      <div className="container-fluid">
        <a className="navbar-brand" href="/">
          <i className="fas fa-cloud me-2"></i>
          Azure Deployment SaaS
        </a>
        
        <div className="navbar-nav ms-auto">
          <div className="nav-item dropdown">
            <a
              className="nav-link dropdown-toggle text-white"
              href="#"
              id="navbarDropdown"
              role="button"
              data-bs-toggle="dropdown"
            >
              <i className="fas fa-user-circle me-2"></i>
              {user?.name || user?.username || 'User'}
            </a>
            <ul className="dropdown-menu">
              <li><a className="dropdown-item" href="/account">Account Settings</a></li>
              <li><hr className="dropdown-divider" /></li>
              <li>
                <button className="dropdown-item" onClick={handleLogout}>
                  <i className="fas fa-sign-out-alt me-2"></i>
                  Sign Out
                </button>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </nav>
  );
};

export default Navbar;