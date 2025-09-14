import React, { useState, useEffect } from 'react';
import { adminAPI } from '../services/api';
import { 
  FaStore, 
  FaCreditCard, 
  FaHistory, 
  FaDollarSign,
  FaCheckCircle,
  FaSpinner
} from 'react-icons/fa';
import './Dashboard.css';

const Dashboard = () => {
  const [statistics, setStatistics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadStatistics();
  }, []);

  const loadStatistics = async () => {
    try {
      setLoading(true);
      const response = await adminAPI.getStatistics();
      setStatistics(response.data);
      setError(null);
    } catch (err) {
      setError('Failed to load statistics');
      console.error('Error loading statistics:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="dashboard-loading">
        <FaSpinner className="spinner" />
        <p>Loading dashboard...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="dashboard-error">
        <h2>Error</h2>
        <p>{error}</p>
        <button onClick={loadStatistics} className="retry-btn">
          Retry
        </button>
      </div>
    );
  }

  const statCards = [
    {
      title: 'Total Merchants',
      value: statistics?.totalMerchants || 0,
      icon: FaStore,
      color: '#667eea',
      change: '+12%'
    },
    {
      title: 'Active Merchants',
      value: statistics?.activeMerchants || 0,
      icon: FaCheckCircle,
      color: '#28a745',
      change: '+8%'
    },
    {
      title: 'Payment Methods',
      value: statistics?.totalPaymentMethods || 0,
      icon: FaCreditCard,
      color: '#17a2b8',
      change: '+5%'
    },
    {
      title: 'Enabled Methods',
      value: statistics?.enabledPaymentMethods || 0,
      icon: FaCreditCard,
      color: '#ffc107',
      change: '+3%'
    },
    {
      title: 'Total Transactions',
      value: statistics?.totalTransactions || 0,
      icon: FaHistory,
      color: '#6f42c1',
      change: '+15%'
    },
    {
      title: 'Completed',
      value: statistics?.completedTransactions || 0,
      icon: FaCheckCircle,
      color: '#28a745',
      change: '+10%'
    },
    {
      title: 'Total Volume',
      value: `$${(statistics?.totalVolume || 0).toFixed(2)}`,
      icon: FaDollarSign,
      color: '#dc3545',
      change: '+20%'
    }
  ];

  return (
    <div className="dashboard">
      <div className="dashboard-header">
        <h1>Dashboard</h1>
        <p>Payment Service Provider Overview</p>
      </div>

      <div className="stats-grid">
        {statCards.map((stat, index) => {
          const Icon = stat.icon;
          return (
            <div key={index} className="stat-card">
              <div className="stat-card-header">
                <div className="stat-icon" style={{ backgroundColor: stat.color }}>
                  <Icon />
                </div>
                <div className="stat-change positive">
                  {stat.change}
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

      <div className="dashboard-sections">
        <div className="dashboard-section">
          <h2>Recent Activity</h2>
          <div className="activity-list">
            <div className="activity-item">
              <div className="activity-icon">
                <FaStore />
              </div>
              <div className="activity-content">
                <p><strong>New merchant registered:</strong> Telekom Srbija</p>
                <span className="activity-time">2 hours ago</span>
              </div>
            </div>
            <div className="activity-item">
              <div className="activity-icon">
                <FaCreditCard />
              </div>
              <div className="activity-content">
                <p><strong>Payment method added:</strong> Payoneer</p>
                <span className="activity-time">4 hours ago</span>
              </div>
            </div>
            <div className="activity-item">
              <div className="activity-icon">
                <FaHistory />
              </div>
              <div className="activity-content">
                <p><strong>Transaction completed:</strong> $49.99</p>
                <span className="activity-time">6 hours ago</span>
              </div>
            </div>
          </div>
        </div>

        <div className="dashboard-section">
          <h2>Quick Actions</h2>
          <div className="quick-actions">
            <button className="quick-action-btn">
              <FaStore />
              <span>Add Merchant</span>
            </button>
            <button className="quick-action-btn">
              <FaCreditCard />
              <span>Add Payment Method</span>
            </button>
            <button className="quick-action-btn">
              <FaHistory />
              <span>View Transactions</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
