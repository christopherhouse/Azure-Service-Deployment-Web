import React from 'react';
import { Link, useLocation } from 'react-router-dom';

const Sidebar: React.FC = () => {
  const location = useLocation();

  const isActive = (path: string) => location.pathname === path;

  const menuItems = [
    { path: '/', icon: 'fas fa-tachometer-alt', label: 'Dashboard' },
    { path: '/templates', icon: 'fas fa-file-code', label: 'Template Library' },
    { path: '/deployments', icon: 'fas fa-rocket', label: 'Deployments' },
    { path: '/account', icon: 'fas fa-user-cog', label: 'Account' },
  ];

  return (
    <div className="position-sticky pt-3">
      <ul className="nav flex-column">
        {menuItems.map(item => (
          <li key={item.path} className="nav-item">
            <Link
              className={`nav-link ${isActive(item.path) ? 'active' : ''}`}
              to={item.path}
            >
              <i className={`${item.icon} me-2`}></i>
              {item.label}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
};

export default Sidebar;