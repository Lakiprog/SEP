import React, { useState } from 'react';
import { webShopAPI } from '../services/api';
import { FaStore, FaLock, FaUser, FaSpinner } from 'react-icons/fa';
import './WebShopLogin.css';

const WebShopLogin = ({ onLoginSuccess }) => {
  const [formData, setFormData] = useState({
    merchantId: '',
    merchantPassword: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await webShopAPI.login(formData);
      
      if (response.data.success) {
        // Store token and client info
        localStorage.setItem('webshopToken', response.data.token);
        localStorage.setItem('webshopClient', JSON.stringify(response.data.client));
        
        // Call success callback
        onLoginSuccess(response.data.client);
      } else {
        setError('Login failed. Please check your credentials.');
      }
    } catch (err) {
      setError(err.response?.data?.message || 'Login failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="webshop-login">
      <div className="login-container">
        <div className="login-header">
          <div className="login-icon">
            <FaStore />
          </div>
          <h1>WebShop Login</h1>
          <p>Sign in to manage your payment methods</p>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          {error && (
            <div className="error-message">
              {error}
            </div>
          )}

          <div className="form-group">
            <label htmlFor="merchantId">
              <FaUser className="input-icon" />
              Merchant ID
            </label>
            <input
              type="text"
              id="merchantId"
              name="merchantId"
              value={formData.merchantId}
              onChange={handleChange}
              placeholder="Enter your Merchant ID"
              required
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="merchantPassword">
              <FaLock className="input-icon" />
              Password
            </label>
            <input
              type="password"
              id="merchantPassword"
              name="merchantPassword"
              value={formData.merchantPassword}
              onChange={handleChange}
              placeholder="Enter your password"
              required
              disabled={loading}
            />
          </div>

          <button 
            type="submit" 
            className="login-btn"
            disabled={loading}
          >
            {loading ? (
              <>
                <FaSpinner className="spinner" />
                Signing In...
              </>
            ) : (
              'Sign In'
            )}
          </button>
        </form>

        <div className="login-footer">
          <p>Demo Credentials:</p>
          <p><strong>Merchant ID:</strong> TELECOM_001</p>
          <p><strong>Password:</strong> telecom123</p>
        </div>
      </div>
    </div>
  );
};

export default WebShopLogin;
