import React, { useState, useEffect } from 'react';
import { adminAPI } from '../services/api';
import { toast } from 'react-toastify';
import './PaymentMethods.css';

const PaymentMethods = () => {
  const [paymentMethods, setPaymentMethods] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingMethod, setEditingMethod] = useState(null);
  const [formData, setFormData] = useState({
    name: '',
    type: '',
    description: '',
    isEnabled: true
  });

  useEffect(() => {
    loadPaymentMethods();
  }, []);

  const loadPaymentMethods = async () => {
    try {
      setLoading(true);
      const response = await adminAPI.getPaymentMethods();
      setPaymentMethods(response.data);
    } catch (error) {
      console.error('Error loading payment methods:', error);
      toast.error('Failed to load payment methods');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingMethod) {
        await adminAPI.updatePaymentMethod(editingMethod.id, formData);
        toast.success('Payment method updated successfully');
      } else {
        await adminAPI.createPaymentMethod(formData);
        toast.success('Payment method created successfully');
      }
      setShowModal(false);
      setEditingMethod(null);
      setFormData({ name: '', type: '', description: '', isEnabled: true });
      loadPaymentMethods();
    } catch (error) {
      console.error('Error saving payment method:', error);
      toast.error('Failed to save payment method');
    }
  };

  const handleEdit = (method) => {
    setEditingMethod(method);
    setFormData({
      name: method.name,
      type: method.type,
      description: method.description,
      isEnabled: method.isEnabled
    });
    setShowModal(true);
  };

  const handleDelete = async (id) => {
    if (window.confirm('Are you sure you want to delete this payment method?')) {
      try {
        await adminAPI.deletePaymentMethod(id);
        toast.success('Payment method deleted successfully');
        loadPaymentMethods();
      } catch (error) {
        console.error('Error deleting payment method:', error);
        toast.error('Failed to delete payment method');
      }
    }
  };

  const openModal = () => {
    setEditingMethod(null);
    setFormData({ name: '', type: '', description: '', isEnabled: true });
    setShowModal(true);
  };

  if (loading) {
    return (
      <div className="payment-methods-container">
        <div className="loading">Loading payment methods...</div>
      </div>
    );
  }

  return (
    <div className="payment-methods-container">
      <div className="header">
        <h1>Payment Methods</h1>
        <button className="btn btn-primary" onClick={openModal}>
          Add Payment Method
        </button>
      </div>

      <div className="payment-methods-grid">
        {paymentMethods.map((method) => (
          <div key={method.id} className="payment-method-card">
            <div className="card-header">
              <h3>{method.name}</h3>
              <span className={`status ${method.isEnabled ? 'enabled' : 'disabled'}`}>
                {method.isEnabled ? 'Enabled' : 'Disabled'}
              </span>
            </div>
            <div className="card-body">
              <p><strong>Type:</strong> {method.type}</p>
              <p><strong>Description:</strong> {method.description}</p>
              <p><strong>Created:</strong> {new Date(method.createdAt).toLocaleDateString()}</p>
            </div>
            <div className="card-actions">
              <button 
                className="btn btn-secondary" 
                onClick={() => handleEdit(method)}
              >
                Edit
              </button>
              <button 
                className="btn btn-danger" 
                onClick={() => handleDelete(method.id)}
              >
                Delete
              </button>
            </div>
          </div>
        ))}
      </div>

      {showModal && (
        <div className="modal-overlay">
          <div className="modal">
            <div className="modal-header">
              <h2>{editingMethod ? 'Edit Payment Method' : 'Add Payment Method'}</h2>
              <button className="close-btn" onClick={() => setShowModal(false)}>×</button>
            </div>
            <form onSubmit={handleSubmit} className="modal-body">
              <div className="form-group">
                <label>Name</label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label>Type</label>
                <input
                  type="text"
                  value={formData.type}
                  onChange={(e) => setFormData({ ...formData, type: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label>Description</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  rows="3"
                />
              </div>
              <div className="form-group">
                <label>
                  <input
                    type="checkbox"
                    checked={formData.isEnabled}
                    onChange={(e) => setFormData({ ...formData, isEnabled: e.target.checked })}
                  />
                  Enabled
                </label>
              </div>
              <div className="modal-actions">
                <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary">
                  {editingMethod ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default PaymentMethods;
