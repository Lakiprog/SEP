import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { customerPaymentAPI } from '../services/api';
import { toast } from 'react-toastify';
import CustomerLayout from './CustomerLayout';
import './CustomerPaymentSelection.css';

const CustomerPaymentSelection = () => {
  console.log('🎉 CustomerPaymentSelection component loaded!');
  const { transactionId } = useParams();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [paymentData, setPaymentData] = useState(null);
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState('');
  const [processing, setProcessing] = useState(false);
  const [qrCode, setQrCode] = useState('');
  const [showQRInput, setShowQRInput] = useState(false);

  const loadPaymentSelection = useCallback(async () => {
    try {
      setLoading(true);
      const response = await customerPaymentAPI.getPaymentSelectionPage(transactionId);
      console.log('🔍 Payment selection response:', response.data);
      console.log('🔍 Available payment methods:', response.data.AvailablePaymentMethods || response.data.availablePaymentMethods);
      
      // Handle different case styles from backend
      const paymentData = response.data;
      if (paymentData.AvailablePaymentMethods && !paymentData.availablePaymentMethods) {
        paymentData.availablePaymentMethods = paymentData.AvailablePaymentMethods;
      }
      
      setPaymentData(paymentData);
    } catch (error) {
      console.error('Error loading payment selection:', error);
      toast.error('Failed to load payment options');
    } finally {
      setLoading(false);
    }
  }, [transactionId]);

  useEffect(() => {
    if (transactionId) {
      loadPaymentSelection();
    }
  }, [transactionId, loadPaymentSelection]);

  const handlePaymentMethodSelect = async () => {
    if (!selectedPaymentMethod) {
      toast.error('Please select a payment method');
      return;
    }

    try {
      setProcessing(true);
      const response = await customerPaymentAPI.selectPaymentMethod(transactionId, {
        paymentType: selectedPaymentMethod
      });
      
      console.log('🔍 Payment method selection response:', response.data);
      
      // Redirect to the payment service (including QR payment)
      if (response.data.paymentUrl && response.data.paymentUrl.trim() !== '') {
        console.log('🔄 Redirecting to:', response.data.paymentUrl);
        window.location.href = response.data.paymentUrl;
      } else if (response.data.redirectUrl && response.data.redirectUrl.trim() !== '') {
        console.log('🔄 Redirecting to:', response.data.redirectUrl);
        window.location.href = response.data.redirectUrl;
      } else {
        console.log('❌ No payment URL provided:', response.data);
        toast.error('Payment URL not available. Please try again or contact support.');
      }
    } catch (error) {
      console.error('Error selecting payment method:', error);
      
      if (error.response?.status === 400) {
        const errorMessage = error.response.data?.message || 'Transaction is no longer available for payment';
        toast.error(`Payment failed: ${errorMessage}`);
      } else if (error.response?.status === 404) {
        toast.error('Transaction not found. Please refresh the page and try again.');
      } else {
        toast.error('Failed to select payment method. Please try again.');
      }
    } finally {
      setProcessing(false);
    }
  };

  const handleQRPayment = async () => {
    if (!qrCode.trim()) {
      toast.error('Please enter QR code');
      return;
    }

    try {
      setProcessing(true);
      const response = await customerPaymentAPI.processQRPayment(transactionId, {
        qrCode: qrCode.trim()
      });
      
      if (response.data.success) {
        toast.success('QR payment processed successfully!');
        
        // Redirect to success page or return URL
        if (response.data.redirectUrl) {
          window.location.href = response.data.redirectUrl;
        } else if (paymentData?.returnUrl) {
          window.location.href = paymentData.returnUrl;
        } else {
          // Show success message
          setTimeout(() => {
            navigate('/');
          }, 2000);
        }
      } else {
        toast.error(response.data.message || 'QR payment failed');
      }
    } catch (error) {
      console.error('Error processing QR payment:', error);
      const errorMessage = error.response?.data?.message || 'Failed to process QR payment';
      toast.error(errorMessage);
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
      case 'qr':
        return '📱';
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
      case 'qr':
        return 'Pay with QR code scan';
      default:
        return 'Secure payment method';
    }
  };


  if (loading) {
    return (
      <div className="customer-payment-container">
        <div className="loading">
          <div className="spinner"></div>
          <p>Loading payment options...</p>
        </div>
      </div>
    );
  }

  if (!paymentData) {
    return (
      <div className="customer-payment-container">
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
    <CustomerLayout>
      <div className="customer-payment-container">
      <div className="payment-header">
        <div className="merchant-logo">
          <div className="logo-placeholder">🏪</div>
        </div>
        <div className="merchant-info">
          <h1>{paymentData.merchantName}</h1>
          <p>Complete your secure payment</p>
        </div>
      </div>

      <div className="payment-summary">
        <div className="summary-card">
          <h3>Order Summary</h3>
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
        <h3>Choose Payment Method</h3>
        <div className="methods-grid">
          {(paymentData.availablePaymentMethods || []).length === 0 ? (
            <div className="no-payment-methods">
              <p>No payment methods available for this merchant.</p>
              <p>Please contact the merchant or try again later.</p>
            </div>
          ) : (
            (paymentData.availablePaymentMethods || []).map((method) => (
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
            ))
          )}
        </div>
      </div>

      {showQRInput ? (
        <div className="qr-payment-section">
          <div className="qr-input-card">
            <h3>📱 QR Code Payment</h3>
            <p>Enter the QR code from your banking app:</p>
            <div className="qr-input-group">
              <textarea
                value={qrCode}
                onChange={(e) => setQrCode(e.target.value)}
                placeholder="Paste QR code here (e.g., K:PR|V:01|C:1|R:105000000000099939|N:Telekom Srbija|I:RSD49,99|SF:221|RO:0069007399344596557495215)"
                rows="3"
                className="qr-textarea"
              />
            </div>
            <div className="qr-actions">
              <button
                className="btn btn-primary btn-large"
                onClick={handleQRPayment}
                disabled={!qrCode.trim() || processing}
              >
                {processing ? 'Processing QR Payment...' : 'Pay with QR Code'}
              </button>
              <button
                className="btn btn-secondary"
                onClick={() => {
                  setShowQRInput(false);
                  setQrCode('');
                }}
              >
                Back to Payment Methods
              </button>
            </div>
          </div>
        </div>
      ) : (
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
            onClick={() => window.history.back()}
          >
            Cancel
          </button>
        </div>
      )}

      <div className="security-info">
        <div className="security-badge">
          <span className="lock-icon">🔒</span>
          <span>Secure Payment</span>
        </div>
        <p>Your payment information is encrypted and secure.</p>
        </div>
      </div>
    </CustomerLayout>
  );
};

export default CustomerPaymentSelection;
