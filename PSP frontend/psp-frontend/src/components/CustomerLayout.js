import React from 'react';
import './CustomerLayout.css';

const CustomerLayout = ({ children }) => {
  return (
    <div className="customer-layout">
      {/* Simple customer header without admin navigation */}
      <header className="customer-header">
        <div className="header-content">
          <h1>🏪 Secure Payment</h1>
          <div className="security-badge">
            <span className="lock-icon">🔒</span>
            <span>SSL Protected</span>
          </div>
        </div>
      </header>

      {/* Customer content area */}
      <main className="customer-content">
        {children}
      </main>

      {/* Simple customer footer */}
      <footer className="customer-footer">
        <div className="footer-content">
          <p>&copy; 2024 Payment Service Provider. All rights reserved.</p>
          <div className="trust-badges">
            <span>🛡️ Secure</span>
            <span>⚡ Fast</span>
            <span>✅ Trusted</span>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default CustomerLayout;
