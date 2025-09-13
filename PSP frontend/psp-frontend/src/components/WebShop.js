import React, { useState, useEffect } from 'react';
import WebShopLogin from './WebShopLogin';
import WebShopDashboard from './WebShopDashboard';

const WebShop = () => {
  const [client, setClient] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if user is already logged in
    const token = localStorage.getItem('webshopToken');
    const clientData = localStorage.getItem('webshopClient');

    if (token && clientData) {
      try {
        const parsedClient = JSON.parse(clientData);
        setClient(parsedClient);
      } catch (error) {
        // Invalid client data, clear storage
        localStorage.removeItem('webshopToken');
        localStorage.removeItem('webshopClient');
      }
    }
    
    setLoading(false);
  }, []);

  const handleLoginSuccess = (clientData) => {
    setClient(clientData);
  };

  const handleLogout = () => {
    setClient(null);
  };

  if (loading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        fontSize: '18px',
        color: '#666'
      }}>
        Loading...
      </div>
    );
  }

  return (
    <div className="webshop">
      {client ? (
        <WebShopDashboard 
          client={client} 
          onLogout={handleLogout}
        />
      ) : (
        <WebShopLogin onLoginSuccess={handleLoginSuccess} />
      )}
    </div>
  );
};

export default WebShop;
