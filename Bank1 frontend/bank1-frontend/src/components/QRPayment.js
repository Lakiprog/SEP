import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { bankAPI } from '../services/api';
import { toast } from 'react-toastify';
import './QRPayment.css';

const QRPayment = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  
  const [loading, setLoading] = useState(true);
  // QR code will be automatically processed, no manual input needed
  const [paymentData, setPaymentData] = useState(null);
  const [qrCodeImage, setQrCodeImage] = useState('');
  const [verifying, setVerifying] = useState(false);
  const [paymentId, setPaymentId] = useState('');
  
  // Ref to prevent duplicate API calls in React StrictMode
  const hasInitialized = useRef(false);

  // Extract parameters from URL
  const amount = searchParams.get('amount');
  const currency = searchParams.get('currency');
  const merchantId = searchParams.get('merchantId');
  const orderId = searchParams.get('orderId');
  const pspTransactionId = searchParams.get('pspTransactionId');
  const returnUrl = searchParams.get('returnUrl');
  const cancelUrl = searchParams.get('cancelUrl');

  useEffect(() => {
    // Prevent duplicate calls in React StrictMode
    if (hasInitialized.current) return;
    hasInitialized.current = true;
    
    if (amount && currency && merchantId) {
      console.log('[QR DEBUG] Initializing QR code generation...');
      generateQRCode();
    } else {
      toast.error('Invalid payment parameters');
      setLoading(false);
    }
  }, [amount, currency, merchantId]);

  const generateQRCode = async () => {
    try {
      setLoading(true);
      
      const qrRequest = {
        amount: parseFloat(amount),
        currency: 'RSD',
        merchantId: merchantId,
        orderId: orderId || '',
        accountNumber: '105000000000099939', // Default Telecom account
        receiverName: 'Telekom Srbija'
      };

      console.log('🔄 Generating QR code with:', qrRequest);

      const response = await bankAPI.generateQRPayment(qrRequest);
      
      console.log('🔍 QR generation response:', response.data);
      
      if (response.data.success) {
        console.log('✅ QR kod generated successfully, setting image:', response.data.qrCode ? 'YES' : 'NO');
        setQrCodeImage(response.data.qrCode);
        setPaymentId(response.data.paymentId);
        setPaymentData({
          amount: response.data.amount,
          currency: response.data.currency,
          accountNumber: response.data.accountNumber,
          receiverName: response.data.receiverName,
          orderId: response.data.orderId
        });
        toast.success('QR kod je uspešno generisan!');
      } else {
        console.log('❌ QR generation failed:', response.data);
        toast.error('Greška prilikom generisanja QR koda');
      }
    } catch (error) {
      console.error('Error generating QR code:', error);
      toast.error('Greška u komunikaciji sa bankom');
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    // Redirect back to PSP payment selection
    if (cancelUrl) {
      window.location.href = cancelUrl;
    } else {
      // Fallback to PSP payment selection page
      window.location.href = `http://localhost:3001/payment-selection/${pspTransactionId}`;
    }
  };

  const handleVerifyAndPay = async () => {
    if (!paymentData || !paymentId) {
      toast.error('Podaci o plaćanju nisu dostupni');
      return;
    }

    try {
      setVerifying(true);
      
      console.log('🔄 Processing automatic payment for:', {
        paymentId,
        amount: paymentData.amount,
        currency: 'RSD'
      });
      
      // Directly process the payment using the generated QR data
      // The bank will internally use the QR validation and processing logic
      const transactionResponse = await bankAPI.processQRTransaction({
        paymentId: paymentId,
        amount: paymentData.amount,
        currency: 'RSD'
      });

      if (transactionResponse.data.success) {
        toast.success('Plaćanje je uspešno izvršeno!');
        
        // Wait a bit for user to see the success message
        setTimeout(() => {
          // Redirect back to PSP with success
          if (returnUrl) {
            window.location.href = returnUrl;
          } else {
            // Fallback redirect
            window.location.href = `http://localhost:3001/payment-success?transactionId=${pspTransactionId}`;
          }
        }, 2000);
      } else {
        toast.error(`Plaćanje neuspešno: ${transactionResponse.data.message || 'Nepoznata greška'}`);
      }
    } catch (error) {
      console.error('Error processing payment:', error);
      const errorMessage = error.response?.data?.message || 'Greška prilikom obrade plaćanja';
      toast.error(errorMessage);
    } finally {
      setVerifying(false);
    }
  };

  if (loading) {
    return (
      <div className="qr-payment-container">
        <div className="loading">
          <div className="spinner"></div>
          <p>Generisanje QR koda...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="qr-payment-container">
      <div className="qr-payment-card">
        <div className="bank-header">
          <h1>QR Payment</h1>
          <p>Secure payment via QR code</p>
        </div>

        <div className="payment-info">
          <h3>Payment details</h3>
          <div className="payment-details">
            <div className="detail-row">
              <span>Amount:</span>
              <span className="amount">{amount} RSD</span>
            </div>
            <div className="detail-row">
              <span>Receiver:</span>
              <span>{paymentData?.receiverName || 'Telekom Srbija'}</span>
            </div>
            <div className="detail-row">
              <span>Account:</span>
              <span>{paymentData?.accountNumber || '105000000000099939'}</span>
            </div>
            <div className="detail-row">
              <span>Order ID:</span>
              <span>{orderId}</span>
            </div>
          </div>
        </div>

        {qrCodeImage && (
          <div className="qr-code-section">
            <h3>Generate QR code</h3>
            {console.log('🖼️ Rendering QR code section, image exists:', !!qrCodeImage)}
            <div className="qr-code-container">
              <img 
                src={`data:image/png;base64,${qrCodeImage}`} 
                alt="QR Code" 
                className="qr-code-image"
              />
            </div>

          </div>
        )}
          <p>Click the button below to process the payment automatically using this QR code.</p>

        <div className="action-buttons">
          <button
            className="btn btn-secondary"
            onClick={handleCancel}
            disabled={verifying}
          >
            ❌ Cancel
          </button>
          <button
            className="btn btn-primary"
            onClick={handleVerifyAndPay}
            disabled={!qrCodeImage || verifying}
          >
            {verifying ? '🔄 Processing payment...' : '✅ Process payment'}
          </button>
        </div>

        <div className="security-notice">
          <p>🔒 Your data is secure and encrypted</p>
        </div>
      </div>
    </div>
  );
};

export default QRPayment;
