import React, { useState, useEffect, useCallback } from 'react';
import { webShopAPI } from '../services/api';
import { 
  FaStore, 
  FaCreditCard, 
  FaHistory, 
  FaDollarSign,
  FaCheckCircle,
  FaSpinner,
  FaCog,
  FaSignOutAlt
} from 'react-icons/fa';
import './WebShopDashboard.css';

const WebShopDashboard = ({ client, onLogout }) => {
  const [dashboard, setDashboard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [activeTab, setActiveTab] = useState('overview');

  const getStatusString = (status) => {
    const statusMap = {
      0: 'Pending',
      1: 'Processing', 
      2: 'Completed',
      3: 'Failed',
      4: 'Cancelled',
      5: 'Refunded'
    };
    return statusMap[status] || 'Unknown';
  };

  const loadDashboard = useCallback(async () => {
    try {
      setLoading(true);
      const response = await webShopAPI.getDashboard(client.id);
      setDashboard(response.data);
      setError(null);
    } catch (err) {
      setError('Failed to load dashboard data');
      console.error('Error loading dashboard:', err);
    } finally {
      setLoading(false);
    }
  }, [client]);

  useEffect(() => {
    if (client) {
      loadDashboard();
    }
  }, [client, loadDashboard]);

  const handleLogout = () => {
    localStorage.removeItem('webshopToken');
    localStorage.removeItem('webshopClient');
    onLogout();
  };

  if (loading) {
    return (
      <div className="webshop-dashboard-loading">
        <FaSpinner className="spinner" />
        <p>Loading dashboard...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="webshop-dashboard-error">
        <h2>Error</h2>
        <p>{error}</p>
        <button onClick={loadDashboard} className="retry-btn">
          Retry
        </button>
      </div>
    );
  }

  const statCards = [
    {
      title: 'Payment Methods',
      value: dashboard?.paymentMethodsCount || 0,
      icon: FaCreditCard,
      color: '#667eea'
    },
    {
      title: 'Total Transactions',
      value: dashboard?.totalTransactions || 0,
      icon: FaHistory,
      color: '#17a2b8'
    },
    {
      title: 'Completed',
      value: dashboard?.completedTransactions || 0,
      icon: FaCheckCircle,
      color: '#28a745'
    },
    {
      title: 'Total Volume',
      value: `$${(dashboard?.totalVolume || 0).toFixed(2)}`,
      icon: FaDollarSign,
      color: '#ffc107'
    }
  ];

  return (
    <div className="webshop-dashboard">
      <div className="dashboard-header">
        <div className="header-left">
          <div className="client-info">
            <div className="client-icon">
              <FaStore />
            </div>
            <div className="client-details">
              <h1>{client.name}</h1>
              <p>{client.description}</p>
              <span className="client-id">ID: {client.merchantId}</span>
            </div>
          </div>
        </div>
        <div className="header-right">
          <button className="settings-btn" onClick={() => setActiveTab('settings')}>
            <FaCog />
            Settings
          </button>
          <button className="logout-btn" onClick={handleLogout}>
            <FaSignOutAlt />
            Logout
          </button>
        </div>
      </div>

      <div className="dashboard-tabs">
        <button 
          className={`tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
          onClick={() => setActiveTab('overview')}
        >
          Overview
        </button>
        <button 
          className={`tab-btn ${activeTab === 'transactions' ? 'active' : ''}`}
          onClick={() => setActiveTab('transactions')}
        >
          Transactions
        </button>
        <button 
          className={`tab-btn ${activeTab === 'settings' ? 'active' : ''}`}
          onClick={() => setActiveTab('settings')}
        >
          Payment Methods
        </button>
      </div>

      <div className="dashboard-content">
        {activeTab === 'overview' && (
          <div className="overview-tab">
            <div className="stats-grid">
              {statCards.map((stat, index) => {
                const Icon = stat.icon;
                return (
                  <div key={index} className="stat-card">
                    <div className="stat-card-header">
                      <div className="stat-icon" style={{ backgroundColor: stat.color }}>
                        <Icon />
                      </div>
                    </div>
                    <div className="stat-card-body">
                      <h3>{stat.value}</h3>
                      <p>{stat.title}</p>
                    </div>
                  </div>
                );
              })}
            </div>

            <div className="recent-transactions">
              <h2>Recent Transactions</h2>
              <div className="transactions-list">
                {dashboard?.recentTransactions?.length > 0 ? (
                  dashboard.recentTransactions.map((transaction) => (
                    <div key={transaction.id} className="transaction-item">
                      <div className="transaction-icon">
                        <FaCreditCard />
                      </div>
                      <div className="transaction-content">
                        <div className="transaction-main">
                          <span className="amount">${transaction.amount.toFixed(2)}</span>
                          <span className={`status status-${getStatusString(transaction.status).toLowerCase()}`}>
                            {getStatusString(transaction.status)}
                          </span>
                        </div>
                        <div className="transaction-details">
                          <span className="payment-type">{transaction.paymentType}</span>
                          <span className="date">
                            {new Date(transaction.createdAt).toLocaleDateString()}
                          </span>
                        </div>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="no-transactions">
                    <p>No recent transactions</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {activeTab === 'transactions' && (
          <div className="transactions-tab">
            <h2>Transaction History</h2>
            <div className="transactions-table">
              <div className="table-header">
                <div>Amount</div>
                <div>Status</div>
                <div>Payment Method</div>
                <div>Date</div>
              </div>
              {dashboard?.recentTransactions?.length > 0 ? (
                dashboard.recentTransactions.map((transaction) => (
                  <div key={transaction.id} className="table-row">
                    <div className="amount">${transaction.amount.toFixed(2)}</div>
                    <div className={`status status-${getStatusString(transaction.status).toLowerCase()}`}>
                      {getStatusString(transaction.status)}
                    </div>
                    <div className="payment-type">{transaction.paymentType}</div>
                    <div className="date">
                      {new Date(transaction.createdAt).toLocaleDateString()}
                    </div>
                  </div>
                ))
              ) : (
                <div className="no-data">
                  <p>No transactions found</p>
                </div>
              )}
            </div>
          </div>
        )}

        {activeTab === 'settings' && (
          <PaymentMethodsSettings clientId={client.id} />
        )}
      </div>
    </div>
  );
};

// Payment Methods Settings Component
const PaymentMethodsSettings = ({ clientId }) => {
  const [paymentMethods, setPaymentMethods] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);

  const loadPaymentMethods = useCallback(async () => {
    try {
      setLoading(true);
      const response = await webShopAPI.getAvailablePaymentMethods(clientId);
      setPaymentMethods(response.data);
      setError(null);
    } catch (err) {
      setError('Failed to load payment methods');
      console.error('Error loading payment methods:', err);
    } finally {
      setLoading(false);
    }
  }, [clientId]);

  useEffect(() => {
    loadPaymentMethods();
  }, [clientId, loadPaymentMethods]);

  const handleTogglePaymentMethod = (paymentMethodId) => {
    setPaymentMethods(prev => 
      prev.map(pm => 
        pm.id === paymentMethodId 
          ? { ...pm, isSelected: !pm.isSelected }
          : pm
      )
    );
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setSuccess(false);
      
      const selectedPaymentTypeIds = paymentMethods
        .filter(pm => pm.isSelected)
        .map(pm => pm.id);

      await webShopAPI.updatePaymentMethods(clientId, {
        selectedPaymentTypeIds
      });

      setSuccess(true);
      setTimeout(() => setSuccess(false), 3000);
    } catch (err) {
      setError('Failed to update payment methods');
      console.error('Error updating payment methods:', err);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="payment-methods-loading">
        <FaSpinner className="spinner" />
        <p>Loading payment methods...</p>
      </div>
    );
  }

  return (
    <div className="payment-methods-settings">
      <h2>Payment Methods Settings</h2>
      <p>Select which payment methods you want to offer to your customers.</p>

      {error && (
        <div className="error-message">
          {error}
        </div>
      )}

      {success && (
        <div className="success-message">
          Payment methods updated successfully!
        </div>
      )}

      <div className="payment-methods-list">
        {paymentMethods.map((method) => (
          <div key={method.id} className="payment-method-item">
            <div className="method-info">
              <div className="method-icon">
                <FaCreditCard />
              </div>
              <div className="method-details">
                <h3>{method.name}</h3>
                <p>{method.description}</p>
                <span className="method-type">{method.type}</span>
              </div>
            </div>
            <div className="method-toggle">
              <label className="toggle-switch">
                <input
                  type="checkbox"
                  checked={method.isSelected}
                  onChange={() => handleTogglePaymentMethod(method.id)}
                  disabled={saving}
                />
                <span className="slider"></span>
              </label>
            </div>
          </div>
        ))}
      </div>

      <div className="save-section">
        <button 
          className="save-btn"
          onClick={handleSave}
          disabled={saving}
        >
          {saving ? (
            <>
              <FaSpinner className="spinner" />
              Saving...
            </>
          ) : (
            'Save Changes'
          )}
        </button>
      </div>
    </div>
  );
};

export default WebShopDashboard;
