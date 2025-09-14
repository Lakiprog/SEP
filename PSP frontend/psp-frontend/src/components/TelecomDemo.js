import React, { useState } from 'react';
import { paymentInitiationAPI } from '../services/api';
import { toast } from 'react-toastify';
import { FaSpinner, FaCreditCard, FaShoppingCart } from 'react-icons/fa';
import './TelecomDemo.css';

const TelecomDemo = () => {
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    amount: '49.99',
    description: 'Monthly Telecom Service',
    customerEmail: 'customer@example.com',
    customerName: 'John Doe'
  });

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handlePayment = async (e) => {
    e.preventDefault();
    setLoading(true);

    try {
      const paymentData = {
        merchantId: 'TELECOM_001',
        merchantPassword: 'telecom123',
        amount: parseFloat(formData.amount),
        currency: 'USD',
        merchantOrderId: `TELECOM_${Date.now()}`,
        description: formData.description,
        returnUrl: 'http://localhost:3001/webshop/success',
        cancelUrl: 'http://localhost:3001/webshop/cancel',
        callbackUrl: 'http://localhost:3001/api/payment/callback',
        customerEmail: formData.customerEmail,
        customerName: formData.customerName
      };

      const response = await paymentInitiationAPI.initiatePayment(paymentData);
      
      if (response.data.success) {
        // Redirect to PSP payment selection page
        window.location.href = response.data.paymentSelectionUrl;
      } else {
        toast.error(response.data.message || 'Payment initiation failed');
      }
    } catch (error) {
      console.error('Payment initiation error:', error);
      toast.error(error.response?.data?.message || 'Payment initiation failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="telecom-demo">
      <div className="demo-container">
        <div className="demo-header">
          <div className="merchant-logo">
            <FaShoppingCart />
          </div>
          <h1>Telecom Operator</h1>
          <p>Your trusted telecommunications provider</p>
        </div>

        <div className="demo-content">
          <div className="product-info">
            <h2>Monthly Telecom Service</h2>
            <div className="price">
              <span className="currency">USD</span>
              <span className="amount">${formData.amount}</span>
            </div>
            <p className="description">
              Unlimited calls, texts, and 10GB data plan
            </p>
          </div>

          <form onSubmit={handlePayment} className="payment-form">
            <h3>Customer Information</h3>
            
            <div className="form-group">
              <label htmlFor="customerName">Full Name</label>
              <input
                type="text"
                id="customerName"
                name="customerName"
                value={formData.customerName}
                onChange={handleInputChange}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="customerEmail">Email Address</label>
              <input
                type="email"
                id="customerEmail"
                name="customerEmail"
                value={formData.customerEmail}
                onChange={handleInputChange}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="amount">Amount (USD)</label>
              <input
                type="number"
                id="amount"
                name="amount"
                value={formData.amount}
                onChange={handleInputChange}
                step="0.01"
                min="0.01"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="description">Description</label>
              <input
                type="text"
                id="description"
                name="description"
                value={formData.description}
                onChange={handleInputChange}
                required
              />
            </div>

            <button 
              type="submit" 
              className="pay-btn"
              disabled={loading}
            >
              {loading ? (
                <>
                  <FaSpinner className="spinner" />
                  Processing...
                </>
              ) : (
                <>
                  <FaCreditCard />
                  Pay Now
                </>
              )}
            </button>
          </form>

          <div className="demo-info">
            <h4>Demo Information</h4>
            <p>This is a demo of the Telecom webshop checkout process.</p>
            <p>Clicking "Pay Now" will redirect you to the PSP payment selection page where you can choose your preferred payment method.</p>
            <div className="demo-credentials">
              <strong>Demo Credentials:</strong>
              <br />
              Merchant ID: TELECOM_001
              <br />
              Password: telecom123
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TelecomDemo;
