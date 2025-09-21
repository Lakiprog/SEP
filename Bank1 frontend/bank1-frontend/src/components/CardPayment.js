import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { bankAPI } from '../services/api';
import { toast } from 'react-toastify';
import './CardPayment.css';

const CardPayment = () => {
  const [searchParams] = useSearchParams();
  
  const [loading, setLoading] = useState(false);
  const [processing, setProcessing] = useState(false);
  const [fieldErrors, setFieldErrors] = useState({});
  
  // Ref to prevent duplicate API calls in React StrictMode
  const hasInitialized = useRef(false);
  
  // Card form data
  const [cardData, setCardData] = useState({
    cardNumber: '',
    cardHolderName: '',
    expiryDate: '',
    securityCode: ''
  });

  // Extract parameters from URL
  const amount = searchParams.get('amount');
  const currency = searchParams.get('currency') || 'RSD';
  const merchantId = searchParams.get('merchantId');
  const orderId = searchParams.get('orderId');
  const pspTransactionId = searchParams.get('pspTransactionId');
  const returnUrl = searchParams.get('returnUrl');
  const cancelUrl = searchParams.get('cancelUrl');

  const [paymentId, setPaymentId] = useState('');

  const generatePaymentId = useCallback(async () => {
    try {
      setLoading(true);
      
      const paymentRequest = {
        amount: parseFloat(amount),
        currency: currency,
        merchantId: merchantId,
        orderId: orderId || '',
        accountNumber: '105000000000099939', // Telecom account
        receiverName: 'Telekom Srbija'
      };

      console.log('🔄 Generating payment ID with:', paymentRequest);

      // For card payments, we can use the QR payment endpoint to generate a payment ID
      const response = await bankAPI.generateQRPayment(paymentRequest);
      
      if (response.data.success) {
        setPaymentId(response.data.paymentId);
        console.log('✅ Payment ID generated:', response.data.paymentId);
        toast.success('Ready for card payment!');
      } else {
        toast.error('Error preparing payment');
      }
    } catch (error) {
      console.error('Error generating payment ID:', error);
      toast.error('Error communicating with bank');
    } finally {
      setLoading(false);
    }
  }, [amount, currency, merchantId, orderId]);

  useEffect(() => {
    // Prevent duplicate calls in React StrictMode
    if (hasInitialized.current) return;
    hasInitialized.current = true;
    
    if (amount && merchantId) {
      console.log('[CARD DEBUG] Initializing payment ID generation...');
      generatePaymentId();
    } else {
      toast.error('Invalid payment parameters');
    }
  }, [amount, merchantId, generatePaymentId]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    
    // Apply formatting and validation
    let formattedValue = value;
    let error = '';
    
    if (name === 'cardNumber') {
      // Format card number with spaces (1234 5678 9012 3456)
      formattedValue = value.replace(/\s/g, '').replace(/(.{4})/g, '$1 ').trim();
      if (formattedValue.length > 19) formattedValue = formattedValue.substring(0, 19);
      
      // Validate card number length
      const digitsOnly = formattedValue.replace(/\s/g, '');
      if (digitsOnly.length > 0 && digitsOnly.length < 13) {
        error = 'Card number must have at least 13 digits';
      }
    } else if (name === 'expiryDate') {
      // Format expiry date (MM/YY)
      formattedValue = value.replace(/\D/g, '').replace(/(\d{2})(\d)/, '$1/$2');
      if (formattedValue.length > 5) formattedValue = formattedValue.substring(0, 5);
      
      // Validate expiry date in real-time
      if (formattedValue.length === 5) {
        if (!validateExpiryDate(formattedValue)) {
          const [month] = formattedValue.split('/').map(num => parseInt(num, 10));
          if (month < 1 || month > 12) {
            error = 'Month must be between 01 and 12';
          } else {
            error = 'Card is expired or invalid date';
          }
        }
      }
    } else if (name === 'securityCode') {
      // Allow only numbers, max 4 digits
      formattedValue = value.replace(/\D/g, '');
      if (formattedValue.length > 4) formattedValue = formattedValue.substring(0, 4);
      
      if (formattedValue.length > 0 && formattedValue.length < 3) {
        error = 'CVV must have at least 3 digits';
      }
    } else if (name === 'cardHolderName') {
      // Allow only letters and spaces, uppercase
      formattedValue = value.replace(/[^a-zA-Z\s]/g, '').toUpperCase();
      
      if (formattedValue.length > 0 && formattedValue.length < 2) {
        error = 'Name must have at least 2 characters';
      }
    }
    
    // Update field errors
    setFieldErrors(prev => ({
      ...prev,
      [name]: error
    }));
    
    setCardData(prev => ({
      ...prev,
      [name]: formattedValue
    }));
  };

  const validateExpiryDate = (expiryDate) => {
    // Check format MM/YY
    if (!/^\d{2}\/\d{2}$/.test(expiryDate)) {
      return false;
    }
    
    const [month, year] = expiryDate.split('/').map(num => parseInt(num, 10));
    
    // Check valid month (01-12)
    if (month < 1 || month > 12) {
      return false;
    }
    
    // Check if date is in the future
    const now = new Date();
    const currentYear = now.getFullYear() % 100; // Get last 2 digits of current year
    const currentMonth = now.getMonth() + 1; // getMonth() returns 0-11
    
    // If year is less than current year, it's expired
    if (year < currentYear) {
      return false;
    }
    
    // If year is current year, check month
    if (year === currentYear && month < currentMonth) {
      return false;
    }
    
    return true;
  };

  const validateForm = () => {
    if (!cardData.cardNumber || cardData.cardNumber.replace(/\s/g, '').length < 13) {
      toast.error('Enter a valid card number');
      return false;
    }
    if (!cardData.cardHolderName || cardData.cardHolderName.length < 2) {
      toast.error('Enter the card holder name');
      return false;
    }
    if (!cardData.expiryDate || !validateExpiryDate(cardData.expiryDate)) {
      toast.error('Enter a valid expiry date (MM/YY) - card must not be expired');
      return false;
    }
    if (!cardData.securityCode || cardData.securityCode.length < 3) {
      toast.error('Enter a valid CVV code');
      return false;
    }
    return true;
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

  const handlePayment = async () => {
    if (!validateForm()) return;
    if (!paymentId) {
      toast.error('Payment ID is not available');
      return;
    }

    try {
      setProcessing(true);
      
      console.log('🔄 Processing card payment:', {
        paymentId,
        amount: parseFloat(amount),
        cardNumber: cardData.cardNumber.replace(/\s/g, ''),
        maskedCard: cardData.cardNumber.replace(/\d(?=\d{4})/g, '*')
      });
      
      // Process the card payment
      const transactionResponse = await bankAPI.processTransaction({
        PAYMENT_ID: paymentId,
        PAN: cardData.cardNumber.replace(/\s/g, ''),
        SecurityCode: cardData.securityCode,
        CardHolderName: cardData.cardHolderName,
        ExpiryDate: cardData.expiryDate,
        Amount: parseFloat(amount)
      });

      if (transactionResponse.data.success) {
        toast.success('Card payment successful!');
        
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
        toast.error(`Card payment failed: ${transactionResponse.data.message || 'Unknown error'}`);
      }
    } catch (error) {
      console.error('Error processing card payment:', error);
      const errorMessage = error.response?.data?.error || error.response?.data?.message || 'Error processing card payment';
      toast.error(errorMessage);
    } finally {
      setProcessing(false);
    }
  };

  if (loading) {
    return (
      <div className="card-payment-container">
        <div className="loading">
          <div className="spinner"></div>
          <p>Preparing payment...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="card-payment-container">
      <div className="card-payment-card">
        <div className="bank-header">
          <h1>Card Payment</h1>
          <p>Secure payment with credit or debit card</p>
        </div>

        <div className="payment-info">
          <h3>Payment details</h3>
          <div className="payment-details">
            <div className="detail-row">
              <span>Amount:</span>
              <span className="amount">{amount} {currency}</span>
            </div>
            <div className="detail-row">
              <span>Merchant:</span>
              <span>Telekom Srbija</span>
            </div>
            <div className="detail-row">
              <span>Order ID:</span>
              <span>{orderId}</span>
            </div>
          </div>
        </div>

        <div className="card-form">
          <h3>Card information</h3>
          
          <div className="form-group">
            <label htmlFor="cardNumber">Card number</label>
            <input
              type="text"
              id="cardNumber"
              name="cardNumber"
              value={cardData.cardNumber}
              onChange={handleInputChange}
              placeholder="1234 5678 9012 3456"
              className={`card-input ${fieldErrors.cardNumber ? 'error' : ''}`}
              maxLength="19"
            />
            {fieldErrors.cardNumber && <span className="error-message">{fieldErrors.cardNumber}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="cardHolderName">Card holder name</label>
            <input
              type="text"
              id="cardHolderName"
              name="cardHolderName"
              value={cardData.cardHolderName}
              onChange={handleInputChange}
              placeholder="JOHN DOE"
              className={`card-input ${fieldErrors.cardHolderName ? 'error' : ''}`}
            />
            {fieldErrors.cardHolderName && <span className="error-message">{fieldErrors.cardHolderName}</span>}
          </div>

          <div className="form-row">
            <div className="form-group half">
              <label htmlFor="expiryDate">Expiry date</label>
              <input
                type="text"
                id="expiryDate"
                name="expiryDate"
                value={cardData.expiryDate}
                onChange={handleInputChange}
                placeholder="MM/YY"
                className={`card-input ${fieldErrors.expiryDate ? 'error' : ''}`}
                maxLength="5"
              />
              {fieldErrors.expiryDate && <span className="error-message">{fieldErrors.expiryDate}</span>}
            </div>
            <div className="form-group half">
              <label htmlFor="securityCode">CVV</label>
              <input
                type="text"
                id="securityCode"
                name="securityCode"
                value={cardData.securityCode}
                onChange={handleInputChange}
                placeholder="123"
                className={`card-input ${fieldErrors.securityCode ? 'error' : ''}`}
                maxLength="4"
              />
              {fieldErrors.securityCode && <span className="error-message">{fieldErrors.securityCode}</span>}
            </div>
          </div>
        </div>

        <div className="action-buttons">
          <button
            className="btn btn-secondary"
            onClick={handleCancel}
            disabled={processing}
          >
            ❌ Cancel
          </button>
          <button
            className="btn btn-primary"
            onClick={handlePayment}
            disabled={processing || !paymentId}
          >
            {processing ? '🔄 Processing payment...' : '✅ Pay now'}
          </button>
        </div>

        <div className="security-notice">
          <p>💡 Never share your card data with unauthorized people</p>
        </div>
      </div>
    </div>
  );
};

export default CardPayment;
