import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { 
  FaTachometerAlt, 
  FaStore, 
  FaCreditCard, 
  FaHistory, 
  FaBars, 
  FaTimes,
  FaExternalLinkAlt,
  FaSignOutAlt,
  FaShoppingCart
} from 'react-icons/fa';
import './Layout.css';

const Layout = ({ children, onLogout }) => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const location = useLocation();

  const menuItems = [
    { path: '/', icon: FaTachometerAlt, label: 'Dashboard' },
    { path: '/merchants', icon: FaStore, label: 'Merchants' },
    { path: '/payment-methods', icon: FaCreditCard, label: 'Payment Methods' },
    { path: '/transactions', icon: FaHistory, label: 'Transactions' },
  ];

  const toggleSidebar = () => {
    setSidebarOpen(!sidebarOpen);
  };

  return (
    <div className="layout">
      {/* Sidebar */}
      <div className={`sidebar ${sidebarOpen ? 'open' : ''}`}>
        <div className="sidebar-header">
          <h2>PSP Admin</h2>
          <button className="sidebar-toggle" onClick={toggleSidebar}>
            <FaTimes />
          </button>
        </div>
        
        <nav className="sidebar-nav">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;
            
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`nav-item ${isActive ? 'active' : ''}`}
                onClick={() => setSidebarOpen(false)}
              >
                <Icon className="nav-icon" />
                <span className="nav-label">{item.label}</span>
              </Link>
            );
          })}
          
          <div className="nav-divider"></div>
          
          <a
            href="/webshop"
            className="nav-item webshop-link"
            onClick={() => setSidebarOpen(false)}
          >
            <FaStore className="nav-icon" />
            <span className="nav-label">WebShop Portal</span>
            <FaExternalLinkAlt className="external-icon" />
          </a>
          
          
        </nav>
      </div>

      {/* Main Content */}
      <div className="main-content">
        {/* Top Bar */}
        <header className="top-bar">
          <button className="mobile-menu-toggle" onClick={toggleSidebar}>
            <FaBars />
          </button>
          <div className="top-bar-content">
            <h1>Payment Service Provider</h1>
            <div className="user-info">
              <span>Admin Panel</span>
              {onLogout && (
                <button className="logout-btn" onClick={onLogout}>
                  <FaSignOutAlt />
                  Logout
                </button>
              )}
            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="page-content">
          {children}
        </main>
      </div>

      {/* Overlay for mobile */}
      {sidebarOpen && (
        <div className="sidebar-overlay" onClick={() => setSidebarOpen(false)} />
      )}
    </div>
  );
};

export default Layout;
