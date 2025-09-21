import React, { useState, useEffect, useCallback, useRef } from 'react';
import QRCodeDisplay from './QRCodeDisplay';
import CountdownTimer from './CountdownTimer';
import { bitcoinPaymentService } from '../services/bitcoinPaymentService';

const CryptoPayment = ({
  amount = 50,
  currency = 'BTC',
  orderId,
  onPaymentComplete,
  onPaymentExpired,
  onPaymentFailed,
  className = ''
}) => {
  const [paymentData, setPaymentData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [paymentStatus, setPaymentStatus] = useState('pending');
  const [serviceAvailable, setServiceAvailable] = useState(null);
  const [checkingStatus, setCheckingStatus] = useState(false);
  const initializedRef = useRef(false);

  // Check if Bitcoin payment service is available
  const checkServiceHealth = useCallback(async () => {
    try {
      const health = await bitcoinPaymentService.checkHealth();
      setServiceAvailable(!!health);
      return !!health;
    } catch (error) {
      setServiceAvailable(false);
      return false;
    }
  }, []);

  // Create payment and QR code
  const createPayment = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const isServiceAvailable = await checkServiceHealth();
      if (!isServiceAvailable) {
        throw new Error('Bitcoin payment service is currently unavailable');
      }

      const paymentRequest = {
        amount: amount,
        orderId: orderId || `ORDER_${Date.now()}`,
        returnUrl: window.location.href,
        currency: currency,
        buyerEmail: 'user@example.com',
        itemName: `Payment for ${currency.toUpperCase()}`
      };

      const response = await bitcoinPaymentService.createQRPayment(paymentRequest);
      setPaymentData(response);
      setPaymentStatus('pending');
    } catch (error) {
      setError(error.message);
      console.error('Error creating payment:', error);
    } finally {
      setLoading(false);
    }
  }, [amount, currency, orderId, checkServiceHealth]);

  // Poll payment status
  const checkPaymentStatus = useCallback(async () => {
    if (!paymentData?.paymentId) return;

    try {
      const status = await bitcoinPaymentService.getPaymentStatus(paymentData.paymentId);
      setPaymentStatus(status.status.toLowerCase());

      switch (status.status.toLowerCase()) {
        case 'completed':
          if (onPaymentComplete) {
            onPaymentComplete(status);
          }
          break;
        case 'expired':
          if (onPaymentExpired) {
            onPaymentExpired(status);
          }
          break;
        case 'failed':
        case 'cancelled':
          if (onPaymentFailed) {
            onPaymentFailed(status);
          }
          break;
        default:
          // Continue polling for pending/confirmed status
          break;
      }
    } catch (error) {
      console.error('Error checking payment status:', error);
    }
  }, [paymentData?.paymentId, onPaymentComplete, onPaymentExpired, onPaymentFailed]);

  // Handle payment expiration from timer
  const handleTimerExpire = useCallback(() => {
    setPaymentStatus('expired');
    if (onPaymentExpired && paymentData) {
      onPaymentExpired({
        ...paymentData,
        status: 'expired',
        is_expired: true
      });
    }
  }, [onPaymentExpired, paymentData]);

  // Manual status check with PSP callback
  const checkStatusManually = useCallback(async () => {
    if (!paymentData?.transactionId || checkingStatus) return;

    setCheckingStatus(true);
    try {
      // Call the new manual check endpoint
      const result = await bitcoinPaymentService.checkTransactionStatusManually(paymentData.transactionId);

      // Get updated status
      const updatedStatus = await bitcoinPaymentService.getPaymentStatus(paymentData.transactionId);
      const newStatus = updatedStatus.status.toLowerCase();

      setPaymentStatus(newStatus);

      // Send PSP callback based on status
      if (newStatus === 'completed') {
        // Successful transaction - send PSP callback
        try {
          await bitcoinPaymentService.sendPSPCallback(updatedStatus, true);
          alert('✅ Plaćanje je uspešno! PSP je obavešten.');

          if (onPaymentComplete) {
            onPaymentComplete(updatedStatus);
          }
        } catch (callbackError) {
          console.error('PSP callback failed:', callbackError);
          alert('✅ Plaćanje je uspešno, ali ima problema sa obaveštavanjem PSP-a.');
        }
      } else if (newStatus === 'failed' || newStatus === 'cancelled' || newStatus === 'expired') {
        // Failed transaction - send PSP callback
        try {
          const message = newStatus === 'expired' ? 'Transakcija je istekla' : 'Transakcija je otkazana ili neuspešna';
          await bitcoinPaymentService.sendPSPCallback(updatedStatus, false, message);
          alert('❌ Plaćanje nije uspešno. PSP je obavešten.');

          if (newStatus === 'expired' && onPaymentExpired) {
            onPaymentExpired(updatedStatus);
          } else if (onPaymentFailed) {
            onPaymentFailed(updatedStatus);
          }
        } catch (callbackError) {
          console.error('PSP callback failed:', callbackError);
          alert('❌ Plaćanje nije uspešno, takođe ima problema sa obaveštavanjem PSP-a.');
        }
      } else {
        // Still pending
        alert('⏳ Transakcija je još uvek u toku. Pokušajte ponovo za nekoliko minuta.');
      }
    } catch (error) {
      console.error('Error checking status manually:', error);
      alert('❌ Greška pri proveri statusa: ' + error.message);
    } finally {
      setCheckingStatus(false);
    }
  }, [paymentData?.transactionId, checkingStatus, onPaymentComplete, onPaymentExpired, onPaymentFailed]);

  // Start payment creation on component mount
  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      createPayment();
    }
  }, [createPayment]);

  // Set up status polling
  useEffect(() => {
    if (!paymentData?.paymentId || paymentStatus === 'completed' || paymentStatus === 'expired' || paymentStatus === 'failed') {
      return;
    }

    const interval = setInterval(checkPaymentStatus, 5000); // Poll every 5 seconds
    return () => clearInterval(interval);
  }, [paymentData?.paymentId, paymentStatus, checkPaymentStatus]);

  // Retry payment creation
  const retryPayment = () => {
    setPaymentData(null);
    setError(null);
    setPaymentStatus('pending');
    initializedRef.current = false; // Reset initialization flag
    createPayment();
  };

  const getStatusDisplay = () => {
    switch (paymentStatus) {
      case 'pending':
        return { text: 'Waiting for payment...', color: '#007bff', icon: '⏳' };
      case 'confirmed':
        return { text: 'Payment confirmed, processing...', color: '#28a745', icon: '✅' };
      case 'completed':
        return { text: 'Payment completed successfully!', color: '#28a745', icon: '🎉' };
      case 'expired':
        return { text: 'Payment expired', color: '#dc3545', icon: '⏰' };
      case 'failed':
        return { text: 'Payment failed', color: '#dc3545', icon: '❌' };
      case 'cancelled':
        return { text: 'Payment cancelled', color: '#6c757d', icon: '🚫' };
      default:
        return { text: 'Unknown status', color: '#6c757d', icon: '❓' };
    }
  };

  const statusDisplay = getStatusDisplay();

  return (
    <div className={`crypto-payment ${className}`}>
      <div className="payment-header">
        <h2>Crypto Payment</h2>
        {serviceAvailable === false && (
          <div className="service-warning">
            ⚠️ Payment service is currently unavailable
          </div>
        )}
      </div>

      {loading && (
        <div className="loading-container">
          <div className="spinner"></div>
          <p>Creating payment...</p>
        </div>
      )}

      {error && (
        <div className="error-container">
          <p>❌ {error}</p>
          <button onClick={retryPayment} className="retry-btn">
            Try Again
          </button>
        </div>
      )}

      {paymentData && (
        <>
          <div className="status-bar">
            <span className="status-icon">{statusDisplay.icon}</span>
            <span className="status-text" style={{ color: statusDisplay.color }}>
              {statusDisplay.text}
            </span>
          </div>

          {(paymentStatus === 'pending' || paymentStatus === 'confirmed') && (
            <>
              <CountdownTimer
                expiresAt={paymentData.expiresAt}
                onExpire={handleTimerExpire}
              />

              <QRCodeDisplay
                qrCodeImage={paymentData.qrCodeImage}
                qrCodeData={paymentData.qrCodeData}
                address={paymentData.address}
                amount={paymentData.amount}
                currency={paymentData.currency}
              />

              <div className="check-status-container">
                <button
                  onClick={checkStatusManually}
                  disabled={checkingStatus}
                  className="check-status-btn"
                >
                  {checkingStatus ? (
                    <>
                      <div className="mini-spinner"></div>
                      Proverava se...
                    </>
                  ) : (
                    '🔍 Proveri status'
                  )}
                </button>
                <p className="check-status-help">
                  Kliknite ovde da proverite da li je plaćanje uspešno završeno
                </p>
              </div>
            </>
          )}

          {paymentStatus === 'expired' && (
            <div className="expired-container">
              <h3>Payment Expired</h3>
              <p>This payment session has expired. Please create a new payment.</p>
              <button onClick={retryPayment} className="retry-btn">
                Create New Payment
              </button>
            </div>
          )}

          {paymentStatus === 'completed' && (
            <div className="success-container">
              <h3>🎉 Payment Successful!</h3>
              <p>Your {currency.toUpperCase()} payment has been completed successfully.</p>
              <div className="payment-summary">
                <p><strong>Amount:</strong> {paymentData.amount} {paymentData.currency}</p>
                <p><strong>Transaction ID:</strong> {paymentData.transactionId}</p>
                <p><strong>Order ID:</strong> {paymentData.paymentId}</p>
              </div>
            </div>
          )}

          <div className="payment-info">
            <div className="info-row">
              <span>Payment ID:</span>
              <span className="mono">{paymentData.paymentId}</span>
            </div>
            {paymentData.transactionId && (
              <div className="info-row">
                <span>Transaction ID:</span>
                <span className="mono">{paymentData.transactionId}</span>
              </div>
            )}
          </div>
        </>
      )}

      <style jsx>{`
        .crypto-payment {
          max-width: 500px;
          margin: 20px auto;
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        }

        .payment-header h2 {
          text-align: center;
          color: #2c3e50;
          margin-bottom: 10px;
        }

        .service-warning {
          background: #fff3cd;
          color: #856404;
          padding: 8px 12px;
          border-radius: 6px;
          text-align: center;
          font-size: 14px;
          margin-bottom: 15px;
          border: 1px solid #ffeaa7;
        }

        .loading-container {
          text-align: center;
          padding: 40px;
          background: #f8f9fa;
          border-radius: 8px;
        }

        .spinner {
          border: 3px solid #f3f3f3;
          border-top: 3px solid #007bff;
          border-radius: 50%;
          width: 30px;
          height: 30px;
          animation: spin 1s linear infinite;
          margin: 0 auto 15px;
        }

        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }

        .error-container {
          background: #f8d7da;
          color: #721c24;
          padding: 15px;
          border-radius: 8px;
          text-align: center;
          margin-bottom: 15px;
          border: 1px solid #f5c6cb;
        }

        .status-bar {
          background: #e9ecef;
          padding: 12px;
          border-radius: 8px;
          text-align: center;
          margin-bottom: 15px;
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 8px;
        }

        .status-icon {
          font-size: 18px;
        }

        .status-text {
          font-weight: 600;
          font-size: 16px;
        }

        .expired-container, .success-container {
          background: white;
          border-radius: 12px;
          padding: 24px;
          text-align: center;
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
          border: 1px solid #e1e5e9;
          margin: 15px 0;
        }

        .success-container {
          border-color: #28a745;
          background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%);
        }

        .expired-container {
          border-color: #dc3545;
          background: linear-gradient(135deg, #f8d7da 0%, #f5c6cb 100%);
        }

        .payment-summary {
          background: rgba(255, 255, 255, 0.8);
          border-radius: 8px;
          padding: 15px;
          margin-top: 15px;
          text-align: left;
        }

        .payment-summary p {
          margin: 5px 0;
          font-size: 14px;
        }

        .retry-btn {
          background: #007bff;
          color: white;
          border: none;
          padding: 10px 20px;
          border-radius: 6px;
          cursor: pointer;
          font-size: 14px;
          margin-top: 10px;
          transition: background-color 0.2s;
        }

        .retry-btn:hover {
          background: #0056b3;
        }

        .check-status-container {
          text-align: center;
          margin: 20px 0;
          padding: 15px;
          background: #f8f9fa;
          border-radius: 8px;
          border: 1px solid #dee2e6;
        }

        .check-status-btn {
          background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
          color: white;
          border: none;
          padding: 12px 24px;
          border-radius: 8px;
          cursor: pointer;
          font-size: 16px;
          font-weight: 600;
          transition: all 0.3s ease;
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 8px;
          margin: 0 auto 10px;
          min-width: 180px;
          box-shadow: 0 2px 8px rgba(40, 167, 69, 0.3);
        }

        .check-status-btn:hover:not(:disabled) {
          transform: translateY(-2px);
          box-shadow: 0 4px 12px rgba(40, 167, 69, 0.4);
          background: linear-gradient(135deg, #218838 0%, #1e7e34 100%);
        }

        .check-status-btn:disabled {
          opacity: 0.7;
          cursor: not-allowed;
          transform: none;
          box-shadow: 0 2px 8px rgba(40, 167, 69, 0.2);
        }

        .mini-spinner {
          border: 2px solid rgba(255, 255, 255, 0.3);
          border-top: 2px solid white;
          border-radius: 50%;
          width: 16px;
          height: 16px;
          animation: spin 1s linear infinite;
        }

        .check-status-help {
          margin: 0;
          font-size: 14px;
          color: #6c757d;
          font-style: italic;
        }

        .payment-info {
          background: #f8f9fa;
          border-radius: 8px;
          padding: 15px;
          margin-top: 15px;
        }

        .info-row {
          display: flex;
          justify-content: space-between;
          margin-bottom: 8px;
          font-size: 14px;
        }

        .info-row:last-child {
          margin-bottom: 0;
        }

        .mono {
          font-family: 'Courier New', monospace;
          color: #6c757d;
          word-break: break-all;
        }

        @media (max-width: 480px) {
          .crypto-payment {
            margin: 10px;
          }

          .info-row {
            flex-direction: column;
            gap: 4px;
          }
        }
      `}</style>
    </div>
  );
};

export default CryptoPayment;