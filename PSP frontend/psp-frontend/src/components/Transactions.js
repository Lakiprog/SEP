import React, { useState, useEffect } from 'react';
import { adminAPI } from '../services/api';
import { toast } from 'react-toastify';
import './Transactions.css';

const Transactions = () => {
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({
    status: '',
    merchant: '',
    dateFrom: '',
    dateTo: ''
  });

  useEffect(() => {
    loadTransactions();
  }, []);

  const loadTransactions = async () => {
    try {
      setLoading(true);
      const response = await adminAPI.getTransactions();
      setTransactions(response.data);
    } catch (error) {
      console.error('Error loading transactions:', error);
      toast.error('Failed to load transactions');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    const statusMap = {
      'Pending': 'pending',
      'Completed': 'completed',
      'Failed': 'failed',
      'Cancelled': 'cancelled',
      'Refunded': 'refunded'
    };
    
    return (
      <span className={`status-badge ${statusMap[status] || 'unknown'}`}>
        {status}
      </span>
    );
  };

  const formatAmount = (amount, currency) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency || 'USD'
    }).format(amount);
  };

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleString();
  };

  const filteredTransactions = transactions.filter(transaction => {
    if (filters.status && transaction.status !== filters.status) return false;
    if (filters.merchant && !transaction.webShopClient?.name?.toLowerCase().includes(filters.merchant.toLowerCase())) return false;
    if (filters.dateFrom && new Date(transaction.createdAt) < new Date(filters.dateFrom)) return false;
    if (filters.dateTo && new Date(transaction.createdAt) > new Date(filters.dateTo)) return false;
    return true;
  });

  if (loading) {
    return (
      <div className="transactions-container">
        <div className="loading">Loading transactions...</div>
      </div>
    );
  }

  return (
    <div className="transactions-container">
      <div className="header">
        <h1>Transactions</h1>
        <button className="btn btn-primary" onClick={loadTransactions}>
          Refresh
        </button>
      </div>

      <div className="filters">
        <div className="filter-group">
          <label>Status:</label>
          <select 
            value={filters.status} 
            onChange={(e) => setFilters({ ...filters, status: e.target.value })}
          >
            <option value="">All</option>
            <option value="Pending">Pending</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
            <option value="Cancelled">Cancelled</option>
            <option value="Refunded">Refunded</option>
          </select>
        </div>
        
        <div className="filter-group">
          <label>Merchant:</label>
          <input
            type="text"
            placeholder="Search merchant..."
            value={filters.merchant}
            onChange={(e) => setFilters({ ...filters, merchant: e.target.value })}
          />
        </div>
        
        <div className="filter-group">
          <label>From:</label>
          <input
            type="date"
            value={filters.dateFrom}
            onChange={(e) => setFilters({ ...filters, dateFrom: e.target.value })}
          />
        </div>
        
        <div className="filter-group">
          <label>To:</label>
          <input
            type="date"
            value={filters.dateTo}
            onChange={(e) => setFilters({ ...filters, dateTo: e.target.value })}
          />
        </div>
      </div>

      <div className="transactions-table-container">
        <table className="transactions-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Merchant</th>
              <th>Amount</th>
              <th>Payment Method</th>
              <th>Status</th>
              <th>Created</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredTransactions.map((transaction) => (
              <tr key={transaction.id}>
                <td>
                  <span className="transaction-id">
                    {transaction.id}
                  </span>
                </td>
                <td>
                  <div className="merchant-info">
                    <strong>{transaction.webShopClient?.name || 'Unknown'}</strong>
                    <small>{transaction.webShopClient?.merchantId}</small>
                  </div>
                </td>
                <td>
                  <span className="amount">
                    {formatAmount(transaction.amount, transaction.currency)}
                  </span>
                </td>
                <td>
                  <span className="payment-method">
                    {transaction.paymentType || 'N/A'}
                  </span>
                </td>
                <td>
                  {getStatusBadge(transaction.status)}
                </td>
                <td>
                  <span className="date">
                    {formatDate(transaction.createdAt)}
                  </span>
                </td>
                <td>
                  <div className="actions">
                    <button 
                      className="btn btn-sm btn-secondary"
                      onClick={() => {
                        // View transaction details
                        console.log('View transaction:', transaction);
                      }}
                    >
                      View
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        
        {filteredTransactions.length === 0 && (
          <div className="no-data">
            No transactions found matching your filters.
          </div>
        )}
      </div>
    </div>
  );
};

export default Transactions;
