import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { pspAPI } from '../services/api';
import { toast } from 'react-toastify';
import { 
  FaCreditCard, 
  FaPaypal, 
  FaBitcoin, 
  FaSpinner,
  FaArrowLeft,
  FaCheckCircle,
  FaExclamationTriangle
} from 'react-icons/fa';
import './PSPPaymentSelection.css';

const PSPPaymentSelection = () => {
  const { transactionId } = useParams();
  const [loading, setLoading] = useState(true);
  const [transaction, setTransaction] = useState(null);
  const [paymentMethods, setPaymentMethods] = useState([]);
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (transactionId) {
      loadTransactionData();
    }
  }, [transactionId]);

  const loadTransactionData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      // Get transaction details
      const transactionResponse = await pspAPI.getPaymentStatus(transactionId);
      setTransaction(transactionResponse.data);
      
      // Get available payment methods for this merchant
      const merchantId = transactionResponse.data.merchantId || 'TELECOM_001'; // Default for demo
      const methodsResponse = await pspAPI.getAvailablePaymentMethods(merchantId);
      setPaymentMethods(methodsResponse.data);
      
    } catch (err) {
      setError('Failed to load payment information');
      console.error('Error loading transaction data:', err);
    } finally {
      setLoading(false);
    }
  };

  const getPaymentMethodIcon = (type) => {
    switch (type.toLowerCase()) {
      case 'card':
        return <FaCreditCard className="payment-icon card" />;
      case 'paypal':
        return <FaPaypal className="payment-icon paypal" />;
      case 'bitcoin':
        return <FaBitcoin className="payment-icon bitcoin" />;
      default:
        return <FaCreditCard className="payment-icon default" />;
    }
  };

  const handlePaymentMethodSelect = (method) => {
    setSelectedPaymentMethod(method);
  };

  const handleContinue = () => {
    if (!selectedPaymentMethod) {
      toast.error('Please select a payment method');
      return;
    }

    // For now, just show a success message
    // Later this will redirect to the actual payment processor
    toast.success(`Selected ${selectedPaymentMethod.name}. Payment processing will be implemented next.`);
    
    // Simulate redirect back to merchant
    setTimeout(() => {
      if (transaction?.returnUrl) {
        window.location.href = transaction.returnUrl;
      } else {
        toast.info('Payment method selected. Returning to merchant...');
      }
    }, 2000);
  };

  const handleCancel = () => {
    if (transaction?.cancelUrl) {
      window.location.href = transaction.cancelUrl;
    } else {
      toast.info('Payment cancelled');
    }
  };

  if (loading) {
    return (
      <div className="psp-payment-loading">
        <FaSpinner className="spinner" />
        <h2>Loading Payment Options...</h2>
        <p>Please wait while we prepare your payment methods</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="psp-payment-error">
        <FaExclamationTriangle className="error-icon" />
        <h2>Payment Error</h2>
        <p>{error}</p>
        <button className="retry-btn" onClick={loadTransactionData}>
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div className="psp-payment-selection">
      <div className="payment-container">
        {/* Header */}
        <div className="payment-header">
          <div className="merchant-info">
            <h1>Complete Your Payment</h1>
            <p>Select your preferred payment method</p>
          </div>
          <div className="transaction-info">
            <div className="amount">
              <span className="currency">{transaction?.currency || 'USD'}</span>
              <span className="value">${transaction?.amount?.toFixed(2) || '0.00'}</span>
            </div>
            {transaction?.description && (
              <p className="description">{transaction.description}</p>
            )}
          </div>
        </div>

        {/* Payment Methods */}
        <div className="payment-methods-section">
          <h2>Choose Payment Method</h2>
          <div className="payment-methods-grid">
            {paymentMethods.map((method) => (
              <div
                key={method.id}
                className={`payment-method-card ${selectedPaymentMethod?.id === method.id ? 'selected' : ''}`}
                onClick={() => handlePaymentMethodSelect(method)}
              >
                <div className="method-icon">
                  {getPaymentMethodIcon(method.type)}
                </div>
                <div className="method-info">
                  <h3>{method.name}</h3>
                  <p>{method.description}</p>
                </div>
                <div className="method-status">
                  {selectedPaymentMethod?.id === method.id && (
                    <FaCheckCircle className="selected-icon" />
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Action Buttons */}
        <div className="payment-actions">
          <button 
            className="cancel-btn"
            onClick={handleCancel}
          >
            <FaArrowLeft />
            Cancel Payment
          </button>
          <button 
            className="continue-btn"
            onClick={handleContinue}
            disabled={!selectedPaymentMethod}
          >
            Continue with {selectedPaymentMethod?.name || 'Payment Method'}
          </button>
        </div>

        {/* Security Notice */}
        <div className="security-notice">
          <FaCheckCircle className="security-icon" />
          <p>Your payment is secured with industry-standard encryption</p>
        </div>
      </div>
    </div>
  );
};

export default PSPPaymentSelection;
