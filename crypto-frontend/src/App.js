import React, { useState, useEffect } from 'react';
import './App.css';
import CryptoPayment from './components/CryptoPayment';
import axios from 'axios';

function App() {
  const [isFromPSP, setIsFromPSP] = useState(false);
  const [pspParams, setPspParams] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if coming from PSP (look for URL parameters)
    const urlParams = new URLSearchParams(window.location.search);
    const amount = urlParams.get('amount');
    const orderId = urlParams.get('orderId');
    const pspTransactionId = urlParams.get('pspTransactionId');
    const merchantName = urlParams.get('merchantName');
    const callbackUrl = urlParams.get('callbackUrl');

    if (amount && orderId && pspTransactionId) {
      // Coming from PSP
      setIsFromPSP(true);
      setPspParams({
        amount: parseFloat(amount),
        orderId: orderId,
        pspTransactionId: pspTransactionId,
        merchantName: merchantName || 'Merchant',
        callbackUrl: callbackUrl
      });
    }
    setLoading(false);
  }, []);

  const handlePaymentExpired = async (paymentResult) => {
    if (isFromPSP && pspParams?.callbackUrl) {
      try {
        // Notify PSP that payment expired
        await axios.post(pspParams.callbackUrl, {
          PSPTransactionId: pspParams.pspTransactionId,
          PaymentId: paymentResult.payment_id || '',
          TransactionId: paymentResult.transaction_id || '',
          Status: 'expired',
          Message: 'Payment expired after 30 minutes',
          Amount: pspParams.amount,
          Currency: 'USD',
          CryptoCurrency: 'LTCT'
        });

        console.log('Notified PSP about expired payment');
      } catch (error) {
        console.error('Failed to notify PSP about expired payment:', error);
      }
    }
  };

  const handlePaymentComplete = async (paymentResult) => {
    if (isFromPSP && pspParams?.callbackUrl) {
      try {
        // Notify PSP that payment completed
        await axios.post(pspParams.callbackUrl, {
          PSPTransactionId: pspParams.pspTransactionId,
          PaymentId: paymentResult.payment_id || '',
          TransactionId: paymentResult.transaction_id || '',
          Status: 'completed',
          Message: 'LTCT payment completed successfully',
          Amount: pspParams.amount,
          Currency: 'USD',
          CryptoCurrency: 'LTCT'
        });

        console.log('Notified PSP about completed payment');
      } catch (error) {
        console.error('Failed to notify PSP about completed payment:', error);
      }
    }
  };

  const handlePaymentFailed = async (paymentResult) => {
    if (isFromPSP && pspParams?.callbackUrl) {
      try {
        // Notify PSP that payment failed
        await axios.post(pspParams.callbackUrl, {
          PSPTransactionId: pspParams.pspTransactionId,
          PaymentId: paymentResult.payment_id || '',
          TransactionId: paymentResult.transaction_id || '',
          Status: 'failed',
          Message: 'LTCT payment failed',
          Amount: pspParams.amount,
          Currency: 'USD',
          CryptoCurrency: 'LTCT'
        });

        console.log('Notified PSP about failed payment');
      } catch (error) {
        console.error('Failed to notify PSP about failed payment:', error);
      }
    }
  };

  if (loading) {
    return (
      <div className="loading-screen">
        <div className="spinner"></div>
        <p>Loading payment...</p>
      </div>
    );
  }

  if (isFromPSP) {
    // PSP payment mode - show only QR code, timer, and amount
    return (
      <div className="App psp-mode">
        <div className="psp-header">
          <h1>Litecoin Payment</h1>
          <div className="payment-info">
            <div className="amount-display">
              <span className="amount">${pspParams.amount.toFixed(2)}</span>
              <span className="currency">USD → LTCT</span>
            </div>
            <div className="merchant-info">
              <span>Payment to: {pspParams.merchantName}</span>
            </div>
          </div>
        </div>

        <CryptoPayment
          amount={pspParams.amount}
          currency="LTCT"
          orderId={pspParams.orderId}
          onPaymentComplete={handlePaymentComplete}
          onPaymentExpired={handlePaymentExpired}
          onPaymentFailed={handlePaymentFailed}
          showMinimalUI={true}
        />

        <div className="payment-footer">
          <p>🔒 Secure payment • ⏰ Expires in 30 minutes</p>
        </div>
      </div>
    );
  }

  // Demo mode for testing
  return (
    <div className="App demo-mode">
      <div className="demo-container">
        <h1>🪙 Crypto Payment Service</h1>
        <div className="demo-info">
          <p>This service is for PSP integration.</p>
          <p>Users will be redirected here from merchant checkout pages.</p>

          <div className="demo-example">
            <h3>Example PSP URL:</h3>
            <code>
              {window.location.origin}?amount=50.00&orderId=PSP_123&pspTransactionId=PSP_123&merchantName=Test%20Store&callbackUrl=https://psp-callback-url
            </code>
          </div>
        </div>
      </div>
    </div>
  );
}

export default App;
