import React, { useState, useEffect } from 'react';
import { adminAPI } from '../services/api';
import { toast } from 'react-toastify';
import { 
  FaPlus, 
  FaEdit, 
  FaTrash, 
  FaSpinner,
  FaStore,
  FaCheckCircle,
  FaTimesCircle,
  FaExclamationTriangle,
  FaCreditCard,
  FaEye,
  FaEyeSlash
} from 'react-icons/fa';
import './Merchants.css';

const Merchants = () => {
  const [merchants, setMerchants] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingMerchant, setEditingMerchant] = useState(null);
  const [expandedMerchants, setExpandedMerchants] = useState(new Set());
  const [merchantPaymentMethods, setMerchantPaymentMethods] = useState({});
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    accountNumber: '',
    merchantId: '',
    merchantPassword: '',
    baseUrl: ''
  });

  useEffect(() => {
    loadMerchants();
  }, []);

  const loadMerchants = async () => {
    try {
      setLoading(true);
      const response = await adminAPI.getMerchants();
      setMerchants(response.data);
      
      // Load payment methods for each merchant
      await loadMerchantPaymentMethods(response.data);
    } catch (error) {
      toast.error('Failed to load merchants');
      console.error('Error loading merchants:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadMerchantPaymentMethods = async (merchantsList) => {
    const paymentMethodsData = {};
    
    for (const merchant of merchantsList) {
      try {
        const response = await adminAPI.getMerchantPaymentMethods(merchant.id);
        paymentMethodsData[merchant.id] = response.data;
      } catch (error) {
        console.error(`Failed to load payment methods for merchant ${merchant.id}:`, error);
        paymentMethodsData[merchant.id] = [];
      }
    }
    
    setMerchantPaymentMethods(paymentMethodsData);
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    try {
      if (editingMerchant) {
        await adminAPI.updateMerchant(editingMerchant.id, formData);
        toast.success('Merchant updated successfully');
      } else {
        await adminAPI.createMerchant(formData);
        toast.success('Merchant created successfully');
      }
      
      setShowForm(false);
      setEditingMerchant(null);
      setFormData({
        name: '',
        description: '',
        accountNumber: '',
        merchantId: '',
        merchantPassword: '',
        baseUrl: ''
      });
      loadMerchants();
    } catch (error) {
      toast.error(error.response?.data?.message || 'Failed to save merchant');
    }
  };

  const handleEdit = (merchant) => {
    setEditingMerchant(merchant);
    setFormData({
      name: merchant.name,
      description: merchant.description || '',
      accountNumber: merchant.accountNumber || '',
      merchantId: merchant.merchantId,
      merchantPassword: '', // Don't pre-fill password
      baseUrl: merchant.baseUrl || ''
    });
    setShowForm(true);
  };

  const handleDelete = async (merchant) => {
    if (window.confirm(`Are you sure you want to delete ${merchant.name}?`)) {
      try {
        await adminAPI.deleteMerchant(merchant.id);
        toast.success('Merchant deleted successfully');
        loadMerchants();
      } catch (error) {
        toast.error('Failed to delete merchant');
      }
    }
  };

  const handleCancel = () => {
    setShowForm(false);
    setEditingMerchant(null);
    setFormData({
      name: '',
      description: '',
      accountNumber: '',
      merchantId: '',
      merchantPassword: '',
      baseUrl: ''
    });
  };

  const toggleMerchantExpansion = (merchantId) => {
    const newExpanded = new Set(expandedMerchants);
    if (newExpanded.has(merchantId)) {
      newExpanded.delete(merchantId);
    } else {
      newExpanded.add(merchantId);
    }
    setExpandedMerchants(newExpanded);
  };

  const getStatusIcon = (status) => {
    switch (status) {
      case 0: return <FaCheckCircle className="status-icon active" />;
      case 1: return <FaTimesCircle className="status-icon inactive" />;
      case 2: return <FaExclamationTriangle className="status-icon suspended" />;
      default: return <FaTimesCircle className="status-icon inactive" />;
    }
  };

  const getStatusText = (status) => {
    switch (status) {
      case 0: return 'Active';
      case 1: return 'Inactive';
      case 2: return 'Suspended';
      default: return 'Unknown';
    }
  };

  if (loading) {
    return (
      <div className="merchants-loading">
        <FaSpinner className="spinner" />
        <p>Loading merchants...</p>
      </div>
    );
  }

  return (
    <div className="merchants">
      <div className="merchants-header">
        <h1>Merchants</h1>
        <button 
          className="btn btn-primary"
          onClick={() => setShowForm(true)}
        >
          <FaPlus />
          Add Merchant
        </button>
      </div>

      {showForm && (
        <div className="merchant-form-overlay">
          <div className="merchant-form">
            <h2>{editingMerchant ? 'Edit Merchant' : 'Add New Merchant'}</h2>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label htmlFor="name">Merchant Name *</label>
                <input
                  type="text"
                  id="name"
                  name="name"
                  value={formData.name}
                  onChange={handleInputChange}
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="description">Description</label>
                <textarea
                  id="description"
                  name="description"
                  value={formData.description}
                  onChange={handleInputChange}
                  rows="3"
                />
              </div>

              <div className="form-group">
                <label htmlFor="accountNumber">Account Number</label>
                <input
                  type="text"
                  id="accountNumber"
                  name="accountNumber"
                  value={formData.accountNumber}
                  onChange={handleInputChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="merchantId">Merchant ID *</label>
                <input
                  type="text"
                  id="merchantId"
                  name="merchantId"
                  value={formData.merchantId}
                  onChange={handleInputChange}
                  required
                />
              </div>

              <div className="form-group">
                <label htmlFor="merchantPassword">Merchant Password *</label>
                <input
                  type="password"
                  id="merchantPassword"
                  name="merchantPassword"
                  value={formData.merchantPassword}
                  onChange={handleInputChange}
                  required={!editingMerchant}
                />
                {editingMerchant && (
                  <small>Leave blank to keep current password</small>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="baseUrl">Base URL</label>
                <input
                  type="url"
                  id="baseUrl"
                  name="baseUrl"
                  value={formData.baseUrl}
                  onChange={handleInputChange}
                  placeholder="https://example.com"
                />
              </div>

              <div className="form-actions">
                <button type="submit" className="btn btn-primary">
                  {editingMerchant ? 'Update' : 'Create'}
                </button>
                <button type="button" className="btn btn-secondary" onClick={handleCancel}>
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="merchants-list">
        {merchants.length === 0 ? (
          <div className="empty-state">
            <FaStore className="empty-icon" />
            <h3>No merchants found</h3>
            <p>Get started by adding your first merchant</p>
            <button 
              className="btn btn-primary"
              onClick={() => setShowForm(true)}
            >
              <FaPlus />
              Add Merchant
            </button>
          </div>
        ) : (
          <div className="merchants-grid">
            {merchants.map((merchant) => (
              <div key={merchant.id} className="merchant-card">
                <div className="merchant-header">
                  <div className="merchant-info">
                    <h3>{merchant.name}</h3>
                    <p className="merchant-id">ID: {merchant.merchantId}</p>
                  </div>
                  <div className="merchant-status">
                    {getStatusIcon(merchant.status)}
                    <span className="status-text">{getStatusText(merchant.status)}</span>
                  </div>
                </div>

                {merchant.description && (
                  <p className="merchant-description">{merchant.description}</p>
                )}

                <div className="merchant-details">
                  {merchant.accountNumber && (
                    <div className="detail-item">
                      <strong>Account:</strong> {merchant.accountNumber}
                    </div>
                  )}
                  {merchant.baseUrl && (
                    <div className="detail-item">
                      <strong>URL:</strong> 
                      <a href={merchant.baseUrl} target="_blank" rel="noopener noreferrer">
                        {merchant.baseUrl}
                      </a>
                    </div>
                  )}
                  <div className="detail-item">
                    <strong>Created:</strong> {new Date(merchant.createdAt).toLocaleDateString()}
                  </div>
                  
                  {/* Payment Methods Section */}
                  <div className="payment-methods-section">
                    <button 
                      className="payment-methods-toggle"
                      onClick={() => toggleMerchantExpansion(merchant.id)}
                    >
                      <FaCreditCard />
                      <span>Payment Methods ({merchantPaymentMethods[merchant.id]?.length || 0})</span>
                      {expandedMerchants.has(merchant.id) ? <FaEyeSlash /> : <FaEye />}
                    </button>
                    
                    {expandedMerchants.has(merchant.id) && (
                      <div className="payment-methods-list">
                        {merchantPaymentMethods[merchant.id]?.length > 0 ? (
                          merchantPaymentMethods[merchant.id].map((paymentMethod) => (
                            <div key={paymentMethod.paymentTypeId} className="payment-method-item">
                              <div className="payment-method-info">
                                <FaCreditCard className="payment-method-icon" />
                                <div>
                                  <div className="payment-method-name">{paymentMethod.name}</div>
                                  <div className="payment-method-type">{paymentMethod.type}</div>
                                </div>
                              </div>
                              <div className={`payment-method-status ${paymentMethod.isEnabled ? 'enabled' : 'disabled'}`}>
                                {paymentMethod.isEnabled ? 'Enabled' : 'Disabled'}
                              </div>
                            </div>
                          ))
                        ) : (
                          <div className="no-payment-methods">
                            <p>No payment methods configured</p>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>

                <div className="merchant-actions">
                  <button 
                    className="btn btn-sm btn-secondary"
                    onClick={() => handleEdit(merchant)}
                  >
                    <FaEdit />
                    Edit
                  </button>
                  <button 
                    className="btn btn-sm btn-danger"
                    onClick={() => handleDelete(merchant)}
                  >
                    <FaTrash />
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default Merchants;
