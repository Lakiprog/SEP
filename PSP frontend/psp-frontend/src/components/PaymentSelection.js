import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { paymentSelectionAPI } from '../services/api';
import { toast } from 'react-toastify';
import './PaymentSelection.css';

const PaymentSelection = () => {
  const { transactionId } = useParams();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [paymentData, setPaymentData] = useState(null);
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState('');
  const [processing, setProcessing] = useState(false);

  useEffect(() => {
    if (transactionId) {
      loadPaymentSelection();
    }
  }, [transactionId]);

  const loadPaymentSelection = async () => {
    try {
      setLoading(true);
      const response = await paymentSelectionAPI.getPaymentSelectionPage(transactionId);
      setPaymentData(response.data);
    } catch (error) {
      console.error('Error loading payment selection:', error);
      toast.error('Failed to load payment options');
    } finally {
      setLoading(false);
    }
  };

  const handlePaymentMethodSelect = async () => {
    if (!selectedPaymentMethod) {
      toast.error('Please select a payment method');
      return;
    }

    try {
      setProcessing(true);
      const response = await paymentSelectionAPI.selectPaymentMethod(transactionId, {
        paymentType: selectedPaymentMethod
      });
      
      // Redirect to the payment service
      if (response.data.redirectUrl) {
        window.location.href = response.data.redirectUrl;
      } else {
        toast.success('Payment method selected successfully');
        // Handle success case
      }
    } catch (error) {
      console.error('Error selecting payment method:', error);
      toast.error('Failed to select payment method');
    } finally {
      setProcessing(false);
    }
  };

  const getPaymentMethodIcon = (type) => {
    switch (type.toLowerCase()) {
      case 'card':
        return '💳';
      case 'paypal':
        return '🅿️';
      case 'bitcoin':
        return '₿';
      default:
        return '💳';
    }
  };

  const getPaymentMethodDescription = (type) => {
    switch (type.toLowerCase()) {
      case 'card':
        return 'Pay with your credit or debit card';
      case 'paypal':
        return 'Pay securely with PayPal';
      case 'bitcoin':
        return 'Pay with Bitcoin cryptocurrency';
      default:
        return 'Secure payment method';
    }
  };

  if (loading) {
    return (
      <div className="payment-selection-container">
        <div className="loading">
          <div className="spinner"></div>
          <p>Loading payment options...</p>
        </div>
      </div>
    );
  }

  if (!paymentData) {
    return (
      <div className="payment-selection-container">
        <div className="error">
          <h2>Payment Not Found</h2>
          <p>The requested payment could not be found.</p>
          <button className="btn btn-primary" onClick={() => navigate('/')}>
            Go Back
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="payment-selection-container">
      <div className="payment-header">
        <h1>Complete Your Payment</h1>
        <div className="merchant-info">
          <h2>{paymentData.merchantName}</h2>
        </div>
      </div>

      <div className="payment-summary">
        <div className="summary-card">
          <h3>Payment Summary</h3>
          <div className="summary-details">
            <div className="summary-row">
              <span>Amount:</span>
              <span className="amount">
                {paymentData.amount} {paymentData.currency}
              </span>
            </div>
            <div className="summary-row">
              <span>Transaction ID:</span>
              <span className="transaction-id">{paymentData.transactionId}</span>
            </div>
            <div className="summary-row">
              <span>Description:</span>
              <span>{paymentData.description || 'Payment for services'}</span>
            </div>
          </div>
        </div>
      </div>

      <div className="payment-methods">
        <h3>Select Payment Method</h3>
        <div className="methods-grid">
          {paymentData.availablePaymentMethods.map((method) => (
            <div
              key={method.type}
              className={`method-card ${selectedPaymentMethod === method.type ? 'selected' : ''}`}
              onClick={() => setSelectedPaymentMethod(method.type)}
            >
              <div className="method-icon">
                {getPaymentMethodIcon(method.type)}
              </div>
              <div className="method-info">
                <h4>{method.name}</h4>
                <p>{getPaymentMethodDescription(method.type)}</p>
              </div>
              <div className="method-radio">
                <input
                  type="radio"
                  name="paymentMethod"
                  value={method.type}
                  checked={selectedPaymentMethod === method.type}
                  onChange={() => setSelectedPaymentMethod(method.type)}
                />
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="payment-actions">
        <button
          className="btn btn-primary btn-large"
          onClick={handlePaymentMethodSelect}
          disabled={!selectedPaymentMethod || processing}
        >
          {processing ? 'Processing...' : 'Continue to Payment'}
        </button>
        <button
          className="btn btn-secondary"
          onClick={() => navigate('/')}
        >
          Cancel
        </button>
      </div>

      <div className="security-info">
        <div className="security-badge">
          <span className="lock-icon">🔒</span>
          <span>Secure Payment</span>
        </div>
        <p>Your payment information is encrypted and secure.</p>
      </div>
    </div>
  );
};

export default PaymentSelection;
